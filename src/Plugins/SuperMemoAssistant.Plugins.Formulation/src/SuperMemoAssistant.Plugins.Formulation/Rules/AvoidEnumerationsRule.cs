// Rule 10: a question should not ask for a list.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Engine;
using Text;

/// <summary>Finds questions that ask for a list ("list", "name all", "what are the", or a number of expected parts).</summary>
public sealed partial class AvoidEnumerationsRule()
  : FormulationRule(RuleIds.AvoidEnumerations, "Avoid enumerations", "Enumerations")
{
  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    // In a cloze statement, words such as "list" are content, not a request.
    if (item.IsCloze || !EnumerationRegex().IsMatch(item.Question))
      yield break;

    yield return Finding(
      FindingSeverity.Advice,
      $"The question {TextTools.Quote(item.Question)} asks for a list. Enumerations are hard to remember. "
      + "You could use overlapping cloze deletions, with one item for each member.");
  }

  [GeneratedRegex(
    @"\b(?:list|enumerate|name\s+all|what\s+are\s+the)\b"
    + @"|\b(?:name|give|state|identify|mention)\s+(?:the\s+)?(?:\d+|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\b"
    + @"|\b\d+\s+(?:items|parts|steps|stages|reasons|examples|types|kinds|elements|factors|causes)\b",
    RegexOptions.IgnoreCase)]
  private static partial Regex EnumerationRegex();
}
