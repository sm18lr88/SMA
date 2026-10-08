// Runs the enabled rules on an item.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using Rules;

/// <summary>Checks an item against a set of <see cref="IFormulationRule" />. It has no SuperMemo or WPF dependency.</summary>
public sealed class FormulationAnalyzer
{
  private readonly IReadOnlyList<IFormulationRule> _rules;

  /// <summary>Creates an analyzer with the built-in rules.</summary>
  public FormulationAnalyzer() : this(CreateDefaultRules()) { }

  /// <summary>Creates an analyzer with <paramref name="rules" />. Add a rule here to extend the advisor.</summary>
  public FormulationAnalyzer(IEnumerable<IFormulationRule> rules)
  {
    ArgumentNullException.ThrowIfNull(rules);

    _rules = rules.ToList();
  }

  /// <summary>The rules that this analyzer runs, in order.</summary>
  public IReadOnlyList<IFormulationRule> Rules => _rules;

  /// <summary>Returns a new list of the built-in rules, in the order of the 20 rules article.</summary>
  public static IReadOnlyList<IFormulationRule> CreateDefaultRules() =>
  [
    new MinimumInformationRule(),
    new ClozeDeletionRule(),
    new AvoidSetsRule(),
    new AvoidEnumerationsRule(),
    new ContextCuesRule(),
    new OptimizeWordingRule(),
    new ProvideSourcesRule(),
    new DateStampingRule(),
  ];

  /// <summary>Returns the findings of the enabled rules: warnings first, then advice, each in rule order.</summary>
  public IReadOnlyList<FormulationFinding> Analyze(FormulationItem item, FormulationOptions options)
  {
    ArgumentNullException.ThrowIfNull(item);
    ArgumentNullException.ThrowIfNull(options);

    return _rules.Where(r => options.IsEnabled(r.Id))
                 .SelectMany(r => r.Check(item, options))
                 .OrderByDescending(f => f.Severity)
                 .ToList();
  }
}
