// Imports themes from base16/base24 files, VS Code themes and extensions, and Obsidian themes into the user's library.
using System.Text.Json;
using SuperMemoAssistant.Themes.Import;

namespace SuperMemoAssistant.Themes;

/// <param name="Added">The themes now in the user library, with the ids they were stored under.</param>
/// <param name="Skipped">Files that were left out, each with the reason.</param>
/// <param name="Notes">Things worth telling the user, for example that Obsidian was not found.</param>
public sealed record ImportResult(IReadOnlyList<ThemeEntry> Added, IReadOnlyList<string> Skipped, IReadOnlyList<string> Notes);

/// <summary>
///   Reads theme files and stores the result in the user's library file (<see cref="ThemeLibrary.Load" /> reads it back).
///   Accepted sources: a base16 or base24 .yaml file or a folder of them, a VS Code theme .json or extension folder, and an
///   Obsidian theme folder (one that holds theme.css).
/// </summary>
public sealed class ThemeImporter
{
  private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

  private readonly ThemeLibrary _library;
  private readonly string       _userLibraryPath;
  private readonly string?      _obsidianCachePath;

  /// <param name="library">The library the new themes are checked against for id clashes.</param>
  /// <param name="userLibraryPath">The user's own library file. It is created when the first theme is imported.</param>
  /// <param name="obsidianCachePath">Where to keep Obsidian's default variables once they are read from an installed Obsidian. Null for no cache.</param>
  public ThemeImporter(ThemeLibrary library, string userLibraryPath, string? obsidianCachePath = null)
  {
    _library           = library;
    _userLibraryPath   = userLibraryPath;
    _obsidianCachePath = obsidianCachePath;
  }

  /// <summary>Reads <paramref name="path" /> and adds what it finds to the user library.</summary>
  /// <param name="path">A file or folder to import.</param>
  /// <param name="name">A name for a VS Code theme file that has none of its own.</param>
  /// <param name="only">For a VS Code extension: the labels of the themes to import. Empty for all.</param>
  /// <exception cref="ThemeException">The source is not a supported theme source, or a single file cannot be read.</exception>
  public ImportResult Import(string path, string? name = null, IReadOnlyList<string>? only = null)
  {
    ReadOutcome read;

    try
    {
      read = Read(path, name, only ?? []);
    }
    catch (Exception ex) when (ex is not ThemeException && IsBadSource(ex))
    {
      throw new ThemeException($"{path} cannot be imported: {ex.Message}", ex);
    }

    var merged = Merge(_library.Entries, read.Entries);

    if (merged.Count > 0)
      SaveUserLibrary(Existing().Concat(merged));

    return new ImportResult(merged, read.Skipped, read.Notes);
  }

  internal sealed record ReadOutcome(List<ThemeEntry> Entries, List<string> Skipped, List<string> Notes);

  internal ReadOutcome Read(string path, string? name, IReadOnlyList<string> only)
  {
    var skipped = new List<string>();
    var notes   = new List<string>();

    if (Directory.Exists(path))
    {
      if (File.Exists(Path.Combine(path, "theme.css")))
      {
        var css = ObsidianBase.Load(_obsidianCachePath, out var note);

        if (note is not null)
          notes.Add(note);

        return new ReadOutcome(ObsidianImporter.Import(path, css), skipped, notes);
      }

      if (File.Exists(Path.Combine(path, "package.json")))
        return new ReadOutcome(VsCodeExtension(path, only), skipped, notes);

      var entries = new List<ThemeEntry>();
      var files   = Directory.EnumerateFiles(path, "*.yaml").Concat(Directory.EnumerateFiles(path, "*.yml"))
                             .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

      foreach (var file in files)
      {
        try
        {
          entries.Add(Base16Importer.Import(file));
        }
        catch (Exception ex) when (IsBadSource(ex))
        {
          skipped.Add($"{Path.GetFileName(file)}: {ex.Message}");
        }
      }

      return new ReadOutcome(entries, skipped, notes);
    }

    var extension = Path.GetExtension(path).ToLowerInvariant();

    try
    {
      if (extension is ".yaml" or ".yml")
        return new ReadOutcome([Base16Importer.Import(path)], skipped, notes);

      if (extension == ".json")
      {
        var data = Jsonc.Parse(Jsonc.ReadFile(path));
        var own  = Jsonc.String(data, "name");

        return new ReadOutcome([VsCodeImporter.Import(path, name ?? (string.IsNullOrEmpty(own) ? Path.GetFileNameWithoutExtension(path) : own))], skipped, notes);
      }
    }
    catch (Exception ex) when (IsBadSource(ex))
    {
      throw new ThemeException($"{Path.GetFileName(path)} cannot be imported: {ex.Message}", ex);
    }

    throw new ThemeException($"don't know how to import {path}");
  }

