// sRGB color math for theme building: hex conversion, mixing, contrast, HLS. Rounding and modulo follow Python so results match the reference implementation.
namespace SuperMemoAssistant.Themes.Colors;

internal readonly record struct Rgb(int R, int G, int B)
{
  public double[] Channels => [R, G, B];
}

internal readonly record struct Rgba(int R, int G, int B, double A)
{
  public Rgb Rgb => new(R, G, B);
}

internal static class ColorMath
{
  public static readonly Rgb White = new(255, 255, 255);
  public static readonly Rgb Black = new(0, 0, 0);

  /// <summary>Python's float modulo: the result has the sign of the divisor.</summary>
  internal static double PyMod(double x, double y)
  {
    var r = x % y;

    return r != 0 && (r < 0) != (y < 0) ? r + y : r;
  }

  /// <summary>Parses "#rgb", "#rgba", "#rrggbb" or "#rrggbbaa" (alpha is ignored). Longer or odd forms read the first three byte pairs.</summary>
  public static Rgb HexToRgb(string text)
  {
    var h = text.Trim().TrimStart('#');

    if (h.Length is 3 or 4)
      h = string.Concat(h.Select(c => $"{c}{c}"));

    return new Rgb(ParseHex(h, 0, 2), ParseHex(h, 2, 2), ParseHex(h, 4, 2));
  }

  /// <summary>Hex value of a slice, as Python's int(h[a:b], 16) reads it (a short slice is read as it is).</summary>
  internal static int ParseHex(string h, int start, int length)
  {
    var slice = start >= h.Length ? string.Empty : h.Substring(start, Math.Min(length, h.Length - start));

    return int.Parse(slice, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture);
  }

  public static string RgbToHex(Rgb c) => $"#{Clamp(c.R):X2}{Clamp(c.G):X2}{Clamp(c.B):X2}";

  private static int Clamp(int v) => Math.Max(0, Math.Min(255, v));

  /// <summary>t=0 gives a, t=1 gives b.</summary>
  public static Rgb Mix(Rgb a, Rgb b, double t)
  {
    int Step(int x, int y) => (int)Math.Round(x + (y - x) * t, MidpointRounding.ToEven);

    return new Rgb(Step(a.R, b.R), Step(a.G, b.G), Step(a.B, b.B));
  }

  public static Rgba WithAlpha(Rgb rgb, double alpha) => new(rgb.R, rgb.G, rgb.B, alpha);

  /// <summary>Composites a translucent color over an opaque background.</summary>
  public static Rgb Over(Rgba c, Rgb bg) => Mix(bg, c.Rgb, c.A);

  public static double Luminance(Rgb c)
  {
    static double Channel(int value)
    {
      var v = value / 255.0;

      return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
  }

  public static double Contrast(Rgb a, Rgb b)
  {
    var la = Luminance(a);
    var lb = Luminance(b);

    if (la < lb)
      (la, lb) = (lb, la);

    return (la + 0.05) / (lb + 0.05);
  }

  /// <summary>The most readable of the candidates, white and black. Candidates get a 0.5 bonus; the first best option wins ties.</summary>
  public static Rgb BestText(Rgb bg, params Rgb[] candidates)
  {
    var options = candidates.Concat([White, Black]).ToArray();
    var best    = options[0];
    var bestKey = double.NegativeInfinity;

    foreach (var option in options)
    {
      var key = Contrast(bg, option) + (candidates.Contains(option) ? 0.5 : 0);

      if (key > bestKey)
      {
        best    = option;
        bestKey = key;
      }
    }

    return best;
  }

  public static (double H, double L, double S) ToHls(Rgb c)
  {
    double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
    var    max = Math.Max(r, Math.Max(g, b));
    var    min = Math.Min(r, Math.Min(g, b));
    var    sum = max + min;
    var    range = max - min;
    var    l = sum / 2.0;

    if (min == max)
      return (0.0, l, 0.0);

    var s  = l <= 0.5 ? range / sum : range / (2.0 - max - min);
    var rc = (max - r) / range;
    var gc = (max - g) / range;
    var bc = (max - b) / range;
    var h  = r == max ? bc - gc : g == max ? 2.0 + rc - bc : 4.0 + gc - rc;

    return (PyMod(h / 6.0, 1.0), l, s);
  }

  public static Rgb FromHls(double h, double light, double s)
  {
    light = Math.Max(0.0, Math.Min(1.0, light));
    s     = Math.Max(0.0, Math.Min(1.0, s));
    h     = PyMod(h, 1.0);

    double r, g, b;

    if (s == 0.0)
    {
      r = g = b = light;
    }
    else
    {
      var m2 = light <= 0.5 ? light * (1.0 + s) : light + s - light * s;
      var m1 = 2.0 * light - m2;

      r = HlsValue(m1, m2, h + 1.0 / 3.0);
      g = HlsValue(m1, m2, h);
      b = HlsValue(m1, m2, h - 1.0 / 3.0);
    }

    return new Rgb(Round(r * 255), Round(g * 255), Round(b * 255));
  }

  private static int Round(double v) => (int)Math.Round(v, MidpointRounding.ToEven);

  private static double HlsValue(double m1, double m2, double hue)
  {
    hue = PyMod(hue, 1.0);

    if (hue < 1.0 / 6.0)
      return m1 + (m2 - m1) * hue * 6.0;

    if (hue < 0.5)
      return m2;

    if (hue < 2.0 / 3.0)
      return m1 + (m2 - m1) * (2.0 / 3.0 - hue) * 6.0;

    return m1;
  }

  /// <summary>Distance between two hues in degrees (0 to 180).</summary>
  public static double HueDistance(double a, double b)
  {
    var d = PyMod(Math.Abs(a - b), 360);

    return Math.Min(d, 360 - d);
  }
}
