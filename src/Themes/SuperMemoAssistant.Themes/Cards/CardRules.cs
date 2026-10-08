// The card stylesheet rules (background and text colors for BODY, cloze, extract, highlight, links, ...) derived from a palette.
using SuperMemoAssistant.Themes.Colors;

namespace SuperMemoAssistant.Themes.Cards;

internal readonly record struct CardRule(string Selector, string Property, string Value);

internal static class CardRules
{
  private static Rgb Readable(Rgb text, Rgb bg, Rgb[] fallbacks, double minimum = 4.5)
  {
    foreach (var candidate in fallbacks.Prepend(text))
    {
      if (ColorMath.Contrast(candidate, bg) >= minimum)
        return candidate;
    }

    return ColorMath.BestText(bg, fallbacks);
  }

  public static IReadOnlyList<CardRule> For(IReadOnlyDictionary<string, string> roles)
  {
    var c  = roles.ToDictionary(kv => kv.Key, kv => ColorMath.HexToRgb(kv.Value));
    var bg = c["bg"];
    var fg = c["fg"];

    Rgb Tint(string color, double amount) => ColorMath.Mix(bg, c[color], amount);
    Rgb Text(Rgb preferred, Rgb back, double minimum = 4.5) => Readable(preferred, back, [fg], minimum);

    var blocks = new (string Selector, Rgb Back, Rgb Text)[]
    {
      ("BODY", bg, fg),
      (".Cloze", Tint("yellow", 0.3), Text(c["red"], Tint("yellow", 0.3), 3)),
      (".clozed", Tint("orange", 0.3), Text(fg, Tint("orange", 0.3))),
      (".Extract", Tint("accent", 0.3), Text(fg, Tint("accent", 0.3))),
      (".headers", c["bg_alt"], c["alt_fg"]),
      (".RefText", Tint("yellow", 0.12), Text(fg, Tint("yellow", 0.12))),
      (".Reference", Tint("purple", 0.2), Text(c["fg_dim"], Tint("purple", 0.2))),
      (".Highlight", Tint("yellow", 0.35), Text(c["red"], Tint("yellow", 0.35), 3)),
      (".SearchHighlight", Tint("yellow", 0.5), Text(fg, Tint("yellow", 0.5))),
      (".SearchHighlight1", Tint("cyan", 0.45), Text(fg, Tint("cyan", 0.45))),
      (".Ignore", Tint("red", 0.15), Text(c["muted"], Tint("red", 0.15))),
      (".tablelabel", c["bg_alt"], Readable(c["accent"], c["bg_alt"], [c["alt_fg"]])),
    };

    var rules = new List<CardRule>();

    foreach (var (selector, back, text) in blocks)
    {
      rules.Add(new CardRule(selector, "background-color", ColorMath.RgbToHex(back)));
      rules.Add(new CardRule(selector, "color", ColorMath.RgbToHex(text)));
    }

    rules.Add(new CardRule("A", "color", ColorMath.RgbToHex(Readable(c["accent"], bg, [c["blue"], fg]))));

    return rules;
  }

  public static string Apply(string css, IEnumerable<CardRule> rules) =>
    rules.Aggregate(css, (current, r) => CardCss.Set(current, r.Selector, r.Property, r.Value));
}
