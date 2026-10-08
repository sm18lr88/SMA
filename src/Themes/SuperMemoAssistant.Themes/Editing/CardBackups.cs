// Lists and restores the backups that edits made, and saves and compares snapshots to find where SuperMemo keeps a setting.
using System.Text;
using System.Text.Json;

namespace SuperMemoAssistant.Themes.Editing;

internal sealed class CardBackups(SuperMemoInstall install, Encoding ansi)
{
  private string SnapshotFolder(string name) => Path.Combine(install.CollectionBackupFolder, "snapshots", name);

  /// <summary>Names of the backup folders (those with a manifest), in name order.</summary>
  public List<string> List()
  {
    var root = install.CollectionBackupFolder;

    return Directory.Exists(root)
      ? Directory.GetDirectories(root).Where(d => File.Exists(Path.Combine(d, "manifest.json"))).Select(d => Path.GetFileName(d)!).Order(StringComparer.OrdinalIgnoreCase).ToList()
      : [];
  }

  /// <summary>The files of a backup with their original contents, ready to be written back through the safe writer.</summary>
  public List<KeyValuePair<string, byte[]>> RestoreChanges(string name)
  {
    var source   = Path.Combine(install.CollectionBackupFolder, name);
    var manifest = Path.Combine(source, "manifest.json");

    if (!File.Exists(manifest))
      throw new ThemeException($"there is no backup named '{name}' (see the backups command)");

    using var document = JsonDocument.Parse(File.ReadAllText(manifest));

    return document.RootElement.GetProperty("files").EnumerateArray()
                   .Select(f => new KeyValuePair<string, byte[]>(f.GetProperty("original").GetString()!, File.ReadAllBytes(Path.Combine(source, f.GetProperty("backup").GetString()!))))
                   .ToList();
  }

  /// <summary>info\*, registry\*, collection.ini and the stylesheets and ini files of bin: everything an edit or a setting can change.</summary>
  private List<string> SnapshotFiles()
  {
    var collection = install.CollectionFolder;
    var files      = new List<string>();

    foreach (var (folder, pattern) in new[] { (Path.Combine(collection, "info"), "*"), (Path.Combine(collection, "registry"), "*") })
      files.AddRange(Sorted(folder, pattern));

    files.Add(Path.Combine(collection, "collection.ini"));
    files.AddRange(Sorted(install.BinFolder, "*.css"));
    files.AddRange(Sorted(install.BinFolder, "*.ini"));

    return files.Where(File.Exists).ToList();
  }

  private static IEnumerable<string> Sorted(string folder, string pattern) =>
    Directory.Exists(folder) ? Directory.EnumerateFiles(folder, pattern).Order(StringComparer.OrdinalIgnoreCase) : [];

  private string Relative(string path) => path.StartsWith(install.Root + "\\", StringComparison.OrdinalIgnoreCase)
    ? path[(install.Root.Length + 1)..]
    : Path.Combine("external", Path.GetPathRoot(path)!.TrimEnd('\\', ':'), path[Path.GetPathRoot(path)!.Length..]);

  public string Snapshot(string name)
  {
    var destination = SnapshotFolder(name);

    if (Directory.Exists(destination))
      Directory.Delete(destination, true);

    foreach (var file in SnapshotFiles())
    {
      var target = Path.Combine(destination, Relative(file));

      Directory.CreateDirectory(Path.GetDirectoryName(target)!);
      File.Copy(file, target, true);
    }

    return destination;
  }

  /// <summary>Spans of bytes that differ, merging spans closer than <paramref name="gap" /> bytes.</summary>
  internal static List<(int Start, int End)> Ranges(byte[] a, byte[] b, int gap = 4)
  {
    var spans = new List<(int Start, int End)>();

    for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
    {
      var same = i < a.Length && i < b.Length ? a[i] == b[i] : i >= a.Length && i >= b.Length;

      if (same)
        continue;

      if (spans.Count > 0 && i - spans[^1].End <= gap)
        spans[^1] = (spans[^1].Start, i + 1);
      else
        spans.Add((i, i + 1));
    }

    return spans;
  }

  private static string Locate(CardCollection collection, int offset)
  {
    foreach (var record in collection.Elements.Concat(collection.Templates))
    {
      if (record.ComponPosition > offset || offset >= record.End)
        continue;

      var label = $"{record.Kind} {record.Number} '{(record.Title.Length > 30 ? record.Title[..30] : record.Title)}'";

      foreach (var component in record.Components)
      {
        if (component.BodyOffset <= offset && offset < component.BodyOffset + component.Size)
          return $"{label} component {component.Index} ({component.Kind}) body+{offset - component.BodyOffset}";
      }

      return $"{label} header+{offset - record.ComponPosition - 11}";
    }

    return "unreferenced space";
  }

  private static string Hex(byte[] data, int start, int end) => string.Join(' ', data.Skip(start).Take(Math.Min(32, end - start)).Select(b => b.ToString("x2")));

  /// <summary>What changed since a snapshot, as the lines "diff" prints. A snapshot that does not exist counts as empty.</summary>
  public List<string> Diff(string name)
  {
    var snapshot   = SnapshotFolder(name);
    var collection = CardCollection.Open(install.CollectionFolder, ansi);
    var lines      = new List<string>();

    foreach (var file in SnapshotFiles())
    {
      var oldPath = Path.Combine(snapshot, Relative(file));
      var old     = File.Exists(oldPath) ? File.ReadAllBytes(oldPath) : [];
      var current = File.ReadAllBytes(file);

      if (old.AsSpan().SequenceEqual(current))
        continue;

      lines.Add($"== {Relative(file)} ({old.Length} -> {current.Length} bytes)");

      if (Path.GetExtension(file).ToLowerInvariant() is ".ini" or ".css")
      {
        var before = SplitLinesSet(ansi.GetString(old));

        lines.AddRange(SplitLinesSet(ansi.GetString(current)).Except(before).Order(StringComparer.Ordinal).Select(l => $"   + {l}"));

        continue;
      }

      foreach (var (start, end) in Ranges(old, current).Take(40))
      {
        var where = string.Equals(Path.GetFileName(file), "compon.dat", StringComparison.OrdinalIgnoreCase) ? Locate(collection, start) : "";

        lines.Add($"   @{start}: {Hex(old, Math.Min(start, old.Length), Math.Min(end, old.Length))} -> {Hex(current, Math.Min(start, current.Length), Math.Min(end, current.Length))} {where}");
      }
    }

    return lines;
  }

  private static HashSet<string> SplitLinesSet(string text) => PreviewDiff.SplitLines(text).ToHashSet();
}
