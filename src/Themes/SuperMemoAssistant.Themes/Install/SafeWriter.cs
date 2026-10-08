// Writes files only while SuperMemo is closed, and backs up every file it changes first.
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes;

internal sealed partial class SafeWriter(SuperMemoInstall install, Func<IReadOnlyList<string>> running, Func<DateTime>? now = null)
{
  [GeneratedRegex(@"^sm\d+$", RegexOptions.IgnoreCase)]
  private static partial Regex SuperMemoProcess();

  [GeneratedRegex("[^A-Za-z0-9_-]+")]
  private static partial Regex NotLabel();

  /// <summary>Names of running SuperMemo processes, for example "sm20.exe".</summary>
  public static IReadOnlyList<string> RunningSuperMemo()
  {
    var processes = Process.GetProcesses();

    try
    {
      return processes.Where(p => SuperMemoProcess().IsMatch(p.ProcessName)).Select(p => $"{p.ProcessName.ToLowerInvariant()}.exe").Distinct().Order().ToList();
    }
    finally
    {
      foreach (var p in processes)
        p.Dispose();
    }
  }

  public void RequireClosed()
  {
    var found = running();

    if (found.Count > 0)
      throw new ThemeException($"SuperMemo is running ({string.Join(", ", found)}). Close it first: it keeps the collection in memory and would overwrite these edits on exit.");
  }

  /// <summary>The files of <paramref name="changes" /> whose content differs from what is on disk, in the order given.</summary>
  public static List<KeyValuePair<string, byte[]>> Pending(IEnumerable<KeyValuePair<string, byte[]>> changes) =>
    changes.Where(kv => !File.Exists(kv.Key) || !File.ReadAllBytes(kv.Key).AsSpan().SequenceEqual(kv.Value)).ToList();

  /// <summary>Backs up and writes the files whose content changes. Returns how many files were written.</summary>
  public int Write(IReadOnlyDictionary<string, byte[]> changes, string label) => Write(changes, label, out _);

  /// <summary>As <see cref="Write(IReadOnlyDictionary{string, byte[]}, string)" />, and says where the backup went (null when nothing changed).</summary>
  public int Write(IEnumerable<KeyValuePair<string, byte[]>> changes, string label, out string? backupFolder)
  {
    var changed = Pending(changes);

    backupFolder = null;

    if (changed.Count == 0)
      return 0;

    RequireClosed();
    backupFolder = MakeBackup(changed.Select(kv => kv.Key).ToList(), label);

    foreach (var (path, data) in changed)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllBytes(path, data);
    }

    return changed.Count;
  }

  private string Relative(string path)
  {
    var full = Path.GetFullPath(path);
    var root = Path.GetFullPath(install.Root).TrimEnd('\\') + '\\';

    if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
      return full[root.Length..];

    var drive = Path.GetPathRoot(full)!.TrimEnd('\\', ':');

    return Path.Combine("external", drive, full[(Path.GetPathRoot(full)!.Length)..]);
  }

  internal string MakeBackup(IReadOnlyList<string> files, string label)
  {
    var stamp = (now?.Invoke() ?? DateTime.Now).ToString("yyyyMMdd-HHmmss");
    var dest  = Path.Combine(install.CollectionBackupFolder, $"{stamp}-{NotLabel().Replace(label, "-")}");

    for (var n = 2; Directory.Exists(dest); n++)
      dest = Path.Combine(install.CollectionBackupFolder, $"{Path.GetFileName(dest)}-{n}");

    var kept = new List<object>();

    foreach (var f in files.Where(File.Exists))
    {
      var target = Path.Combine(dest, Relative(f));

      Directory.CreateDirectory(Path.GetDirectoryName(target)!);
      File.Copy(f, target, true);
      kept.Add(new { backup = Relative(f), original = Path.GetFullPath(f) });
    }

    Directory.CreateDirectory(dest);
    File.WriteAllText(Path.Combine(dest, "manifest.json"), JsonSerializer.Serialize(new { label, files = kept }, new JsonSerializerOptions { WriteIndented = true }));

    return dest;
  }
}
