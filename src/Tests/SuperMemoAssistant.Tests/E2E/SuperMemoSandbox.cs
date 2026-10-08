// A throwaway copy of SuperMemo 20 (exe, bin, and its smallest collection) that never touches the real install.
namespace SuperMemoAssistant.Tests.E2E;

using System.Text;

internal sealed class SuperMemoSandbox : IDisposable
{
  private readonly FileStream _realExeLock;

  private readonly string _collection;

  private SuperMemoSandbox(string smRoot, string root)
  {
    Root        = root;
    _collection = SmallestCollection(smRoot);

    // Hold the real exe open without delete sharing: nothing in the sandbox run can rename or delete it.
    _realExeLock = new FileStream(Path.Combine(smRoot, "sm20.exe"), FileMode.Open, FileAccess.Read, FileShare.Read);

    Directory.CreateDirectory(Path.Combine(root, "systems"));
    File.Copy(Path.Combine(smRoot, "sm20.exe"), ExePath);
    CopyDirectory(Path.Combine(smRoot, "bin"), Path.Combine(root, "bin"));
    CopyDirectory(Path.Combine(smRoot, "systems", _collection), CollectionDir);
    File.Copy(Path.Combine(smRoot, "systems", _collection + ".KNO"), KnoPath);

    var ini = Path.Combine(root, "bin", "supermemo.ini");
    var text = File.ReadAllText(ini, Encoding.Latin1);
    text = IniSet(text, "Termination", "Collection", CollectionDir.ToLowerInvariant());
    text = IniSet(text, "Systems", "System1", CollectionDir.ToLowerInvariant());
    text = IniSet(text, "System", "Show tips", "0");
    text = IniSet(text, "Termination", "Error", "0");
    File.WriteAllText(ini, text, Encoding.Latin1);
  }

  public string Root { get; }

  public string ExePath => Path.Combine(Root, "sm20.exe");

  public string CollectionDir => Path.Combine(Root, "systems", _collection);

  public string KnoPath => Path.Combine(Root, "systems", _collection + ".KNO");

  public static SuperMemoSandbox Create(string smRoot) =>
    new(smRoot, Path.Combine(Path.GetTempPath(), $"sma-e2e-{Guid.NewGuid():N}"));

  public void Dispose()
  {
    _realExeLock.Dispose();

    for (var attempt = 0; attempt < 20; attempt++)
      try
      {
        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
          File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(Root, recursive: true);
        return;
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        Thread.Sleep(500); // the terminated process may still hold file handles for a moment
      }
  }

  /// <summary>The name of the collection (a .KNO file with a folder of the same name) that is the cheapest to copy.</summary>
  private static string SmallestCollection(string smRoot)
  {
    var systems = Path.Combine(smRoot, "systems");

    return Directory.EnumerateFiles(systems, "*.KNO")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => Directory.Exists(Path.Combine(systems, name!)))
                    .OrderBy(name => Directory.EnumerateFiles(Path.Combine(systems, name!), "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length))
                    .FirstOrDefault()
           ?? throw new InvalidOperationException($"{systems} has no collection (a .KNO file and a folder of the same name).");
  }

  private static void CopyDirectory(string source, string target)
  {
    foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
      Directory.CreateDirectory(dir.Replace(source, target, StringComparison.OrdinalIgnoreCase));
    Directory.CreateDirectory(target);
    foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
      File.Copy(file, file.Replace(source, target, StringComparison.OrdinalIgnoreCase));
  }

  private static string IniSet(string ini, string section, string key, string value)
  {
    var lines = ini.Replace("\r\n", "\n").Split('\n').ToList();
    var start = lines.FindIndex(l => l.Trim().Equals($"[{section}]", StringComparison.OrdinalIgnoreCase));
    if (start < 0)
    {
      lines.AddRange([$"[{section}]", $"{key}={value}"]);
      return string.Join("\r\n", lines);
    }

    var end = lines.FindIndex(start + 1, l => l.TrimStart().StartsWith('['));
    if (end < 0) end = lines.Count;

    var existing = lines.FindIndex(start + 1, end - start - 1, l => l.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
    if (existing >= 0) lines[existing] = $"{key}={value}";
    else lines.Insert(start + 1, $"{key}={value}");

    return string.Join("\r\n", lines);
  }
}
