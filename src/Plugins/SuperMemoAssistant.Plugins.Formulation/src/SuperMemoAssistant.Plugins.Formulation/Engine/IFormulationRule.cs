// The extension point of the advisor: one class per rule.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System;
using System.Collections.Generic;

/// <summary>A check of one of the 20 rules of formulating knowledge.</summary>
public interface IFormulationRule
{
  /// <summary>A stable identifier (see <see cref="RuleIds" />), used by the settings.</summary>
  string Id { get; }

  /// <summary>The rule title as Piotr Wozniak wrote it.</summary>
  string Title { get; }

  /// <summary>The matching section of the 20 rules article.</summary>
  Uri Link { get; }

  /// <summary>Returns the findings of this rule for <paramref name="item" />. Returns nothing when the rule does not apply.</summary>
  IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options);
}

/// <summary>Base class for rules: holds the rule identity and creates findings.</summary>
public abstract class FormulationRule : IFormulationRule
{
  /// <summary>The 20 rules article. The summary page on supermemo.guru links to it and has no section per rule.</summary>
  public static Uri Article { get; } = new("https://super-memory.com/articles/20rules.htm");

  /// <summary>Creates a rule whose link points to <paramref name="anchor" /> in <see cref="Article" />.</summary>
  protected FormulationRule(string id, string title, string anchor)
  {
    Id    = id;
    Title = title;
    Link  = new Uri(Article, "#" + anchor);
  }

  /// <inheritdoc />
  public string Id { get; }

  /// <inheritdoc />
  public string Title { get; }

  /// <inheritdoc />
  public Uri Link { get; }

  /// <inheritdoc />
  public abstract IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options);

  /// <summary>Creates a finding of this rule.</summary>
  protected FormulationFinding Finding(FindingSeverity severity, string message) => new(Id, Title, severity, message, Link);
}

/// <summary>The identifiers of the built-in rules.</summary>
public static class RuleIds
{
  public const string MinimumInformation = "minimum-information";
  public const string ClozeDeletion      = "cloze-deletion";
  public const string AvoidSets          = "avoid-sets";
  public const string AvoidEnumerations  = "avoid-enumerations";
  public const string OptimizeWording    = "optimize-wording";
  public const string ContextCues        = "context-cues";
  public const string ProvideSources     = "provide-sources";
  public const string DateStamping       = "date-stamping";
}