  private static bool IsBadSource(Exception ex) =>
    ex is FormatException or KeyNotFoundException or JsonException or InvalidOperationException or IOException or ArgumentException or UnauthorizedAccessException;

  private static List<ThemeEntry> VsCodeExtension(string folder, IReadOnlyList<string> only)
  {
    var package = Jsonc.Parse(Jsonc.ReadFile(Path.Combine(folder, "package.json")));
    var nlsFile = Path.Combine(folder, "package.nls.json");
    var nls     = File.Exists(nlsFile) ? Jsonc.Parse(Jsonc.ReadFile(nlsFile)) : default;

    string Localize(string text)
    {
      if (!(text.StartsWith('%') && text.EndsWith('%')) || nls.ValueKind != JsonValueKind.Object || !nls.TryGetProperty(text.Trim('%'), out var found))
        return text;

      return found.ValueKind switch
      {
        JsonValueKind.Object => Jsonc.String(found, "message") ?? text,
        JsonValueKind.String => found.GetString()!,
        _                    => text,
      };
    }

    var output = new List<ThemeEntry>();

    if (package.ValueKind != JsonValueKind.Object || !package.TryGetProperty("contributes", out var contributes)
        || contributes.ValueKind != JsonValueKind.Object || !contributes.TryGetProperty("themes", out var themes) || themes.ValueKind != JsonValueKind.Array)
      return output;

    var source = $"VS Code: {Localize(FirstNonEmpty(Jsonc.String(package, "displayName"), Jsonc.String(package, "name"), Path.GetFileName(folder.TrimEnd('\\', '/'))))}";

    foreach (var theme in themes.EnumerateArray())
    {
      var themePath = Jsonc.String(theme, "path") ?? throw new KeyNotFoundException("a theme has no path");
      var label     = Localize(FirstNonEmpty(Jsonc.String(theme, "label"), Jsonc.String(theme, "id"), Path.GetFileNameWithoutExtension(themePath)));

      if (only.Count > 0 && !only.Any(o => string.Equals(o, label, StringComparison.OrdinalIgnoreCase)))
        continue;

      var entry = VsCodeImporter.Import(Path.GetFullPath(Path.Combine(folder, themePath)), label, Jsonc.String(theme, "uiTheme") ?? "vs-dark");

      output.Add(entry with { Source = source });
    }

    return output;
  }

  private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";

  /// <summary>Adds entries. The same id from another source gets a source-tagged id and name, so neither theme is lost.</summary>
  internal static List<ThemeEntry> Merge(IReadOnlyList<ThemeEntry> library, IEnumerable<ThemeEntry> added)
  {
    var byId   = library.ToDictionary(e => e.Id, e => e);
    var output = new List<ThemeEntry>();

    foreach (var incoming in added)
    {
      var entry = incoming;

      if (byId.TryGetValue(entry.Id, out var old) && old.Source != entry.Source)
      {
        var tag = entry.Source.StartsWith("VS Code", StringComparison.Ordinal) ? "VS Code"
                  : entry.Source.StartsWith("tinted", StringComparison.Ordinal) ? entry.Source.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1]
                  : entry.Source.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        entry = entry with { Id = $"{entry.Id}-{Palette.Slug(tag)}", Name = $"{entry.Name} ({tag})" };
      }

      byId[entry.Id] = entry;
      output.Add(entry);
    }

    return output;
  }

  private List<ThemeEntry> Existing()
  {
    if (!File.Exists(_userLibraryPath))
      return [];

    try
    {
      return JsonSerializer.Deserialize<List<ThemeEntry>>(File.ReadAllText(_userLibraryPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
    catch (JsonException ex)
    {
      throw new ThemeException($"your theme library file {_userLibraryPath} is damaged ({ex.Message}); fix or delete it, then import again.", ex);
    }
  }

  /// <summary>Writes the user library: one entry per id (the last wins), sorted by name, through a temporary file.</summary>
  internal void SaveUserLibrary(IEnumerable<ThemeEntry> entries)
  {
    var unique = entries.GroupBy(e => e.Id).Select(g => g.Last()).OrderBy(e => e.Name.ToLowerInvariant(), StringComparer.Ordinal).ToList();

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_userLibraryPath))!);

    var temp = _userLibraryPath + ".tmp";

    File.WriteAllText(temp, JsonSerializer.Serialize(unique, Json));
    File.Move(temp, _userLibraryPath, true);
  }
}
