// Turns SuperMemo HTML into plain text for the rules.
namespace SuperMemoAssistant.Plugins.Formulation.Text;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

/// <summary>Converts SuperMemo component HTML to plain text and finds SuperMemo's cloze markup.</summary>
public static partial class HtmlText
{
  /// <summary>
  ///   Returns the visible text of <paramref name="html" />: the reference block, scripts, styles, and tags are removed,
  ///   entities are decoded, and each block element becomes one line.
  /// </summary>
  public static string ToPlainText(string? html)
  {
    if (string.IsNullOrWhiteSpace(html))
      return string.Empty;

    var text = ReferenceBlockRegex().Replace(html, "\n");
    text = InvisibleRegex().Replace(text, " ");
    text = LineBreakRegex().Replace(text, "\n");
    text = TagRegex().Replace(text, string.Empty);
    text = WebUtility.HtmlDecode(text).Replace('\u00A0', ' ');

    var lines = text.Split('\n')
                    .Select(l => SpacesRegex().Replace(l, " ").Trim())
                    .Where(l => l.Length > 0);

    return string.Join("\n", lines);
  }

  /// <summary>Returns the plain text of each <c>&lt;li&gt;</c> entry of <paramref name="html" />.</summary>
  public static IReadOnlyList<string> ListItems(string? html) => InnerTexts(ListItemRegex(), html);

  /// <summary>Returns the plain text of each SuperMemo cloze span (<c>&lt;span class=cloze&gt;</c>) of <paramref name="html" />.</summary>
  public static IReadOnlyList<string> ClozeSpans(string? html) => InnerTexts(ClozeSpanRegex(), html);

  /// <summary>Counts the "[...]" placeholders in plain <paramref name="text" />.</summary>
  public static int CountPlaceholders(string text) => PlaceholderRegex().Count(text);

  /// <summary>Returns the position and length of the first "[...]" placeholder in <paramref name="text" />, or null.</summary>
  public static (int Index, int Length)? FindPlaceholder(string text)
  {
    var match = PlaceholderRegex().Match(text);

    return match.Success ? (match.Index, match.Length) : null;
  }

  /// <summary>Whether <paramref name="text" /> is exactly one "[...]" placeholder.</summary>
  public static bool IsPlaceholder(string text)
  {
    var trimmed = text.Trim();
    var match   = PlaceholderRegex().Match(trimmed);

    return match.Success && match.Length == trimmed.Length;
  }

  private static IReadOnlyList<string> InnerTexts(Regex regex, string? html)
  {
    if (string.IsNullOrWhiteSpace(html))
      return [];

    var withoutReferences = ReferenceBlockRegex().Replace(html, string.Empty);

    return regex.Matches(withoutReferences)
                .Select(m => ToPlainText(m.Groups["inner"].Value).Replace('\n', ' '))
                .Where(t => t.Length > 0)
                .ToList();
  }

  [GeneratedRegex(@"(?:<br\s*/?>\s*)*(?:<hr[^>]*SuperMemo[^>]*>\s*)?<SuperMemoReference\b.*?(?:</SuperMemoReference>|$)",
                  RegexOptions.IgnoreCase | RegexOptions.Singleline)]
  private static partial Regex ReferenceBlockRegex();

  [GeneratedRegex(@"<(head|script|style)\b.*?</\1\s*>|<!--.*?-->", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
  private static partial Regex InvisibleRegex();

  [GeneratedRegex(@"<br\s*/?>|</?(?:p|div|li|ul|ol|tr|h[1-6]|blockquote)\b[^>]*>", RegexOptions.IgnoreCase)]
  private static partial Regex LineBreakRegex();

  [GeneratedRegex(@"<[^>]*>")]
  private static partial Regex TagRegex();

  [GeneratedRegex(@"[ \t\r\f\v]+")]
  private static partial Regex SpacesRegex();

  [GeneratedRegex(@"<li\b[^>]*>(?<inner>.*?)(?=<li\b|</li\s*>|</ul\s*>|</ol\s*>|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
  private static partial Regex ListItemRegex();

  [GeneratedRegex(@"<span\b[^>]*\bclass\s*=\s*[""']?cloze\b[^>]*>(?<inner>.*?)</span\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
  private static partial Regex ClozeSpanRegex();

  [GeneratedRegex(@"\[\s*(?:\.\s*){3}\]|\[\s*…\s*\]")]
  private static partial Regex PlaceholderRegex();
}
