// Rule 9: an answer should not be an unordered set of parts.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Engine;
using Text;

/// <summary>
///   Finds answers that are sets: an HTML list, or short parts separated by commas, semicolons, line breaks, or "and".
/// </summary>
public sealed partial class AvoidSetsRule() : FormulationRule(RuleIds.AvoidSets, "Avoid sets", "Avoid")
{
  /// <summary>A part with more words than this is a clause, so the text is a sentence and not a set.</summary>
  public const int MaxPartWords = 4;

  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    if (item.AnswerListItems.Count >= options.MinSetParts)
    {
      yield return SetFinding("answer", string.Join(", ", item.AnswerListItems), item.AnswerListItems.Count);
      yield break;
    }

    var label = item.IsCloze ? "cloze deletion" : "answer";

    foreach (var text in item.RecalledTexts)
    {
      var parts = SplitSet(text);

      if (parts.Count >= options.MinSetParts)
        yield return SetFinding(label, text, parts.Count);
    }
  }

  /// <summary>Returns the parts of <paramref name="text" /> when it reads as a set, or an empty list.</summary>
  public static IReadOnlyList<string> SplitSet(string text)
  {
    var trimmed = text.Trim().TrimEnd('.', '!', '?');

    if (trimmed.Length == 0 || TextTools.Sentences(trimmed.Replace('\n', ' ')).Count > 1)
      return [];

    var parts = SeparatorRegex().Split(trimmed)
                                .Select(p => p.Trim())
                                .Where(p => p.Length > 0)
                                .ToList();

    return parts.All(p => TextTools.CountWords(p) <= MaxPartWords) ? parts : [];
  }

  private FormulationFinding SetFinding(string label, string text, int count) =>
    Finding(
      FindingSeverity.Warning,
      $"The {label} {TextTools.Quote(text)} is a set of {count} parts. Sets are hard to remember. "
      + "You could turn it into an enumeration or make one item for each part.");

  [GeneratedRegex(@"\s*[,;\n]\s*(?:and\s+)?|\s+and\s+|\s*&\s*", RegexOptions.IgnoreCase)]
  private static partial Regex SeparatorRegex();
}
