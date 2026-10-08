// Parses CSS color values that have no var() left: names, hex, rgb()/hsl() with calc() arithmetic, and color-mix().
using System.Globalization;
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes.Colors;

internal static partial class CssColor
{
  private static readonly Dictionary<string, Rgba> Named = new()
  {
    ["white"]       = new Rgba(255, 255, 255, 1.0),
    ["black"]       = new Rgba(0, 0, 0, 1.0),
    ["transparent"] = new Rgba(0, 0, 0, 0.0),
    ["red"]         = new Rgba(255, 0, 0, 1.0),
    ["gray"]        = new Rgba(128, 128, 128, 1.0),
    ["grey"]        = new Rgba(128, 128, 128, 1.0),
  };

  [GeneratedRegex(@"^#[0-9a-f]{3,8}$")]
  private static partial Regex HexRegex();

  [GeneratedRegex(@"^(rgba?|hsla?)\((.*)\)$", RegexOptions.Singleline)]
  private static partial Regex FunctionRegex();

  [GeneratedRegex(@"^color-mix\(in [\w-]+,\s*(.+?)\s+([\d.]+)%\s*,\s*(.+)\)$", RegexOptions.Singleline)]
  private static partial Regex ColorMixRegex();

  [GeneratedRegex(@"^\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}$")]
  private static partial Regex TripleRegex();

  /// <summary>Parses a color. Returns null when the text is not a color.</summary>
  public static Rgba? Parse(string text)
  {
    var t = text.Trim().ToLowerInvariant().Replace("!important", "").Trim();

    if (Named.TryGetValue(t, out var named))
      return named;

    if (HexRegex().IsMatch(t))
      return ParseHex(t);

    var function = FunctionRegex().Match(t);

    if (function.Success)
      return ParseFunction(function.Groups[1].Value, function.Groups[2].Value);

    var mix = ColorMixRegex().Match(t);

    if (mix.Success)
      return ParseColorMix(mix);

    if (TripleRegex().IsMatch(t))
    {
      var parts = t.Split(',').Select(p => int.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();

      return new Rgba(parts[0], parts[1], parts[2], 1.0);
    }

    return null;
  }

  private static Rgba ParseHex(string t)
  {
    var h = t[1..];

    if (h.Length is 4 or 8)
    {
      var alphaHex = h.Length == 4 ? $"{h[3]}{h[3]}" : h[6..8];
      var alpha    = int.Parse(alphaHex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) / 255.0;

      return ColorMath.WithAlpha(ColorMath.HexToRgb(h.Length == 4 ? h[..3] : h[..6]), alpha);
    }

    return ColorMath.WithAlpha(ColorMath.HexToRgb(h), 1.0);
  }

  private static Rgba? ParseFunction(string name, string inner)
  {
    var args = SplitArguments(inner);

    try
    {
      var alpha = args.Count > 3 ? Channel(args[3], 1) : 1.0;

      if (name.StartsWith("rgb", StringComparison.Ordinal))
      {
        if (args.Count < 3)
          return null;

        int R(int i) => (int)Math.Round(Channel(args[i], 255), MidpointRounding.ToEven);

        return new Rgba(R(0), R(1), R(2), alpha);
      }

      var hue   = CalcExpression.Evaluate(args[0]);
      var sat   = Channel(args[1], 1);
      var light = Channel(args[2], 1);

      sat   = sat > 1 ? sat / 100 : sat;
      light = light > 1 ? light / 100 : light;

      return ColorMath.WithAlpha(ColorMath.FromHls(hue / 360, light, sat), alpha);
    }
    catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException or DivideByZeroException or OverflowException)
    {
      return null;
    }
  }

  private static Rgba? ParseColorMix(Match m)
  {
    var first  = Parse(m.Groups[1].Value);
    var second = Parse(m.Groups[3].Value);

    if (first is not { } a || second is not { } b)
      return null;

    if (!double.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
      return null;

    var share = percent / 100;

    if (b.A == 0)
      return ColorMath.WithAlpha(a.Rgb, a.A * share);

    return ColorMath.WithAlpha(ColorMath.Mix(b.Rgb, a.Rgb, share), a.A * share + b.A * (1 - share));
  }

  /// <summary>Evaluates one channel. A percentage (outside calc) is a share of <paramref name="scale" />.</summary>
  private static double Channel(string text, double scale)
  {
    var percent = text.Trim().EndsWith('%') && !text.Contains("calc", StringComparison.Ordinal);
    var value   = CalcExpression.Evaluate(text);

    return percent ? value * scale / 100 : value;
  }

  /// <summary>Splits function arguments at commas, slashes and spaces outside nested parentheses.</summary>
  internal static List<string> SplitArguments(string inner)
  {
    var parts = new List<string>();
    var depth = 0;
    var cur   = new System.Text.StringBuilder();

    foreach (var ch in inner.Replace('/', ','))
    {
      if (ch == '(')
        depth++;
      else if (ch == ')')
        depth--;

      if (depth == 0 && (ch == ',' || (char.IsWhiteSpace(ch) && cur.ToString().Trim().Length > 0)))
      {
        if (cur.ToString().Trim().Length > 0)
          parts.Add(cur.ToString().Trim());

        cur.Clear();

        continue;
      }

      cur.Append(ch);
    }

    if (cur.ToString().Trim().Length > 0)
      parts.Add(cur.ToString().Trim());

    return parts;
  }
}
