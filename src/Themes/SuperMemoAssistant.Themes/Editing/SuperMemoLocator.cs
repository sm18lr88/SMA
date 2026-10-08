// Finds the SuperMemo installation and its collections: an explicit folder, an environment variable, SMA's own setting, or the .kno file association.
using System.Text;
using Microsoft.Win32;
using SuperMemoAssistant.Themes.Cards;

namespace SuperMemoAssistant.Themes.Editing;

internal static class SuperMemoLocator
{
  public const string RootVariable = "SMCARDS_ROOT";

  private static bool IsInstall(string folder) =>
    Directory.Exists(Path.Combine(folder, "bin")) && Directory.Exists(folder) && Directory.EnumerateFiles(folder, "sm*.exe").Any();

  /// <summary>The folder of the exe registered for .kno files. SuperMemo registers itself on every start.</summary>
  private static string? RegisteredInstall()
  {
    if (!OperatingSystem.IsWindows())
      return null;

    try
    {
      using var kno      = Registry.ClassesRoot.OpenSubKey(".kno");
      var       progId   = kno?.GetValue("") as string;
      using var command  = progId is null ? null : Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
      var       text     = command?.GetValue("") as string;

      if (string.IsNullOrWhiteSpace(text))
        return null;

      var exe = text.StartsWith('"') ? text.Split('"')[1] : text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

      return Path.GetDirectoryName(exe);
    }
    catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
    {
      return null;
    }
  }

  public static string FindRoot(string? explicitRoot) => FindRoot(explicitRoot, SmaData.Folder());

  /// <summary>
  ///   The SuperMemo folder: <paramref name="explicitRoot" />, then $SMCARDS_ROOT, then the sm20.exe that SMA is set up with
  ///   (in <paramref name="smaDataFolder" />), then the .kno association.
  /// </summary>
  public static string FindRoot(string? explicitRoot, string smaDataFolder)
  {
    foreach (var (source, folder) in new[] { ("--sm-root", explicitRoot), (RootVariable, Environment.GetEnvironmentVariable(RootVariable)) })
    {
      if (string.IsNullOrEmpty(folder))
        continue;

      return IsInstall(folder) ? folder : throw new ThemeException($"{source} {folder} is not a SuperMemo folder (needs bin\\ and sm*.exe)");
    }

    if (SmaData.ConfiguredExe(smaDataFolder) is { } exe && Path.GetDirectoryName(exe) is { } smaRoot && IsInstall(smaRoot))
      return smaRoot;

    var registered = RegisteredInstall();

    return registered is not null && IsInstall(registered)
      ? registered
      : throw new ThemeException($"SuperMemo not found. Pass --sm-root <SuperMemo folder>, set {RootVariable}, or set up SMA with your sm20.exe.");
  }

  private static bool IsCollection(string folder) => File.Exists(Path.Combine(folder, "info", "compon.dat"));

  public static List<string> ListCollections(string root, Encoding ansi)
  {
    var systems = Path.Combine(root, "systems");
    var found   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    if (Directory.Exists(systems))
    {
      foreach (var kno in Directory.EnumerateFiles(systems, "*.KNO"))
        found.TryAdd(Path.Combine(Path.GetDirectoryName(kno)!, Path.GetFileNameWithoutExtension(kno)), "");
    }

    var ini = Path.Combine(root, "bin", "supermemo.ini");

    if (File.Exists(ini) && IniSection(File.ReadAllBytes(ini), ansi, "Systems") is { } values)
    {
      foreach (var value in values.Select(NormalizePath))
        found.TryAdd(value, "");
    }

    return found.Keys.Where(IsCollection).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
  }

  /// <summary>The value written the way Python's Path prints it: backslashes, no trailing separator.</summary>
  private static string NormalizePath(string path) => path.Replace('/', '\\').TrimEnd('\\');

  private static List<string>? IniSection(byte[] raw, Encoding ansi, string section)
  {
    var text    = ansi.GetString(raw);
    var values  = new List<string>();
    var inside  = false;
    var present = false;

    foreach (var line in text.Split('\n').Select(l => l.TrimEnd('\r').Trim()))
    {
      if (line.StartsWith('[') && line.EndsWith(']'))
      {
        inside = string.Equals(line[1..^1], section, StringComparison.Ordinal);
        present |= inside;

        continue;
      }

      if (inside && line.Length > 0 && line[0] is not ('#' or ';') && line.IndexOfAny(['=', ':']) is var at and > 0)
        values.Add(line[(at + 1)..].Trim());
    }

    return present ? values : null;
  }

  public static string ResolveCollection(string root, string? wanted, Encoding ansi)
  {
    var collections = ListCollections(root, ansi);

    if (!string.IsNullOrEmpty(wanted))
    {
      if (IsCollection(wanted))
        return NormalizePath(wanted);

      var matches = collections.Where(c => Path.GetFileName(c).Contains(wanted, StringComparison.OrdinalIgnoreCase)).ToList();

      return matches.Count == 1
        ? matches[0]
        : throw new ThemeException($"collection {PyRepr(wanted)} matched {matches.Count}: {PyList(matches.Select(Path.GetFileName))}");
    }

    var ini  = Path.Combine(root, "bin", "supermemo.ini");
    var last = File.Exists(ini) ? new IniText(ansi.GetString(File.ReadAllBytes(ini))).Get("Termination", "Collection") : "";

    if (last.Length > 0 && IsCollection(last))
      return NormalizePath(last);

    return collections.Count == 1
      ? collections[0]
      : throw new ThemeException($"pick a collection with --collection: {PyList(collections.Select(Path.GetFileName))}");
  }

  /// <summary>How Python prints a string in an error message: single quotes.</summary>
  internal static string PyRepr(string text) => "'" + text.Replace("\\", "\\\\").Replace("'", "\\'") + "'";

  internal static string PyList(IEnumerable<string?> items) => "[" + string.Join(", ", items.Select(i => PyRepr(i ?? ""))) + "]";
}
