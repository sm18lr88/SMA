// Item fixtures for the formulation rule tests.
namespace SuperMemoAssistant.Tests.Formulation;

using SuperMemoAssistant.Plugins.Formulation.Engine;
using SuperMemoAssistant.Plugins.Formulation.Text;

internal static class Items
{
  public static FormulationItem Qa(string question, string answer, ItemReferences? references = null) =>
    new() { Question = question, Answer = answer, References = references };

  public static FormulationItem Cloze(string question, string deletion, ItemReferences? references = null) =>
    new()
    {
      Question              = question,
      Answer                = deletion,
      IsCloze               = true,
      ClozePlaceholderCount = HtmlText.CountPlaceholders(question),
      ClozeDeletions        = [deletion],
      References            = references,
    };

  public static IReadOnlyList<FormulationFinding> Run(IFormulationRule rule, FormulationItem item, FormulationOptions? options = null) =>
    rule.Check(item, options ?? FormulationOptions.Default).ToList();
}
