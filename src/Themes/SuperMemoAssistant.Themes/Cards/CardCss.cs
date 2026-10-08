// Edits one property of one CSS rule in a stylesheet while keeping the file's own style (case, line endings, spacing).
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes.Cards;

internal static partial class CardCss
{
  [GeneratedRegex(@"(?<sel>[^{}]+?)\s*\{(?<body>[^}]*)\}", RegexOptions.Singleline)]
  private static partial Regex RuleRegex();

  [GeneratedRegex(@"[A-Z]{3,}-?[A-Z]*\s*:")]
  private static partial Regex UpperCaseProperty();

  /// <summary>Sets one property in one rule, or removes it when <paramref name="value" /> is null.</summary>
  public static string Set(string css, string selector, string prop, string? value)
  {
    foreach (Match m in RuleRegex().Matches(css))
    {
      if (!string.Equals(m.Groups["sel"].Value.Trim(), selector.Trim(), StringComparison.OrdinalIgnoreCase))
        continue;

      var body = EditBody(m.Groups["body"].Value, prop, value);
      var at   = m.Groups["body"];

      return string.Concat(css.AsSpan(0, at.Index), body, css.AsSpan(at.Index + at.Length));
    }

    if (value is null)
      return css;

    var nl   = css.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    var lead = css.Length == 0 || css.EndsWith('\n') || css.EndsWith('\r') ? "" : nl;

    return $"{css}{lead}{selector} {{{prop}: {value};}}{nl}";
  }

  private static string EditBody(string body, string prop, string? value)
  {
    var decl = new Regex($@"(^|[;\s{{])({Regex.Escape(prop)})\s*:\s*[^;]*;?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    var hit  = decl.Match(body);

    if (hit.Success)
    {
      var replacement = value is null ? hit.Groups[1].Value : $"{hit.Groups[1].Value}{hit.Groups[2].Value}: {value};";

      return string.Concat(body.AsSpan(0, hit.Index), replacement, body.AsSpan(hit.Index + hit.Length));
    }

    if (value is null)
      return body;

    var name = UpperCaseProperty().IsMatch(body) ? prop.ToUpperInvariant() : prop;
    var sep  = body.Trim().Length == 0 || body.TrimEnd().EndsWith(';') ? "" : ";";

    return body.Trim().Length > 0 ? $"{body.TrimEnd()}{sep} {name}: {value};" : $"{name}: {value};";
  }
}
