// Rules about the size of an item: minimum information, one cloze deletion, and short wording.
namespace SuperMemoAssistant.Tests.Formulation;

using SuperMemoAssistant.Plugins.Formulation.Engine;
using SuperMemoAssistant.Plugins.Formulation.Rules;
using Xunit;

public sealed class SizeRuleTests
{
  private const string LongAnswer = "It is the largest city and the political, economic and cultural centre of the country";

  [Fact]
  public void MinimumInformationFlagsALongAnswer()
  {
    var finding = Assert.Single(Items.Run(new MinimumInformationRule(), Items.Qa("What is Paris?", LongAnswer)));

    Assert.Equal(RuleIds.MinimumInformation, finding.RuleId);
    Assert.Equal(FindingSeverity.Warning, finding.Severity);
    Assert.Contains("The answer \"It is the largest city", finding.Message);
    Assert.Contains("15 words", finding.Message);
    Assert.Equal("https://super-memory.com/articles/20rules.htm#minimum", finding.Link.AbsoluteUri);
  }

  [Fact]
  public void MinimumInformationAcceptsAShortAnswer() =>
    Assert.Empty(Items.Run(new MinimumInformationRule(), Items.Qa("What is the capital of France?", "Paris")));

  [Fact]
  public void MinimumInformationChecksTheClozeDeletionNotTheSentence()
  {
    var shortDeletion = Items.Cloze("The capital of France is [...], a city that " + LongAnswer, "Paris");
    var longDeletion  = Items.Cloze("Paris: [...].", LongAnswer);

    Assert.Empty(Items.Run(new MinimumInformationRule(), shortDeletion));
    Assert.Contains("The cloze deletion", Assert.Single(Items.Run(new MinimumInformationRule(), longDeletion)).Message);
  }

  [Theory]
  [InlineData(10, false)]
  [InlineData(9, true)]
  public void MinimumInformationThresholdIsASetting(int maxWords, bool expected)
  {
    var item     = Items.Qa("Q?", "one two three four five six seven eight nine ten");
    var options  = FormulationOptions.Default with { MaxAnswerWords = maxWords };
    var findings = Items.Run(new MinimumInformationRule(), item, options);

    Assert.Equal(expected, findings.Count == 1);
  }

  [Fact]
  public void ClozeDeletionFlagsSeveralPlaceholders()
  {
    var finding = Assert.Single(Items.Run(new ClozeDeletionRule(), Items.Cloze("[...] is the capital of [...].", "Paris")));

    Assert.Equal(FindingSeverity.Warning, finding.Severity);
    Assert.Contains("\"[...] is the capital of [...].\" hides 2 parts", finding.Message);
  }

  [Theory]
  [InlineData("Paris is the capital of [...].")]
  [InlineData("What is the capital of France?")]
  public void ClozeDeletionAcceptsOnePlaceholderOrNone(string question) =>
    Assert.Empty(Items.Run(new ClozeDeletionRule(), Items.Cloze(question, "France")));

  [Fact]
  public void OptimizeWordingFlagsALongQuestion()
  {
    var question = string.Join(' ', Enumerable.Repeat("word", 31)) + "?";
    var finding  = Assert.Single(Items.Run(new OptimizeWordingRule(), Items.Qa(question, "a")));

    Assert.Equal(FindingSeverity.Advice, finding.Severity);
    Assert.Contains("31 words", finding.Message);
    Assert.Contains("\"word word", finding.Message);
  }

  [Fact]
  public void OptimizeWordingAcceptsAShortQuestion() =>
    Assert.Empty(Items.Run(new OptimizeWordingRule(), Items.Qa("What is the capital of France?", "Paris")));

  [Theory]
  [InlineData(30, false)]
  [InlineData(5, true)]
  public void OptimizeWordingThresholdIsASetting(int maxWords, bool expected)
  {
    var options  = FormulationOptions.Default with { MaxQuestionWords = maxWords };
    var findings = Items.Run(new OptimizeWordingRule(), Items.Qa("What is the capital city of France today?", "Paris"), options);

    Assert.Equal(expected, findings.Count == 1);
  }
}
