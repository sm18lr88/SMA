namespace SuperMemoAssistant.Tests.Commands;

using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.SMA.Commands;
using Xunit;

public sealed class CommandPaletteModelTests
{
  private static readonly PaletteEntry ImportBook  = Entry("Books", "ImportBook", "Import a book or Kindle highlights", HotKeyScopes.SM);
  private static readonly PaletteEntry PdfExtract  = Entry("PDF", "Extract", "Create an extract", HotKeyScopes.SMBrowser);
  private static readonly PaletteEntry OmniMemo    = Entry("OmniMemo", "Show", "Show the OmniMemo window", HotKeyScopes.Global);
  private static readonly PaletteEntry Formulation = Entry("Formulation", "Check", "Check the formulation of the current item", HotKeyScopes.SM);

  private static readonly PaletteEntry[] All = [ImportBook, PdfExtract, OmniMemo, Formulation];

  [Theory]
  [InlineData("ibk", "Import a book or Kindle highlights")]
  [InlineData("IMPORT", "Import a book or Kindle highlights")]
  [InlineData("pdf extract", "Create an extract PDF")]
  [InlineData("omni", "Show the OmniMemo window OmniMemo")]
  public void TheMatcherFindsSubsequencesInAnyCase(string query, string text) =>
    Assert.NotNull(CommandMatcher.Score(query, text));

  [Theory]
  [InlineData("xyz", "Import a book")]
  [InlineData("import zebra", "Import a book")]
  [InlineData("kooB", "Book")]
  public void TheMatcherRejectsTextWithoutEveryWord(string query, string text) =>
    Assert.Null(CommandMatcher.Score(query, text));

  [Fact]
  public void WordStartsAndAdjacentCharactersRankHigher()
  {
    Assert.True(CommandMatcher.Score("form", "Check the formulation") > CommandMatcher.Score("form", "Transformer for magic"));
    Assert.True(CommandMatcher.Score("ck", "Check Kindle") > CommandMatcher.Score("ck", "Clock"));
    Assert.True(CommandMatcher.Score("ext", "Extract") > CommandMatcher.Score("ext", "Next text"));
  }

  [Fact]
  public void TheContextHidesCommandsThatNeedSuperMemoInTheForeground()
  {
    Assert.Equal(All.Length, Model(PaletteContext.ElementWindow).Results.Count);
    Assert.DoesNotContain(PdfExtract, Model(PaletteContext.SuperMemo).Results);
    Assert.Equal([OmniMemo], Model(PaletteContext.OtherApplication).Results);
  }

  [Fact]
  public void AnEmptyQueryListsRecentCommandsFirstThenByPluginAndTitle()
  {
    var registry = new CommandRegistry();
    registry.MarkUsed(OmniMemo.Command.Key);
    registry.MarkUsed(Formulation.Command.Key);

    var model = new CommandPaletteModel(All, registry.LastUsed, PaletteContext.ElementWindow);

    Assert.Equal([Formulation, OmniMemo, ImportBook, PdfExtract], model.Results);
    Assert.Same(Formulation, model.Selected);
  }

  [Fact]
  public void TypingRanksTheMatchesAndSelectsTheBest()
  {
    var model = Model(PaletteContext.ElementWindow);
    var changed = new List<string?>();
    model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

    model.Query = "kindle";

    Assert.Equal([ImportBook], model.Results);
    Assert.Same(ImportBook, model.Selected);
    Assert.Contains(nameof(CommandPaletteModel.Results), changed);

    model.Query = "no such command";

    Assert.Empty(model.Results);
    Assert.Null(model.Selected);
  }

  [Fact]
  public void TheSelectionStopsAtTheFirstAndTheLastCommand()
  {
    var model = Model(PaletteContext.ElementWindow);

    model.MoveSelection(-1);
    Assert.Equal(0, model.SelectedIndex);

    model.MoveSelection(100);
    Assert.Equal(All.Length - 1, model.SelectedIndex);
  }

  [Fact]
  public void TheRegistryReplacesByOwnerAndIdAndRemovesAStoppedPlugin()
  {
    var registry = new CommandRegistry();
    var ran      = "";

    registry.Register(ImportBook.Command, () => ran = "old");
    registry.Register(new PaletteCommand("Books", "Books", "ImportBook", "Import a book", null, HotKeyScopes.SM), () => ran = "new");
    registry.Register(OmniMemo.Command, () => { });

    Assert.Equal(2, registry.Snapshot().Count);
    registry.Snapshot().Single(e => e.Command.Owner == "Books").Execute();
    Assert.Equal("new", ran);

    Assert.Equal(1, registry.RemoveOwner("BOOKS"));
    registry.Unregister("OmniMemo", "Show");
    Assert.Empty(registry.Snapshot());
  }

  private static CommandPaletteModel Model(PaletteContext context) => new(All, _ => 0, context);

  private static PaletteEntry Entry(string owner, string id, string title, HotKeyScopes scopes) =>
    new(new PaletteCommand(owner, owner, id, title, null, scopes), () => { });
}
