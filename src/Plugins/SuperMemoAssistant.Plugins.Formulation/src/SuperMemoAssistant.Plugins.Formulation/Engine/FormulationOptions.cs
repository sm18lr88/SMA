// Thresholds and rule switches for one analysis.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System;
using System.Collections.Generic;

/// <summary>The settings that the rules read. <see cref="FormulationCfg.ToOptions" /> builds them from the user settings.</summary>
public sealed record FormulationOptions
{
  /// <summary>The default options: every rule is on.</summary>
  public static FormulationOptions Default { get; } = new();

  /// <summary>An answer or cloze deletion with more words than this breaks the minimum information principle.</summary>
  public int MaxAnswerWords { get; init; } = 10;

  /// <summary>A question with more words than this needs shorter wording.</summary>
  public int MaxQuestionWords { get; init; } = 30;

  /// <summary>An answer with at least this many parts is a set.</summary>
  public int MinSetParts { get; init; } = 3;

  /// <summary>The rules that do not run (see <see cref="RuleIds" />).</summary>
  public IReadOnlySet<string> DisabledRuleIds { get; init; } = new HashSet<string>(StringComparer.Ordinal);

  /// <summary>Whether the rule with <paramref name="ruleId" /> runs.</summary>
  public bool IsEnabled(string ruleId) => !DisabledRuleIds.Contains(ruleId);
}
