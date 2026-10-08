// Rule 4: the answer (or the cloze deletion) should be short.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System.Collections.Generic;
using Engine;
using Text;

/// <summary>Finds answers and cloze deletions that are longer than <see cref="FormulationOptions.MaxAnswerWords" />.</summary>
public sealed class MinimumInformationRule()
  : FormulationRule(RuleIds.MinimumInformation, "Stick to the minimum information principle", "minimum")
{
  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    var label = item.IsCloze ? "cloze deletion" : "answer";

    foreach (var text in item.RecalledTexts)
    {
      var words = TextTools.CountWords(text);

      if (words > options.MaxAnswerWords)
        yield return Finding(
          FindingSeverity.Warning,
          $"The {label} {TextTools.Quote(text)} has {words} words. You could split it into smaller items that each ask for one fact.");
    }
  }
}
