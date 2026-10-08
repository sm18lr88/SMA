// Building the plain-text item model: cloze detection, deletions, lists, references, and component roles.
namespace SuperMemoAssistant.Tests.Formulation;

using SuperMemoAssistant.Interop.SuperMemo.Content.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using SuperMemoAssistant.Plugins.Formulation.Engine;
using SuperMemoAssistant.Plugins.Formulation.Integration;
using Xunit;

public sealed class ItemModelBuilderTests
{
  private static FormulationItem Build(string question, string answer, bool referencesReadable = true) =>
    ItemModelBuilder.Build(new ItemHtml([question], [answer], referencesReadable));

  [Fact]
  public void QuestionAndAnswerItemIsNotCloze()
  {
    var item = Build("<p>What is the capital of France?</p>", "<p>Paris</p>");

    Assert.False(item.IsCloze);
    Assert.Equal(0, item.ClozePlaceholderCount);
    Assert.Empty(item.ClozeDeletions);
    Assert.Equal("What is the capital of France?", item.Question);
    Assert.Equal(["Paris"], item.RecalledTexts);
  }

  [Fact]
  public void SuperMemoClozeMarkupGivesTheDeletion()
  {
    var item = Build("The capital of France is <SPAN class=cloze>[...]</SPAN>.", "<SPAN class=cloze>Paris</SPAN>");

    Assert.True(item.IsCloze);
    Assert.Equal(1, item.ClozePlaceholderCount);
    Assert.Equal(["Paris"], item.ClozeDeletions);
  }

  [Fact]
  public void AnswerWithoutMarkupIsTheDeletion()
  {
    var item = Build("The capital of France is [...].", "Paris");

    Assert.Equal(["Paris"], item.ClozeDeletions);
  }

  [Fact]
  public void AnswerThatRepeatsTheSentenceGivesTheTextBehindThePlaceholder()
  {
    var item = Build("The capital of France is [...] since 508.", "The capital of France is Paris since 508.");

    Assert.Equal(["Paris"], item.ClozeDeletions);
  }

  [Fact]
  public void ClozeHintCountsAsAPlaceholder()
  {
    var item = Build("Rome was founded in <span class=cloze>[year]</span>.", "753 BC");

    Assert.True(item.IsCloze);
    Assert.Equal(1, item.ClozePlaceholderCount);
  }

  [Fact]
  public void SeveralPlaceholdersAreCounted()
  {
    var item = Build("[...] is the capital of [...].", "Paris, France");

    Assert.Equal(2, item.ClozePlaceholderCount);
  }

  [Fact]
  public void AnswerListEntriesAreCollected()
  {
    var item = Build("Primary colors?", "<ul><li>red</li><li>green</li><li>blue</li></ul>");

    Assert.Equal(["red", "green", "blue"], item.AnswerListItems);
  }

  [Fact]
  public void ReferencesComeFromAnyComponent()
  {
    var references = new References().WithSource("Atlas").ToString();
    var item       = Build("Capital of France?", "Paris" + references);

    Assert.Equal("Paris", item.Answer);
    Assert.Equal("Atlas", item.References?.Source);
  }

  [Fact]
  public void MissingReferenceBlockGivesEmptyReferences()
  {
    var item = Build("Capital of France?", "Paris");

    Assert.NotNull(item.References);
    Assert.False(item.References.HasSource);
  }

  [Fact]
  public void UnreadableReferencesAreNull() => Assert.Null(Build("Capital of France?", "Paris", referencesReadable: false).References);

  [Fact]
  public void SeveralComponentsAreJoined()
  {
    var item = ItemModelBuilder.Build(new ItemHtml(["Part one", "part two"], ["Answer"], true));

    Assert.Equal("Part one\npart two", item.Question);
  }

  [Theory]
  [InlineData(AtFlags.All, ComponentRole.Question)]
  [InlineData(AtFlags.Question | AtFlags.Browsing, ComponentRole.Question)]
  [InlineData(AtFlags.NonQuestion, ComponentRole.Answer)]
  [InlineData(AtFlags.Answer, ComponentRole.Answer)]
  [InlineData(AtFlags.AfterGrading, ComponentRole.Answer)]
  [InlineData(AtFlags.Editing | AtFlags.Browsing, ComponentRole.None)]
  public void ComponentRoleFollowsTheDisplayAtFlags(AtFlags displayAt, ComponentRole expected) =>
    Assert.Equal(expected, ComponentRoles.Classify(displayAt));
}
