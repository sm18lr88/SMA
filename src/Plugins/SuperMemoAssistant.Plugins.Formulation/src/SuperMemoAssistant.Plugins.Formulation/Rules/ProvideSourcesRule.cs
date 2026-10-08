// Rule 18: an item should name its source.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System.Collections.Generic;
using Engine;
using Text;

/// <summary>Finds items whose references have no Source and no Link. Silent when the references cannot be read.</summary>
public sealed class ProvideSourcesRule() : FormulationRule(RuleIds.ProvideSources, "Provide sources", "sources")
{
  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    if (item.References is not { HasSource: false })
      yield break;

    yield return Finding(
      FindingSeverity.Advice,
      $"The item {TextTools.Quote(item.Question)} has no Source or Link reference. A source helps you check the item later.");
  }
}
