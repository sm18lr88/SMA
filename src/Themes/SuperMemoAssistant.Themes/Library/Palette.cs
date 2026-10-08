// Palette helpers: slugs, and completing a partial set of color roles into a full theme entry with minimum contrast.
using System.Text;
using System.Text.RegularExpressions;
using SuperMemoAssistant.Themes.Colors;

namespace SuperMemoAssistant.Themes;

internal static partial class Palette
{
  public static readonly string[] Roles =
  [
    "bg", "bg_alt", "surface", "border", "selection", "fg", "fg_dim", "muted", "accent", "accent_fg", "alt_fg",
    "surface_fg", "selection_fg", "red", "orange", "yellow", "green", "cyan", "blue", "purple",
  ];

  private static readonly Dictionary<string, Dictionary<string, string>> Ansi = new()
  {
    ["dark"] = new()
    {
      ["red"] = "#F14C4C", ["orange"] = "#E5A050", ["yellow"] = "#E5E510", ["green"] = "#23D18B",
      ["cyan"] = "#29B8DB", ["blue"] = "#3B8EEA", ["purple"] = "#D670D6",
    },
    ["light"] = new()
    {
      ["red"] = "#CD3131", ["orange"] = "#C26A00", ["yellow"] = "#949800", ["green"] = "#00BC00",
      ["cyan"] = "#0598BC", ["blue"] = "#0451A5", ["purple"] = "#BC05BC",
    },
  };

  [GeneratedRegex("[^a-z0-9]+")]
  private static partial Regex NonSlug();

  public static string Slug(string text)
  {
    var ascii = new StringBuilder();

    foreach (var c in text.Normalize(NormalizationForm.FormKD).Where(c => c < 128))
      ascii.Append(c);

    return NonSlug().Replace(ascii.ToString().ToLowerInvariant(), "-").Trim('-');
  }

  /// <summary>Fills missing roles from bg, fg and accent, enforces minimum contrast, and returns a library entry.</summary>
  public static ThemeEntry Complete(IReadOnlyDictionary<string, string?> roles, string name, string source, string? variant = null)
  {
    var c  = roles.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => ColorMath.HexToRgb(kv.Value!));
    var bg = c["bg"];
    var fg = c.TryGetValue("fg", out var givenFg) ? givenFg : ColorMath.BestText(bg);

    variant ??= ColorMath.Luminance(bg) < 0.2 ? "dark" : "light";

    foreach (var (k, v) in Ansi[variant])
      c.TryAdd(k, ColorMath.HexToRgb(v));

    c["fg"] = fg;

    SetDefault(c, "accent", c["blue"]);
    SetDefault(c, "bg_alt", ColorMath.Mix(bg, fg, 0.05));

    if (ColorMath.Contrast(SetDefault(c, "surface", ColorMath.Mix(bg, fg, 0.08)), bg) < 1.06)
      c["surface"] = ColorMath.Mix(bg, fg, 0.08);

    if (ColorMath.Contrast(SetDefault(c, "border", ColorMath.Mix(bg, fg, 0.2)), bg) < 1.25)
      c["border"] = ColorMath.Mix(bg, fg, 0.2);

    if (ColorMath.Contrast(SetDefault(c, "selection", ColorMath.Mix(bg, c["accent"], 0.3)), bg) < 1.15)
      c["selection"] = ColorMath.Mix(bg, c["accent"], 0.35);

    SetDefault(c, "fg_dim", ColorMath.Mix(fg, bg, 0.25));
    SetDefault(c, "muted", ColorMath.Mix(fg, bg, 0.5));

    c["accent_fg"] = ColorMath.Contrast(c["accent"], ColorMath.White) >= 3.8 ? ColorMath.White : ColorMath.BestText(c["accent"], bg, fg);

    foreach (var area in new[] { "bg_alt", "surface", "selection" })
    {
      var key = area == "bg_alt" ? "alt_fg" : $"{area}_fg";

      c[key] = ColorMath.Contrast(c[area], fg) >= 4 ? fg : ColorMath.BestText(c[area], fg, bg);
    }

    return new ThemeEntry(Slug(name), name, variant, source, Roles.ToDictionary(r => r, r => ColorMath.RgbToHex(c[r])));
  }

  private static Rgb SetDefault(Dictionary<string, Rgb> c, string key, Rgb value)
  {
    if (!c.TryGetValue(key, out var existing))
      c[key] = existing = value;

    return existing;
  }
}
