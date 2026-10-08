// Recolors a VCL style from a theme palette: bitmap pixels, style colors and system colors.
using System.Globalization;
using SuperMemoAssistant.Themes.Colors;

namespace SuperMemoAssistant.Themes.Styles;

internal static class VsfRecolor
{
  /// <summary>Base gray level to palette role, measured from Windows10 / Windows10 Dark.</summary>
  private static readonly Dictionary<string, (int Level, string Role)[]> GrayAnchors = new()
  {
    ["dark"] =
    [
      (0x00, "bg"), (0x15, "chrome"), (0x29, "control"), (0x3F, "border"),
      (0x5A, "selection"), (0x80, "muted"), (0xCF, "fg_dim"), (0xFF, "fg"),
    ],
    ["light"] =
    [
      (0x00, "fg"), (0x26, "fg"), (0x80, "muted"), (0xB8, "border"),
      (0xCC, "control"), (0xE6, "surface"), (0xF3, "chrome"), (0xFF, "bg"),
    ],
  };

  private static readonly (double H, double L, double S) AccentRef = ColorMath.ToHls(new Rgb(0x00, 0x75, 0xDA));
  private static readonly (double H, double L, double S) RedRef    = ColorMath.ToHls(new Rgb(0xE3, 0x41, 0x3E));

  private static readonly Dictionary<string, string> StyleColors = new()
  {
    ["Border"] = "border", ["Splitter"] = "border", ["FocusEffect"] = "accent",
    ["AlternatingRowBackground"] = "alt_row", ["ButtonPressed"] = "selection", ["ComboBox"] = "surface",
    ["ButtonNormal"] = "control", ["ButtonHot"] = "control", ["ButtonFocused"] = "control",
  };

  private static readonly Dictionary<string, string> SysColors = new()
  {
    ["clActiveBorder"] = "border", ["clActiveCaption"] = "chrome", ["clBtnFace"] = "bg",
    ["clBtnHighlight"] = "border", ["clBtnShadow"] = "border", ["clBtnText"] = "fg",
    ["clCaptionText"] = "chrome_fg", ["clGrayText"] = "muted", ["clHighlight"] = "accent",
    ["clHighlightText"] = "accent_fg", ["clInactiveBorder"] = "muted", ["clInactiveCaption"] = "chrome",
    ["clInactiveCaptionText"] = "muted", ["clInfoBk"] = "chrome", ["clInfoText"] = "chrome_fg",
    ["clMenu"] = "chrome", ["clMenuText"] = "chrome_fg", ["clScrollBar"] = "chrome",
    ["cl3DDkShadow"] = "shadow", ["cl3DLight"] = "border", ["clWindow"] = "bg",
    ["clWindowFrame"] = "border", ["clWindowText"] = "fg",
  };

  private static readonly string[] ChromeText = ["Caption", "SmCaption", "MenuItem", "PopupMenuItem", "ToolItem", "StatusPanel"];

  private static readonly string[] SelectedLists = ["List", "Tree", "Grid", "ComboBoxItem", "EditBox"];

  /// <summary>Recolors <paramref name="baseStyle" /> (the built-in Windows10 or Windows10 Dark style) with the palette.</summary>
  public static Vsf Recolor(Vsf baseStyle, IReadOnlyDictionary<string, string> roles, string name, string variant)
  {
    var pal     = BuildPalette(roles, variant);
    var anchors = GrayAnchors[variant];
    var output  = new Vsf([name, baseStyle.Meta[1], "smcards palette theme", "", ""], baseStyle.Pre, baseStyle.Tag);

    output.Objects.AddRange(baseStyle.Objects);

    foreach (var b in baseStyle.Bitmaps)
      output.Bitmaps.Add(new Bitmap(b.Name, b.Width, b.Height, RecolorPixels(b.Pixels, pal, anchors), b.Trailer));

    foreach (var item in baseStyle.Tail)
      output.Tail.Add(item.IsPair ? TailItem.Pair(item.Key!, Recolor(item.Key!, item.Value!, pal)) : item);

    return output;
  }

  private static string Recolor(string key, string value, Dictionary<string, Rgb> pal)
  {
    var c = pal[RoleFor(key, value)];

    if (value.Contains(','))
    {
      var font = value.Split(',');

      return string.Join(',', font.Take(3).Concat([c.R.ToString(CultureInfo.InvariantCulture), c.G.ToString(CultureInfo.InvariantCulture), c.B.ToString(CultureInfo.InvariantCulture)]));
    }

    return $"$00{c.B:X2}{c.G:X2}{c.R:X2}";
  }

  private static Rgb ReadableOn(Rgb back, params Rgb[] candidates)
  {
    foreach (var t in candidates)
    {
      if (ColorMath.Contrast(back, t) >= 4.5)
        return t;
    }

    return ColorMath.BestText(back, candidates);
  }

  private static Rgb Soften(Rgb color, Rgb bg, double maxContrast)
  {
    var t = 0.0;

    while (ColorMath.Contrast(ColorMath.Mix(color, bg, t), bg) > maxContrast && t < 1)
      t += 0.05;

    return ColorMath.Mix(color, bg, t);
  }

