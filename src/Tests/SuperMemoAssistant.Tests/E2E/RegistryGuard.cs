// Snapshots and restores the registry keys that SuperMemo and SMA plugins write, so an end-to-end run leaves no trace.
namespace SuperMemoAssistant.Tests.E2E;

using Microsoft.Win32;

internal sealed class RegistryGuard : IDisposable
{
  /// <summary>Keys restored to their exact prior state (deleted when they did not exist).</summary>
  private static readonly (RegistryKey Root, string Path)[] Guarded =
  [
    (Registry.LocalMachine, @"SOFTWARE\Classes\SuperMemo"),
    (Registry.LocalMachine, @"SOFTWARE\Classes\.kno"),
    (Registry.CurrentUser, @"Software\SuperMemo"),
    (Registry.CurrentUser, @"Software\Classes\SuperMemo"),
    (Registry.CurrentUser, @"Software\Classes\.kno"),
    (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.KNO"),
    (Registry.CurrentUser, @"Software\Google\Chrome\NativeMessagingHosts\supermemoassistant.plugins.import.browserextension"),
    (Registry.CurrentUser, @"Software\Mozilla\NativeMessagingHosts\supermemoassistant.plugins.import.browserextension"),
    (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SuperMemoAssistant"),
  ];

  /// <summary>Shared parents (toast registration): only subkeys created during the run are removed; existing ones stay untouched.</summary>
  private static readonly (RegistryKey Root, string Path)[] AddOnly =
  [
    (Registry.CurrentUser, @"Software\Classes\AppUserModelId"),
    (Registry.CurrentUser, @"Software\Classes\CLSID"),
  ];

  private readonly List<(RegistryKey Root, string Path, Node? Tree)> _snapshot =
    Guarded.Select(g => (g.Root, g.Path, Read(g.Root, g.Path))).ToList();

  private readonly List<(RegistryKey Root, string Path, HashSet<string> Children)> _existingChildren =
    AddOnly.Select(a => (a.Root, a.Path, ChildNames(a.Root, a.Path))).ToList();

  /// <summary>Keys that changed during the run (restored on dispose).</summary>
  public List<string> Changed { get; } = [];

  public void Dispose()
  {
    var failures = new List<Exception>();

    foreach (var (root, path, tree) in _snapshot)
      Restore(failures, path, () =>
      {
        if (Equals(Read(root, path), tree))
          return;

        Changed.Add(path);
        root.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
        if (tree is not null)
          Write(root, path, tree);
      });

    foreach (var (root, path, before) in _existingChildren)
      foreach (var added in ChildNames(root, path).Except(before, StringComparer.OrdinalIgnoreCase))
        Restore(failures, $@"{path}\{added}", () =>
        {
          Changed.Add($@"{path}\{added}");
          root.DeleteSubKeyTree($@"{path}\{added}", throwOnMissingSubKey: false);
        });

    if (failures.Count > 0)
      throw new AggregateException("Some registry keys could not be restored.", failures);
  }

  private static void Restore(List<Exception> failures, string path, Action restore)
  {
    try { restore(); }
    catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
    {
      failures.Add(new InvalidOperationException($"Could not restore {path}.", ex));
    }
  }

  private static HashSet<string> ChildNames(RegistryKey root, string path)
  {
    using var key = root.OpenSubKey(path);
    return new HashSet<string>(key?.GetSubKeyNames() ?? [], StringComparer.OrdinalIgnoreCase);
  }

  private static Node? Read(RegistryKey root, string path)
  {
    using var key = root.OpenSubKey(path);
    if (key is null)
      return null;

    var values  = key.GetValueNames().ToDictionary(n => n, n => (key.GetValue(n, null, RegistryValueOptions.DoNotExpandEnvironmentNames), key.GetValueKind(n)));
    var subkeys = key.GetSubKeyNames().ToDictionary(n => n, n => Read(root, $@"{path}\{n}")!);
    return new Node(values, subkeys);
  }

  private static void Write(RegistryKey root, string path, Node tree)
  {
    using (var key = root.CreateSubKey(path))
      foreach (var (name, (value, kind)) in tree.Values)
        key.SetValue(name, value!, kind);

    foreach (var (name, sub) in tree.Subkeys)
      Write(root, $@"{path}\{name}", sub);
  }

  private sealed record Node(Dictionary<string, (object? Value, RegistryValueKind Kind)> Values, Dictionary<string, Node> Subkeys)
  {
    public bool Equals(Node? other) =>
      other is not null
      && Values.Count == other.Values.Count
      && Values.All(v => other.Values.TryGetValue(v.Key, out var o) && o.Kind == v.Value.Kind && ValueEquals(o.Value, v.Value.Value))
      && Subkeys.Count == other.Subkeys.Count
      && Subkeys.All(s => other.Subkeys.TryGetValue(s.Key, out var o) && s.Value.Equals(o));

    public override int GetHashCode() => Values.Count ^ Subkeys.Count;

    private static bool ValueEquals(object? a, object? b) =>
      a is byte[] x && b is byte[] y ? x.SequenceEqual(y)
      : a is string[] s && b is string[] t ? s.SequenceEqual(t)
      : Equals(a, b);
  }
}
