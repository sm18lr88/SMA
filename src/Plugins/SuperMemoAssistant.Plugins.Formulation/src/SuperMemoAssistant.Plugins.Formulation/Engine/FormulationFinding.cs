// One piece of formulation advice for an item.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System;

/// <summary>How strongly the advisor recommends a change.</summary>
public enum FindingSeverity
{
  /// <summary>A suggestion. The advisor shows it only when the user asks.</summary>
  Advice,

  /// <summary>A likely problem. The automatic check can show a notification for it.</summary>
  Warning,
}

/// <summary>A rule that applies to an item, with a short message that quotes the problem text.</summary>
/// <param name="RuleId">The stable identifier of the rule (see <see cref="RuleIds" />).</param>
/// <param name="RuleTitle">The rule title as Piotr Wozniak wrote it.</param>
/// <param name="Severity">How strongly the advisor recommends a change.</param>
/// <param name="Message">The explanation, which quotes the problem text.</param>
/// <param name="Link">The matching section of the 20 rules article.</param>
public sealed record FormulationFinding(
  string          RuleId,
  string          RuleTitle,
  FindingSeverity Severity,
  string          Message,
  Uri             Link);