  internal static Dictionary<string, Rgb> BuildPalette(IReadOnlyDictionary<string, string> roles, string variant)
  {
    var c = roles.ToDictionary(kv => kv.Key, kv => ColorMath.HexToRgb(kv.Value));

    if (variant == "light")
    {
      c["surface"] = Soften(c["surface"], c["bg"], 1.45);
      c["border"]  = Soften(c["border"], c["bg"], 2.4);
    }

    c["chrome"]    = ColorMath.Contrast(c["bg_alt"], c["bg"]) <= 1.6 ? c["bg_alt"] : ColorMath.Mix(c["bg"], c["fg"], 0.06);
    c["chrome_fg"] = ReadableOn(c["chrome"], c["fg"], c["alt_fg"]);
    c["control"]   = variant == "dark" ? c["surface"] : ColorMath.Mix(c["surface"], c["border"], 0.35);

    if (Math.Min(ColorMath.Contrast(c["control"], c["bg"]), ColorMath.Contrast(c["control"], c["chrome"])) < 1.12)
      c["control"] = ColorMath.Mix(c["chrome"], c["fg"], 0.12);

    c["control_fg"] = ReadableOn(c["control"], c["fg"], c["surface_fg"]);
    c["alt_row"]    = ColorMath.Mix(c["bg"], c["fg"], 0.04);
    c["shadow"]     = ColorMath.Mix(c["bg"], ColorMath.Black, 0.35);

    return c;
  }

  private static byte[] MapPixel(ReadOnlySpan<byte> px, Dictionary<string, Rgb> pal, (int Level, string Role)[] anchors)
  {
    int b = px[0], g = px[1], r = px[2], a = px[3];

    if (a == 0)
      return px.ToArray();

    var hi = Math.Max(r, Math.Max(g, b));
    var lo = Math.Min(r, Math.Min(g, b));

    if (hi - lo < 24)
    {
      var v = (r + g + b) / 3.0;

      for (var i = 0; i + 1 < anchors.Length; i++)
      {
        var (v0, r0) = anchors[i];
        var (v1, r1) = anchors[i + 1];

        if (v0 <= v && v <= v1)
        {
          var t  = v1 > v0 ? (v - v0) / (v1 - v0) : 0;
          var nc = ColorMath.Mix(pal[r0], pal[r1], t);

          return [(byte)nc.B, (byte)nc.G, (byte)nc.R, (byte)a];
        }
      }

      return px.ToArray();
    }

    var (h, light, _) = ColorMath.ToHls(new Rgb(r, g, b));
    var isRed         = h < 0.06 || h > 0.94;
    var reference     = isRed ? RedRef : AccentRef;
    var (th, tl, ts)  = ColorMath.ToHls(pal[isRed ? "red" : "accent"]);
    var mapped        = ColorMath.FromHls(th, tl + (light - reference.L), ts);

    return [(byte)mapped.B, (byte)mapped.G, (byte)mapped.R, (byte)a];
  }

  private static byte[] RecolorPixels(byte[] pixels, Dictionary<string, Rgb> pal, (int Level, string Role)[] anchors)
  {
    var cache = new Dictionary<uint, byte[]>();
    var output = new byte[pixels.Length];

    for (var i = 0; i + 3 < pixels.Length; i += 4)
    {
      var key = BitConverter.ToUInt32(pixels, i);

      if (!cache.TryGetValue(key, out var mapped))
        cache[key] = mapped = MapPixel(pixels.AsSpan(i, 4), pal, anchors);

      Buffer.BlockCopy(mapped, 0, output, i, 4);
    }

    return output;
  }

  internal static string RoleFor(string key, string value)
  {
    if (SysColors.TryGetValue(key, out var sys))
      return sys;

    if (StyleColors.TryGetValue(key, out var style))
      return style;

    if (!value.Contains(','))
    {
      var chrome = key.Contains("Disabled", StringComparison.Ordinal) || key.Contains("Gradient", StringComparison.Ordinal)
                   || key.StartsWith("Category", StringComparison.Ordinal) || key.StartsWith("Hint", StringComparison.Ordinal)
                   || key.StartsWith("ToolBar", StringComparison.Ordinal);

      return chrome ? "chrome" : "bg";
    }

    if (key.Contains("Disabled", StringComparison.Ordinal))
      return "muted";

    if (key.Contains("Drag", StringComparison.Ordinal))
      return "accent";

    if (key.Contains("Selected", StringComparison.Ordinal) && SelectedLists.Any(p => key.StartsWith(p, StringComparison.Ordinal)))
    {
      var parts = value.Split(',').Skip(3).Take(3).Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();

      return parts.Sum() > 384 ? "accent_fg" : "fg";
    }

    if (ChromeText.Any(p => key.StartsWith(p, StringComparison.Ordinal)))
      return "chrome_fg";

    if (key.StartsWith("ButtonText", StringComparison.Ordinal))
      return "control_fg";

    return key.StartsWith("TabTextInactive", StringComparison.Ordinal) ? "fg_dim" : "fg";
  }
}
