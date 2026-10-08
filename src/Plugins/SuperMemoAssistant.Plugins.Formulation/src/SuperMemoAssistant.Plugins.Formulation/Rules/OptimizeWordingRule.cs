// Rule 15: the question should be short.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System.Collections.Generic;
using Engine;
using Text;

/// <summary>Finds questions that are longer than <see cref="FormulationOptions.MaxQuestionWords" />.</summary>
public sealed class OptimizeWordingRule() : FormulationRule(RuleIds.OptimizeWording, "Optimize wording", "Optimize")
{
  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    var words = TextTools.CountWords(item.Question);

    if (words <= options.MaxQuestionWords)
      yield break;

    yield return Finding(
      FindingSeverity.Advice,
      $"The question has {words} words: {TextTools.Quote(item.Question)}. Shorter wording is faster to read and easier to remember.");
  }
}
