// Rule 5: one cloze deletion per item.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System.Collections.Generic;
using Engine;
using Text;

/// <summary>Finds cloze items that hide more than one part of the text.</summary>
public sealed class ClozeDeletionRule()
  : FormulationRule(RuleIds.ClozeDeletion, "Cloze deletion is easy and effective", "Cloze")
{
  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    if (item.ClozePlaceholderCount <= 1)
      yield break;

    yield return Finding(
      FindingSeverity.Warning,
      $"The question {TextTools.Quote(item.Question)} hides {item.ClozePlaceholderCount} parts. "
      + "One cloze deletion per item is easier to recall. You could make one item for each part.");
  }
}
