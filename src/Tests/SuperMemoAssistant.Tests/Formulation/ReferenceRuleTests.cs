// Rules about references: provide sources and provide date stamping.
namespace SuperMemoAssistant.Tests.Formulation;

using SuperMemoAssistant.Plugins.Formulation.Engine;
using SuperMemoAssistant.Plugins.Formulation.Rules;
using Xunit;

public sealed class ReferenceRuleTests
{
  [Fact]
  public void ProvideSourcesFlagsAnItemWithoutSourceOrLink()
  {
    var item    = Items.Qa("What is the capital of France?", "Paris", new ItemReferences(Title: "Geography"));
    var finding = Assert.Single(Items.Run(new ProvideSourcesRule(), item));

    Assert.Equal(FindingSeverity.Advice, finding.Severity);
    Assert.Contains("\"What is the capital of France?\" has no Source or Link reference", finding.Message);
    Assert.Equal("https://super-memory.com/articles/20rules.htm#sources", finding.Link.AbsoluteUri);
  }

  [Theory]
  [InlineData("Atlas", "")]
  [InlineData("", "https://example.org")]
  public void ProvideSourcesAcceptsASourceOrALink(string source, string link)
  {
    var item = Items.Qa("Q?", "A", new ItemReferences(Source: source, Link: link));

    Assert.Empty(Items.Run(new ProvideSourcesRule(), item));
  }

  [Fact]
  public void ProvideSourcesIsSilentWhenReferencesCannotBeRead() =>
    Assert.Empty(Items.Run(new ProvideSourcesRule(), Items.Qa("Q?", "A", references: null)));

  [Theory]
  [InlineData("Who is currently the president of France?", "Macron", "currently")]
  [InlineData("What is the latest version of .NET?", "10", "latest")]
  [InlineData("What do people use nowadays to pay?", "Phones", "nowadays")]
  [InlineData("Which team won the cup this year?", "Spain", "this year")]
  [InlineData("Capital of France?", "Paris, now and before", "now")]
  public void DateStampingFlagsVolatileWordsWithoutAYear(string question, string answer, string word)
  {
    var finding = Assert.Single(Items.Run(new DateStampingRule(), Items.Qa(question, answer, new ItemReferences())));

    Assert.Equal(FindingSeverity.Advice, finding.Severity);
    Assert.StartsWith($"\"{word}\" in \"", finding.Message);
  }

  [Theory]
  [InlineData("Who is currently (2026) the president of France?")]
  [InlineData("In 1999, what was the latest SuperMemo?")]
  [InlineData("What is the capital of France?")]
  [InlineData("Who knows the snowy road?")]
  public void DateStampingAcceptsDatedOrStableItems(string question) =>
    Assert.Empty(Items.Run(new DateStampingRule(), Items.Qa(question, "A", new ItemReferences())));

  [Fact]
  public void DateStampingAcceptsADateReference()
  {
    var item = Items.Qa("Who is currently the president of France?", "Macron", new ItemReferences(Date: "Oct 07, 2026"));

    Assert.Empty(Items.Run(new DateStampingRule(), item));
  }

  [Fact]
  public void DateStampingStillChecksWhenReferencesCannotBeRead() =>
    Assert.Single(Items.Run(new DateStampingRule(), Items.Qa("Who is currently the president?", "Macron", references: null)));
}
