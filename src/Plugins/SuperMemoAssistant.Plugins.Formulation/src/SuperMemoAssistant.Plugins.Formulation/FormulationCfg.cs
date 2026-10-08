// User settings of the formulation advisor (Forge.Forms).
namespace SuperMemoAssistant.Plugins.Formulation;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Engine;
using Forge.Forms.Annotations;
using Newtonsoft.Json;
using Services.UI.Configuration;
using Sys.ComponentModel;

/// <summary>The settings of the formulation advisor: the automatic check, each rule, and the thresholds.</summary>
[Form(Mode = DefaultFields.None)]
[Title("Formulation Advisor Settings", IsVisible = "{Env DialogHostContext}")]
[DialogAction("cancel", "Cancel", IsCancel = true)]
[DialogAction("save", "Save", IsDefault = true, Validates = true)]
public sealed class FormulationCfg : CfgBase<FormulationCfg>, INotifyPropertyChangedEx
{
  [Field(Name = "Check each item when it is displayed (notify only on warnings)")]
  public bool AutoCheck { get; set; } = true;

  [Field(Name = "Rule 4: Minimum information principle")]
  public bool MinimumInformationEnabled { get; set; } = true;

  [Field(Name = "Maximum words in an answer or cloze deletion")]
  [Value(Must.BeGreaterThanOrEqualTo, 1, StrictValidation = true)]
  public int MaxAnswerWords { get; set; } = FormulationOptions.Default.MaxAnswerWords;

  [Field(Name = "Rule 5: One cloze deletion per item")]
  public bool ClozeDeletionEnabled { get; set; } = true;

  [Field(Name = "Rule 9: Avoid sets")]
  public bool AvoidSetsEnabled { get; set; } = true;

  [Field(Name = "Minimum number of parts in a set")]
  [Value(Must.BeGreaterThanOrEqualTo, 2, StrictValidation = true)]
  public int MinSetParts { get; set; } = FormulationOptions.Default.MinSetParts;

  [Field(Name = "Rule 10: Avoid enumerations")]
  public bool AvoidEnumerationsEnabled { get; set; } = true;

  [Field(Name = "Rule 13: Context cues")]
  public bool ContextCuesEnabled { get; set; } = true;

  [Field(Name = "Rule 15: Optimize wording")]
  public bool OptimizeWordingEnabled { get; set; } = true;

  [Field(Name = "Maximum words in a question")]
  [Value(Must.BeGreaterThanOrEqualTo, 1, StrictValidation = true)]
  public int MaxQuestionWords { get; set; } = FormulationOptions.Default.MaxQuestionWords;

  [Field(Name = "Rule 18: Provide sources")]
  public bool ProvideSourcesEnabled { get; set; } = true;

  [Field(Name = "Rule 19: Provide date stamping")]
  public bool DateStampingEnabled { get; set; } = true;

  /// <inheritdoc />
  [JsonIgnore]
  public bool IsChanged { get; set; }

  /// <summary>Converts the settings into the options that the rule engine reads.</summary>
  public FormulationOptions ToOptions()
  {
    var switches = new Dictionary<string, bool>(StringComparer.Ordinal)
    {
      [RuleIds.MinimumInformation] = MinimumInformationEnabled,
      [RuleIds.ClozeDeletion]      = ClozeDeletionEnabled,
      [RuleIds.AvoidSets]          = AvoidSetsEnabled,
      [RuleIds.AvoidEnumerations]  = AvoidEnumerationsEnabled,
      [RuleIds.ContextCues]        = ContextCuesEnabled,
      [RuleIds.OptimizeWording]    = OptimizeWordingEnabled,
      [RuleIds.ProvideSources]     = ProvideSourcesEnabled,
      [RuleIds.DateStamping]       = DateStampingEnabled,
    };

    var disabled = new HashSet<string>(StringComparer.Ordinal);

    foreach (var (ruleId, enabled) in switches)
      if (!enabled)
        disabled.Add(ruleId);

    return new FormulationOptions
    {
      MaxAnswerWords   = Math.Max(1, MaxAnswerWords),
      MaxQuestionWords = Math.Max(1, MaxQuestionWords),
      MinSetParts      = Math.Max(2, MinSetParts),
      DisabledRuleIds  = disabled,
    };
  }

  /// <inheritdoc />
  public override string ToString() => "Formulation";

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;
}
