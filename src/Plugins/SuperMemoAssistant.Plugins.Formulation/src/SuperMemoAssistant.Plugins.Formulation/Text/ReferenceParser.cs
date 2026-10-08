// Reads the SuperMemo reference block (#Title, #Source, #Link, #Date) from component HTML.
namespace SuperMemoAssistant.Plugins.Formulation.Text;

using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Engine;

/// <summary>
///   Parses the <c>&lt;SuperMemoReference&gt;</c> block that SuperMemo appends to HTML components (see
///   <c>SMConst.Elements.ReferenceFormat</c>).
/// </summary>
public static partial class ReferenceParser
{
  /// <summary>Returns the references in <paramref name="html" />, or null when it has no reference block.</summary>
  public static ItemReferences? Parse(string? html)
  {
    if (string.IsNullOrEmpty(html))
      return null;

    var block = BlockRegex().Match(html);

    if (!block.Success)
      return null;

    var text       = LineBreakRegex().Replace(block.Groups["inner"].Value, "\n");
    var plain      = WebUtility.HtmlDecode(TagRegex().Replace(text, string.Empty));
    var references = new ItemReferences();

    foreach (var line in plain.Split('\n'))
    {
      var field = FieldRegex().Match(line.Trim());

      if (!field.Success)
        continue;

      var value = field.Groups["value"].Value.Trim();

      references = field.Groups["name"].Value.ToUpperInvariant() switch
      {
        "TITLE"  => references with { Title = value },
        "SOURCE" => references with { Source = value },
        "LINK"   => references with { Link = value },
        "DATE"   => references with { Date = value },
        _        => references,
      };
    }

    return references;
  }

  /// <summary>Returns the first reference block found in <paramref name="htmls" />, or null when there is none.</summary>
  public static ItemReferences? ParseFirst(IEnumerable<string> htmls)
  {
    foreach (var html in htmls)
      if (Parse(html) is { } references)
        return references;

    return null;
  }

  [GeneratedRegex(@"<SuperMemoReference\b[^>]*>(?<inner>.*?)(?:</SuperMemoReference>|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
  private static partial Regex BlockRegex();

  [GeneratedRegex(@"<br\s*/?>|</?(?:p|div|h[1-6])\b[^>]*>", RegexOptions.IgnoreCase)]
  private static partial Regex LineBreakRegex();

  [GeneratedRegex(@"<[^>]*>")]
  private static partial Regex TagRegex();

  [GeneratedRegex(@"^#(?<name>Title|Source|Link|Date|Author|Comment|E-mail)\s*:\s*(?<value>.*)$", RegexOptions.IgnoreCase)]
  private static partial Regex FieldRegex();
}
