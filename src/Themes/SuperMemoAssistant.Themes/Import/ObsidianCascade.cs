// Simulates the CSS custom-property cascade on <body> for an Obsidian theme, and turns the result into palette roles.
using System.Collections.Immutable;
using SuperMemoAssistant.Themes.Colors;

namespace SuperMemoAssistant.Themes.Import;

/// <summary>One rule that sets custom properties: its weight, its place, the body classes it needs, and what it sets.</summary>
internal sealed record CascadeRule(int Specificity, int Order, HashSet<string> Classes, Dictionary<string, string> Declarations, int Sheet);

internal sealed class ObsidianCascade
{
  public ObsidianCascade(params string[] sheets)
  {
    for (var sheetNo = 0; sheetNo < sheets.Length; sheetNo++)
    {
      foreach (var (selector, body) in CssSheet.Rules(sheets[sheetNo]))
      {
        var declarations = CssSheet.CustomProperties(body);

        if (declarations.Count == 0)
          continue;

        foreach (var part in CssSheet.SplitTop(selector))
        {
          if (CssSheet.BodyClasses(part) is not { } classes)
            continue;

          var lead = part.Trim().Length > 0 ? part.Trim()[0] : '\0';

          Rules.Add(new CascadeRule(classes.Count + (lead is 'b' or 'h' or ':' ? 1 : 0), Rules.Count, classes, declarations, sheetNo));
        }
      }
    }
  }

  public List<CascadeRule> Rules { get; } = [];

  /// <summary>The custom properties that apply to a body that has all of <paramref name="context" /> as classes.</summary>
  public Dictionary<string, string> Variables(HashSet<string> context)
  {
    var output = new Dictionary<string, string>();

    foreach (var rule in Rules.OrderBy(r => r.Specificity).ThenBy(r => r.Order))
    {
      if (rule.Classes.IsSubsetOf(context))
      {
        foreach (var (name, value) in rule.Declarations)
          output[name] = value;
      }
    }

    return output;
  }
}

internal static class ObsidianPalette
{
  private static readonly (string Role, string[] Variables)[] RoleVariables =
  [
    ("bg", ["--background-primary"]),
    ("bg_alt", ["--titlebar-background", "--background-secondary", "--background-secondary-alt"]),
    ("surface", ["--interactive-normal", "--background-modifier-form-field", "--background-primary-alt"]),
    ("border", ["--background-modifier-border"]),
    ("selection", ["--text-selection"]),
    ("fg", ["--text-normal"]),
    ("fg_dim", ["--text-muted"]),
    ("muted", ["--text-faint"]),
    ("accent", ["--interactive-accent", "--color-accent"]),
    ("red", ["--color-red"]),
    ("orange", ["--color-orange"]),
    ("yellow", ["--color-yellow"]),
    ("green", ["--color-green"]),
    ("cyan", ["--color-cyan"]),
    ("blue", ["--color-blue"]),
    ("purple", ["--color-purple"]),
  ];

  /// <summary>Replaces var(--name, fallback) references, the last one first, up to a fixed depth. A variable that refers to itself, directly or through others, counts as unset.</summary>
  public static string Resolve(string value, IReadOnlyDictionary<string, string> env, int depth = 0, ImmutableHashSet<string>? active = null)
  {
    active ??= [];

    if (depth > 40 || !value.Contains("var(", StringComparison.Ordinal))
      return value;

    var i     = value.LastIndexOf("var(", StringComparison.Ordinal);
    var j     = i + 4;
    var level = 1;

    while (j < value.Length && level != 0)
    {
      level += (value[j] == '(' ? 1 : 0) - (value[j] == ')' ? 1 : 0);
      j++;
    }

    var inner    = j - 1 > i + 4 ? value.Substring(i + 4, j - 1 - (i + 4)) : "";
    var parts    = CssSheet.SplitTop(inner);
    var name     = parts[0].Trim();
    var fallback = parts.Skip(1).ToList();
    var unset    = fallback.Count > 0 ? string.Join(",", fallback) : "";
    var repl     = env.TryGetValue(name, out var found) && !active.Contains(name) ? found : unset;
    var expanded = Resolve(repl, env, depth + 1, active.Add(name));

    return Resolve(value[..i] + expanded + value[j..], env, depth + 1, active);
  }

  public static Dictionary<string, string> Roles(IReadOnlyDictionary<string, string> env, string baseBg)
  {
    var baseColor = ColorMath.HexToRgb(baseBg);
    var bgAny     = CssColor.Parse(Resolve(env.GetValueOrDefault("--background-primary", baseBg), env)) ?? ColorMath.WithAlpha(baseColor, 1.0);
    var bg        = ColorMath.Over(bgAny, baseColor);
    var roles     = new Dictionary<string, string>();

    foreach (var (role, variables) in RoleVariables)
    {
      foreach (var name in variables)
      {
        var parsed = env.TryGetValue(name, out var raw) ? CssColor.Parse(Resolve(raw, env)) : null;

        if (parsed is { A: > 0.02 } color)
        {
          roles[role] = ColorMath.RgbToHex(ColorMath.Over(color, bg));

          break;
        }
      }
    }

    roles["bg"] = ColorMath.RgbToHex(bg);

    return roles;
  }
}
