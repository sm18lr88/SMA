// Imports a VS Code color theme (JSON with comments and "include" chains) by mapping workbench colors and syntax hues to roles.
using System.Text.Json;
using SuperMemoAssistant.Themes.Colors;

namespace SuperMemoAssistant.Themes.Import;

internal static class VsCodeImporter
{
  private static readonly (string Role, double Hue)[] Hues =
    [("red", 0), ("orange", 30), ("yellow", 55), ("green", 120), ("cyan", 180), ("blue", 215), ("purple", 285)];

  private static readonly Dictionary<string, Dictionary<string, string>> Defaults = new()
  {
    ["dark"] = new()
    {
      ["editor.background"] = "#1E1E1E", ["editor.foreground"] = "#D4D4D4", ["sideBar.background"] = "#252526",
      ["input.background"] = "#3C3C3C", ["panel.border"] = "#80808059", ["editor.selectionBackground"] = "#264F78",
      ["button.background"] = "#0E639C", ["descriptionForeground"] = "#CCCCCCB3", ["editorLineNumber.foreground"] = "#858585",
    },
    ["light"] = new()
    {
      ["editor.background"] = "#FFFFFF", ["editor.foreground"] = "#000000", ["sideBar.background"] = "#F3F3F3",
      ["input.background"] = "#FFFFFF", ["panel.border"] = "#80808059", ["editor.selectionBackground"] = "#ADD6FF",
      ["button.background"] = "#007ACC", ["descriptionForeground"] = "#717171", ["editorLineNumber.foreground"] = "#237893",
    },
  };

  private static readonly (string Role, string[] Keys)[] Pick =
  [
    ("bg", ["editor.background"]),
    ("bg_alt", ["titleBar.activeBackground", "sideBar.background", "activityBar.background"]),
    ("surface", ["input.background", "dropdown.background", "button.secondaryBackground"]),
    ("border", ["panel.border", "sideBar.border", "editorGroup.border", "contrastBorder"]),
    ("selection", ["editor.selectionBackground", "list.activeSelectionBackground"]),
    ("fg", ["editor.foreground", "foreground"]),
    ("fg_dim", ["descriptionForeground", "foreground"]),
    ("muted", ["editorLineNumber.foreground"]),
    ("accent", ["button.background", "focusBorder", "activityBarBadge.background"]),
  ];

  private static readonly (string Role, string Key)[] AnsiKeys =
  [
    ("red", "terminal.ansiRed"), ("yellow", "terminal.ansiYellow"), ("green", "terminal.ansiGreen"),
    ("cyan", "terminal.ansiCyan"), ("blue", "terminal.ansiBlue"), ("purple", "terminal.ansiMagenta"),
  ];

  private sealed record Loaded(Dictionary<string, string> Colors, List<JsonElement> Tokens, string? Kind);

  /// <summary>Reads a theme and the themes it includes. Later colors win, and token colors accumulate.</summary>
  private static Loaded Load(string path)
  {
    var data   = Jsonc.Parse(Jsonc.ReadFile(path));
    var loaded = Jsonc.String(data, "include") is { Length: > 0 } include
      ? Load(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, include))
      : new Loaded([], [], null);

    if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("colors", out var colors) && colors.ValueKind == JsonValueKind.Object)
    {
      foreach (var property in colors.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.String))
        loaded.Colors[property.Name] = property.Value.GetString()!;
    }

    if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("tokenColors", out var tokens) && tokens.ValueKind == JsonValueKind.Array)
      loaded.Tokens.AddRange(tokens.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.Object));

    var kind = Jsonc.String(data, "type");

    return loaded with { Kind = string.IsNullOrEmpty(kind) ? loaded.Kind : kind };
  }

  public static ThemeEntry Import(string path, string name, string uiTheme = "vs-dark")
  {
    var loaded  = Load(path);
    var kind    = (string.IsNullOrEmpty(loaded.Kind) ? uiTheme : loaded.Kind).ToLowerInvariant();
    var variant = kind is "light" or "vs" or "hc-light" ? "light" : "dark";
    var merged  = new Dictionary<string, string>(Defaults[variant]);

    foreach (var (key, value) in loaded.Colors)
      merged[key] = value;

    var bg = ColorMath.HexToRgb(merged["editor.background"][..Math.Min(7, merged["editor.background"].Length)]);

    string? ColorOf(string key)
    {
      var parsed = merged.TryGetValue(key, out var raw) ? CssColor.Parse(raw) : null;

      return parsed is { } color ? ColorMath.RgbToHex(ColorMath.Over(color, bg)) : null;
    }

    var roles = new Dictionary<string, string?>();

    foreach (var (role, keys) in Pick)
    {
      if (keys.Select(ColorOf).FirstOrDefault(c => c is not null) is { } picked)
        roles[role] = picked;
    }

    foreach (var (role, key) in AnsiKeys)
    {
      if (ColorOf(key) is { } ansi)
        roles[role] = ansi;
    }

    var syntax = loaded.Tokens.Select(ForegroundOf)
                       .Where(c => c is { } rgba && ColorMath.ToHls(rgba.Rgb).S > 0.25)
                       .Select(c => c!.Value.Rgb)
                       .ToList();

    foreach (var (role, hue) in Hues)
    {
      Rgb? best     = null;
      var  bestDist = double.MaxValue;

      foreach (var candidate in syntax)
      {
        var distance = ColorMath.HueDistance(ColorMath.ToHls(candidate).H * 360, hue);

        if (distance < bestDist)
        {
          best     = candidate;
          bestDist = distance;
        }
      }

      if (!roles.ContainsKey(role) && best is { } near && bestDist < 25)
        roles[role] = ColorMath.RgbToHex(near);
    }

    return Palette.Complete(roles, name, "VS Code", variant);
  }

  private static Rgba? ForegroundOf(JsonElement token)
  {
    if (!token.TryGetProperty("settings", out var settings) || Jsonc.String(settings, "foreground") is not { } foreground)
      return null;

    return CssColor.Parse(foreground);
  }
}
