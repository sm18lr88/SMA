// User-facing summaries of a check result.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System.Collections.Generic;
using System.Linq;

/// <summary>Builds the summary line of the findings window and the notification text.</summary>
public static class FindingText
{
  /// <summary>The summary when the current element is not an item.</summary>
  public const string NotAnItem = "The current element is not an item. The formulation advisor checks items only.";

  /// <summary>Returns the summary line of the findings window.</summary>
  public static string Summary(IReadOnlyCollection<FormulationFinding> findings)
  {
    if (findings.Count == 0)
      return "No rule applies. This item follows the rules that the advisor checks.";

    return $"{Count(findings)} for this item. You decide what to change.";
  }

  /// <summary>
  ///   Returns the notification text. It does not quote the item, so that a notification during a repetition does not show
  ///   the answer.
  /// </summary>
  public static string Notification(IReadOnlyCollection<FormulationFinding> findings, string hotKey) =>
    $"Formulation advisor: {Count(findings)} for this item. Press {hotKey} to see them.";

  private static string Count(IReadOnlyCollection<FormulationFinding> findings)
  {
    var warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);
    var advice   = findings.Count - warnings;
    var parts    = new List<string>(2);

    if (warnings > 0)
      parts.Add(warnings == 1 ? "1 warning" : $"{warnings} warnings");

    if (advice > 0)
      parts.Add(advice == 1 ? "1 piece of advice" : $"{advice} pieces of advice");

    return string.Join(" and ", parts);
  }
}
