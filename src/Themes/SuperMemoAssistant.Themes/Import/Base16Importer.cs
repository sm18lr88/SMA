// Imports a tinted-theming base16 or base24 scheme (YAML) by mapping its sixteen colors to roles.
using System.Text;
using System.Text.RegularExpressions;
using SuperMemoAssistant.Themes.Colors;

namespace SuperMemoAssistant.Themes.Import;

internal static partial class Base16Importer
{
  [GeneratedRegex(@"^(\w+):\s*""?([^""\n#]*)""?", RegexOptions.Multiline)]
  private static partial Regex MetaLine();

  [GeneratedRegex(@"^\s+(base[0-9A-Fa-f]{2}):\s*""?(#?[0-9A-Fa-f]{6})", RegexOptions.Multiline)]
  private static partial Regex PaletteLine();

  private static readonly UTF8Encoding Strict = new(false, true);

  public static ThemeEntry Import(string path)
  {
    var text = Strict.GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n").Replace('\r', '\n');
    var meta = new Dictionary<string, string>();

    foreach (Match m in MetaLine().Matches(text))
      meta[m.Groups[1].Value] = m.Groups[2].Value;

    var palette = new Dictionary<string, string>();

    foreach (Match m in PaletteLine().Matches(text))
    {
      var key = m.Groups[1].Value;

      palette[key[..4] + key[4..].ToUpperInvariant()] = "#" + m.Groups[2].Value.TrimStart('#');
    }

    string HexOf(string key) => palette.TryGetValue(key, out var value) ? value : throw new FormatException($"missing color {key}");
    Rgb    RgbOf(string key) => ColorMath.HexToRgb(HexOf(key));

    var roles = new Dictionary<string, string?>
    {
      ["bg"]        = HexOf("base00"),
      ["bg_alt"]    = HexOf("base01"),
      ["surface"]   = ColorMath.RgbToHex(ColorMath.Mix(RgbOf("base00"), RgbOf("base02"), 0.6)),
      ["border"]    = ColorMath.RgbToHex(ColorMath.Mix(RgbOf("base02"), RgbOf("base03"), 0.5)),
      ["selection"] = HexOf("base02"),
      ["fg"]        = HexOf("base05"),
      ["fg_dim"]    = HexOf("base04"),
      ["muted"]     = HexOf("base03"),
      ["accent"]    = HexOf("base0D"),
      ["red"]       = HexOf("base08"),
      ["orange"]    = HexOf("base09"),
      ["yellow"]    = HexOf("base0A"),
      ["green"]     = HexOf("base0B"),
      ["cyan"]      = HexOf("base0C"),
      ["blue"]      = HexOf("base0D"),
      ["purple"]    = HexOf("base0E"),
    };

    var name    = meta.TryGetValue("name", out var n) ? n.Trim() : throw new FormatException("missing name");
    var system  = meta.TryGetValue("system", out var s) ? s.Trim() : "base16";
    var variant = meta.TryGetValue("variant", out var v) ? v.Trim() : "";

    if (variant is not ("" or "dark" or "light"))
      throw new FormatException($"unknown variant '{variant}'");

    return Palette.Complete(roles, name, $"tinted-theming {system}", variant.Length == 0 ? null : variant);
  }
}
