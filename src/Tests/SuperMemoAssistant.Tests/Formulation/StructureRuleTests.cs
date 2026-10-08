// Rules about the shape of an item: sets, enumerations, and context cues.
namespace SuperMemoAssistant.Tests.Formulation;

using SuperMemoAssistant.Plugins.Formulation.Engine;
using SuperMemoAssistant.Plugins.Formulation.Rules;
using Xunit;

public sealed class StructureRuleTests
{
  [Theory]
  [InlineData("red, green, blue")]
  [InlineData("red, green, and blue")]
  [InlineData("red; green; blue")]
  [InlineData("red and green and blue")]
  [InlineData("France, Germany, Italy.")]
  public void AvoidSetsFlagsSeparatedParts(string answer)
  {
    var finding = Assert.Single(Items.Run(new AvoidSetsRule(), Items.Qa("Primary colors of light?", answer)));

    Assert.Equal(FindingSeverity.Warning, finding.Severity);
    Assert.Contains($"\"{answer}\" is a set of 3 parts", finding.Message);
  }

  [Fact]
  public void AvoidSetsFlagsAnHtmlList()
  {
    var item    = Items.Qa("Primary colors?", "red green blue") with { AnswerListItems = ["red", "green", "blue"] };
    var finding = Assert.Single(Items.Run(new AvoidSetsRule(), item));

    Assert.Contains("\"red, green, blue\" is a set of 3 parts", finding.Message);
  }

  [Theory]
  [InlineData("Paris")]
  [InlineData("salt and pepper")]
  [InlineData("In 1789, after years of famine, the people of Paris stormed the Bastille fortress")]
  [InlineData("It rained. Then it snowed, froze, and melted.")]
  public void AvoidSetsAcceptsSentencesAndSmallSets(string answer) =>
    Assert.Empty(Items.Run(new AvoidSetsRule(), Items.Qa("Q?", answer)));

  [Fact]
  public void AvoidSetsChecksTheClozeDeletion() =>
    Assert.Contains("The cloze deletion", Assert.Single(Items.Run(new AvoidSetsRule(), Items.Cloze("Colors: [...].", "red, green, blue"))).Message);

  [Fact]
  public void AvoidSetsMinimumPartsIsASetting()
  {
    var options = FormulationOptions.Default with { MinSetParts = 2 };

    Assert.Single(Items.Run(new AvoidSetsRule(), Items.Qa("Q?", "salt and pepper"), options));
  }

  [Theory]
  [InlineData("List the planets of the solar system.")]
  [InlineData("Name all EU founding members.")]
  [InlineData("What are the causes of inflation?")]
  [InlineData("Name the three branches of government.")]
  [InlineData("Give 5 examples of mammals.")]
  public void AvoidEnumerationsFlagsListQuestions(string question)
  {
    var finding = Assert.Single(Items.Run(new AvoidEnumerationsRule(), Items.Qa(question, "a")));

    Assert.Equal(FindingSeverity.Advice, finding.Severity);
    Assert.Contains($"\"{question}\" asks for a list", finding.Message);
    Assert.Contains("overlapping cloze deletions", finding.Message);
  }

  [Theory]
  [InlineData("What is the capital of France?")]
  [InlineData("Who listened to the radio broadcast?")]
  public void AvoidEnumerationsAcceptsSingleAnswerQuestions(string question) =>
    Assert.Empty(Items.Run(new AvoidEnumerationsRule(), Items.Qa(question, "a")));

  [Fact]
  public void AvoidEnumerationsIgnoresClozeStatements() =>
    Assert.Empty(Items.Run(new AvoidEnumerationsRule(), Items.Cloze("A linked list stores [...] in nodes.", "values")));

  [Theory]
  [InlineData("He discovered [...] in 1928.", "He")]
  [InlineData("Paris is big. Its population is [...].", "Its")]
  [InlineData("\"These\" cells produce [...].", "These")]
  public void ContextCuesFlagsAClozeSentenceThatStartsWithAPronoun(string question, string pronoun)
  {
    var finding = Assert.Single(Items.Run(new ContextCuesRule(), Items.Cloze(question, "x")));

    Assert.Equal(FindingSeverity.Advice, finding.Severity);
    Assert.Contains($"starts with \"{pronoun}\"", finding.Message);
  }

  [Theory]
  [InlineData("Fleming discovered [...] in 1928.")]
  [InlineData("He was born in Scotland. Fleming discovered [...] in 1928.")]
  [InlineData("[...] is the capital of France.")]
  [InlineData("Theory of [...] was proposed by Einstein.")]
  public void ContextCuesAcceptsClozeSentencesWithContext(string question) =>
    Assert.Empty(Items.Run(new ContextCuesRule(), Items.Cloze(question, "x")));

  [Fact]
  public void ContextCuesIgnoresQuestionAndAnswerItems() =>
    Assert.Empty(Items.Run(new ContextCuesRule(), Items.Qa("He discovered what?", "penicillin")));
}
