// Plans the file changes that make cards and settings match a theme: card stylesheets, per-style stylesheets, and the ini keys that pick the window style.
using SuperMemoAssistant.Themes.Cards;
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Styles;

namespace SuperMemoAssistant.Themes;

internal static class CardSync
{
  private const string InvalidStyleFileChars = "<>:\"/\\|?*";

  /// <summary>SuperMemo's default value for Theme: the native Windows look, which is not a style inside sm20.exe.</summary>
  private const string NativeStyle = "Windows";

  internal sealed record Active(string Light, string Dark, bool DarkMode);

  public static Active ActiveStyles(SuperMemoInstall install)
  {
    var collection = ReadIni(Path.Combine(install.CollectionFolder, "collection.ini"));
    var supermemo  = ReadIni(Path.Combine(install.BinFolder, "supermemo.ini"));

    return new Active(collection.Get("Defaults", "Theme"), collection.Get("Defaults", "Dark Theme"), supermemo.Get("SuperMemo", "Dark mode", "0") == "1");
  }

  private static IniText ReadIni(string path) => new(File.Exists(path) ? Ansi.ReadFile(path) : "");

  /// <summary>Library palette for an installed style, or a palette read from a built-in style. Null when no installed style has that name.</summary>
  public static ThemeEntry? EntryFor(ThemeLibrary library, IReadOnlyDictionary<string, byte[]> styles, string style)
  {
    var byResource = library.Entries.GroupBy(StyleNames.ResourceName).ToDictionary(g => g.Key, g => g.Last());

    foreach (var (resource, blob) in styles)
    {
      if (string.Equals(VsfFile.StyleName(blob), style, StringComparison.OrdinalIgnoreCase)) // Delphi compares style names without case
        return byResource.GetValueOrDefault(resource) ?? StyleNames.EntryFromStyle(blob);
    }

    return null;
  }

  private static string BaseCss(SuperMemoInstall install, string variant)
  {
    var file = Path.Combine(install.BinFolder, variant == "dark" ? "DarkMode.css" : "LightMode.css");

    return File.Exists(file) ? Ansi.ReadFile(file) : "";
  }

  /// <summary>Card rules for one slot; <paramref name="live" /> also updates supermemo.css, the active stylesheet.</summary>
  public static Dictionary<string, byte[]> PlanCards(SuperMemoInstall install, string variant, IReadOnlyDictionary<string, string> roles, bool live)
  {
    var files = new List<string> { Path.Combine(install.BinFolder, variant == "dark" ? "DarkMode.css" : "LightMode.css") };

    if (live)
      files.Add(Path.Combine(install.BinFolder, "supermemo.css"));

    var rules = CardRules.For(roles);

    return files.ToDictionary(f => f, f => Ansi.Encode(CardRules.Apply(File.Exists(f) ? Ansi.ReadFile(f) : "", rules), f), StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>The ini edits that select <paramref name="style" /> in its slot and switch dark mode to match.</summary>
  public static Dictionary<string, byte[]> PlanSettings(SuperMemoInstall install, string style, string variant)
  {
    var collectionIni = Path.Combine(install.CollectionFolder, "collection.ini");
    var supermemoIni  = Path.Combine(install.BinFolder, "supermemo.ini");

    if (!File.Exists(collectionIni) || !File.Exists(supermemoIni))
      throw new ThemeException($"{(File.Exists(collectionIni) ? supermemoIni : collectionIni)} was not found; SuperMemo creates it when it first runs.");

    var collection = IniText.Set(Ansi.ReadFile(collectionIni), "Defaults", variant == "dark" ? "Dark Theme" : "Theme", style);
    var general    = IniText.Set(Ansi.ReadFile(supermemoIni), "SuperMemo", "Dark mode", variant == "dark" ? "1" : "0");

    return new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
    {
      [collectionIni] = Ansi.Encode(collection, collectionIni),
      [supermemoIni]  = Ansi.Encode(general, supermemoIni),
    };
  }

  /// <summary>bin\themes\&lt;style&gt;.css for every style in sm20.exe; the live-theme patch loads these.</summary>
  private static Dictionary<string, byte[]> PlanThemeStylesheets(SuperMemoInstall install, ThemeLibrary library, IReadOnlyDictionary<string, byte[]> styles)
  {
    var changes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    foreach (var blob in styles.Values)
    {
      var name = VsfFile.StyleName(blob);

      if (name.Any(c => InvalidStyleFileChars.Contains(c)) || EntryFor(library, styles, name) is not { } entry)
        continue;

      var path = Path.Combine(install.BinFolder, "themes", $"{name}.css");
      var css  = CardRules.Apply(BaseCss(install, entry.Variant), CardRules.For(entry.Roles));

      changes[path] = Ansi.Encode(css, path);
    }

    return changes;
  }

  /// <summary>Writes the per-style stylesheets, then recolors the cards of the light and dark slots from the styles that are active now.</summary>
  public static void Sync(SuperMemoInstall install, ThemeLibrary library, SafeWriter writer, ReportBuilder report)
  {
    var styles  = ExeResources.ReadStyles(install.ExePath);
    var written = writer.Write(PlanThemeStylesheets(install, library, styles), "theme-stylesheets");
    var active  = ActiveStyles(install);
    var cards   = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    foreach (var mode in new[] { "light", "dark" })
    {
      var style = mode == "light" ? active.Light : active.Dark;

      if (style.Length == 0 || string.Equals(style, NativeStyle, StringComparison.OrdinalIgnoreCase))
        continue; // no theme chosen for this slot, or the native Windows look: SuperMemo's own colors stay

      if (EntryFor(library, styles, style) is not { } entry)
      {
        report.Warn($"{mode} style '{style}' is not in sm20.exe; {mode}-mode cards were left unchanged.");

        continue;
      }

      foreach (var (path, data) in PlanCards(install, mode, entry.Roles, mode == (active.DarkMode ? "dark" : "light")))
        cards[path] = data;
    }

    written += writer.Write(cards, "sync");

    if (written > 0)
      report.Did($"Updated {written} card stylesheet file(s).");
  }
}
