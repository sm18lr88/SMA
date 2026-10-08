// Imports an Obsidian theme folder (theme.css) as one library entry per mode and color flavor.
using System.Text;
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes.Import;

internal static partial class ObsidianImporter
{
  [GeneratedRegex(@"^(theme-(dark|light)|is-.*|mod-.*|.*-toggle|.*-enable|print|anp-print)$")]
  private static partial Regex NotFlavor();

  public static List<ThemeEntry> Import(string themeFolder, string appCss)
  {
    var name    = Path.GetFileName(themeFolder.TrimEnd('\\', '/'));
    var themeCss = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(themeFolder, "theme.css")));
    var cascade = new ObsidianCascade(appCss, themeCss);
    var rules   = cascade.Rules.Where(r => r.Sheet == 1).ToList();
    var flavors = rules.SelectMany(r => r.Classes)
                       .Where(c => !NotFlavor().IsMatch(c))
                       .Where(c => rules.Any(r => r.Classes.Contains(c) && (r.Classes.Contains("theme-dark") || r.Classes.Contains("theme-light"))))
                       .Distinct()
                       .Order(StringComparer.Ordinal)
                       .ToList();

    var results = new List<ThemeEntry>();
    var seen    = new HashSet<string>();

    foreach (var mode in new[] { "dark", "light" })
    {
      if (!rules.Any(r => r.Classes.Contains($"theme-{mode}")))
        continue;

      Dictionary<string, string>? defaultRoles = null;

      foreach (var flavor in new string?[] { null }.Concat(flavors))
      {
        var context = new HashSet<string> { $"theme-{mode}" };

        if (flavor is not null)
          context.Add(flavor);

        if (flavor is not null && !rules.Any(r => r.Classes.Contains(flavor) && r.Classes.IsSubsetOf(context)))
          continue;

        var roles = ObsidianPalette.Roles(cascade.Variables(context), mode == "dark" ? "#1E1E1E" : "#FFFFFF");
        var key   = string.Join("\n", roles.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));

        if (seen.Contains(key) || (flavor is not null && defaultRoles is not null && SameCore(roles, defaultRoles)))
          continue; // a layout toggle, not a color flavor

        seen.Add(key);
        defaultRoles ??= roles;

        var label = flavor is null ? name : $"{name} {PyTitle(flavor.Replace('-', ' '))}";

        results.Add(Palette.Complete(ToNullable(roles), $"{label} ({mode})", $"Obsidian theme {name}", mode));
      }
    }

    return results;
  }

  private static bool SameCore(Dictionary<string, string> roles, Dictionary<string, string> reference) =>
    roles.GetValueOrDefault("bg") == reference["bg"] && roles.GetValueOrDefault("fg") == reference.GetValueOrDefault("fg");

  private static Dictionary<string, string?> ToNullable(Dictionary<string, string> roles) => roles.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);

  /// <summary>Python's str.title(): the first letter of every run of letters is upper case, the rest lower case.</summary>
  internal static string PyTitle(string text)
  {
    var output   = new StringBuilder();
    var previous = false;

    foreach (var ch in text)
    {
      var cased = IsCased(ch);

      output.Append(cased ? (previous ? char.ToLowerInvariant(ch) : char.ToUpperInvariant(ch)) : ch);
      previous = cased;
    }

    return output.ToString();
  }

  private static bool IsCased(char ch) =>
    char.GetUnicodeCategory(ch) is System.Globalization.UnicodeCategory.UppercaseLetter
                                or System.Globalization.UnicodeCategory.LowercaseLetter
                                or System.Globalization.UnicodeCategory.TitlecaseLetter;
}
