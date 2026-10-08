// The analyzer, the settings that drive it, the notification throttle, and the summary texts.
namespace SuperMemoAssistant.Tests.Formulation;

using SuperMemoAssistant.Plugins.Formulation;
using SuperMemoAssistant.Plugins.Formulation.Engine;
using Xunit;

public sealed class AnalyzerTests
{
  private static readonly FormulationItem PoorItem = Items.Qa(
    "List the things that are currently important?",
    "one, two, three, four, five, six, seven, eight, nine, ten, eleven",
    new ItemReferences());

  private static readonly FormulationItem GoodItem = Items.Qa("What is the capital of France?", "Paris", new ItemReferences(Source: "Atlas"));

  [Fact]
  public void DefaultRulesHaveUniqueIdsAndArticleLinks()
  {
    var rules = FormulationAnalyzer.CreateDefaultRules();

    Assert.Equal(8, rules.Select(r => r.Id).Distinct().Count());
    Assert.All(rules, r => Assert.StartsWith("https://super-memory.com/articles/20rules.htm#", r.Link.AbsoluteUri));
  }

  [Fact]
  public void AnalyzeListsWarningsBeforeAdvice()
  {
    var findings = new FormulationAnalyzer().Analyze(PoorItem, FormulationOptions.Default);

    Assert.Equal(
      [RuleIds.MinimumInformation, RuleIds.AvoidSets, RuleIds.AvoidEnumerations, RuleIds.ProvideSources, RuleIds.DateStamping],
      findings.Select(f => f.RuleId));
    Assert.Equal(findings.OrderByDescending(f => f.Severity).Select(f => f.Severity), findings.Select(f => f.Severity));
  }

  [Fact]
  public void AnalyzeReturnsNothingForAGoodItem() => Assert.Empty(new FormulationAnalyzer().Analyze(GoodItem, FormulationOptions.Default));

  [Fact]
  public void DisabledRulesDoNotRun()
  {
    var options  = new FormulationOptions { DisabledRuleIds = new HashSet<string> { RuleIds.AvoidSets, RuleIds.DateStamping } };
    var findings = new FormulationAnalyzer().Analyze(PoorItem, options);

    Assert.DoesNotContain(findings, f => f.RuleId is RuleIds.AvoidSets or RuleIds.DateStamping);
    Assert.Contains(findings, f => f.RuleId == RuleIds.MinimumInformation);
  }

  [Fact]
  public void SettingsDefaultsMatchTheEngineDefaults()
  {
    var options = new FormulationCfg().ToOptions();

    Assert.True(new FormulationCfg().AutoCheck);
    Assert.Equal(10, options.MaxAnswerWords);
    Assert.Equal(30, options.MaxQuestionWords);
    Assert.Equal(3, options.MinSetParts);
    Assert.Empty(options.DisabledRuleIds);
  }

  [Fact]
  public void SettingsMapSwitchesAndThresholds()
  {
    var cfg = new FormulationCfg
    {
      MaxAnswerWords            = 4,
      MaxQuestionWords          = 12,
      MinSetParts               = 2,
      MinimumInformationEnabled = false,
      ClozeDeletionEnabled      = false,
      AvoidSetsEnabled          = false,
      AvoidEnumerationsEnabled  = false,
      ContextCuesEnabled        = false,
      OptimizeWordingEnabled    = false,
      ProvideSourcesEnabled     = false,
      DateStampingEnabled       = false,
    };

    var options = cfg.ToOptions();

    Assert.Equal((4, 12, 2), (options.MaxAnswerWords, options.MaxQuestionWords, options.MinSetParts));
    Assert.Equal(FormulationAnalyzer.CreateDefaultRules().Select(r => r.Id).Order(), options.DisabledRuleIds.Order());
    Assert.Empty(new FormulationAnalyzer().Analyze(PoorItem, options));
  }

  [Fact]
  public void SettingsClampInvalidThresholds()
  {
    var options = new FormulationCfg { MaxAnswerWords = 0, MaxQuestionWords = -3, MinSetParts = 1 }.ToOptions();

    Assert.Equal((1, 1, 2), (options.MaxAnswerWords, options.MaxQuestionWords, options.MinSetParts));
  }

  [Fact]
  public void ThrottleNotifiesOncePerElementAndOnlyForWarnings()
  {
    var throttle = new ElementNotificationThrottle();
    var findings = new FormulationAnalyzer().Analyze(PoorItem, FormulationOptions.Default);
    var advice   = findings.Where(f => f.Severity == FindingSeverity.Advice).ToList();

    Assert.False(throttle.ShouldNotify(7, advice));
    Assert.True(throttle.ShouldNotify(7, findings));
    Assert.False(throttle.ShouldNotify(7, findings));
    Assert.True(throttle.ShouldNotify(8, findings));

    throttle.Reset();

    Assert.True(throttle.ShouldNotify(7, findings));
  }

  [Fact]
  public void SummaryAndNotificationTexts()
  {
    var findings = new FormulationAnalyzer().Analyze(PoorItem, FormulationOptions.Default);

    Assert.StartsWith("No rule applies.", FindingText.Summary([]));
    Assert.Equal("2 warnings and 3 pieces of advice for this item. You decide what to change.", FindingText.Summary(findings));
    Assert.Equal(
      "Formulation advisor: 2 warnings and 3 pieces of advice for this item. Press Ctrl+Alt+Shift+F to see them.",
      FindingText.Notification(findings, "Ctrl+Alt+Shift+F"));
    Assert.DoesNotContain("one, two", FindingText.Notification(findings, "x"));
  }
}
