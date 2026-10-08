// Kindle "My Clippings.txt" parsing, grouping, note attachment, planning, and re-import deduplication.
namespace SuperMemoAssistant.Tests.Books;

using SuperMemoAssistant.Plugins.Books.Kindle;
using SuperMemoAssistant.Plugins.Books.Planning;
using Xunit;

public sealed class KindleImportTests
{
  private const string Sep = "==========";

  private static readonly string Clippings = "\uFEFF" + string.Join(
    "\r\n",
    "Thinking, Fast and Slow (Kahneman, Daniel)",
    "- Your Highlight on page 20 | Location 300-302 | Added on Sunday, 10 March 2019 10:42:13",
    "",
    "The first highlight.",
    Sep,
    "\uFEFFThinking, Fast and Slow (Kahneman, Daniel)",
    "- Your Note on page 20 | Location 302 | Added on Sunday, 10 March 2019 10:43:00",
    "",
    "My note on the first highlight.",
    Sep,
    "Thinking, Fast and Slow (Kahneman, Daniel)",
    "- Your Bookmark on page 25 | Location 350 | Added on Sunday, 10 March 2019 10:50:00",
    "",
    "",
    Sep,
    "Thinking, Fast and Slow (Kahneman, Daniel)",
    "- Your Highlight on Location 120-121 | Added on Wednesday, March 6, 2019 7:58:10 PM",
    "",
    "An earlier passage,",
    "on two lines.",
    Sep,
    "Thinking, Fast and Slow (Kahneman, Daniel)",
    "- Your Note on Location 900 | Added on Wednesday, March 6, 2019 8:00:00 PM",
    "",
    "A note without a highlight.",
    Sep,
    "Thinking, Fast and Slow (Kahneman, Daniel)",
    "- Your Highlight on page 20 | Location 300-302 | Added on Monday, 11 March 2019 09:00:00",
    "",
    "The first   highlight.",
    Sep,
    "Untitled Notes",
    "- Your Highlight on Location 10-12 | Added on Friday, 1 January 2021 00:00:01",
    "",
    "A highlight in a document without an author.",
    Sep,
    "") + "\r\n";

  private static readonly PlanOptions Options = new(25, 0.5, 100);

  [Fact]
  public void Parser_HandlesBomCrlfNotesBookmarksAndMissingAuthor()
  {
    var result = KindleClippingsParser.Parse(Clippings);

    Assert.Equal(6, result.Clippings.Count);
    Assert.Equal(1, result.SkippedBookmarks);
    Assert.Equal(0, result.SkippedInvalid);

    var first = result.Clippings[0];
    Assert.Equal("Thinking, Fast and Slow", first.Title);
    Assert.Equal("Kahneman, Daniel", first.Author);
    Assert.Equal(KindleClippingKind.Highlight, first.Kind);
    Assert.Equal("20", first.Page);
    Assert.Equal((300, 302), (first.LocationStart, first.LocationEnd));
    Assert.Equal(new DateTime(2019, 3, 10, 10, 42, 13), first.Added);
    Assert.Equal("page 20, location 300-302", first.Position);

    Assert.Equal(KindleClippingKind.Note, result.Clippings[1].Kind);
    Assert.Equal("Thinking, Fast and Slow", result.Clippings[1].Title);
    Assert.Null(result.Clippings[2].Page);
    Assert.Equal("An earlier passage,\non two lines.", result.Clippings[2].Text);
    Assert.Equal(new DateTime(2019, 3, 6, 19, 58, 10), result.Clippings[2].Added);

    var untitled = result.Clippings[^1];
    Assert.Equal("Untitled Notes", untitled.Title);
    Assert.Null(untitled.Author);
  }

  [Theory]
  [InlineData("Book (Volume 2) (Jane Doe)", "Book (Volume 2)", "Jane Doe")]
  [InlineData("No Author Here", "No Author Here", null)]
  [InlineData("(Only Parentheses)", "(Only Parentheses)", null)]
  public void TitleLine_TakesTheLastParenthesizedGroupAsTheAuthor(string line, string title, string? author)
  {
    Assert.Equal((title, author), KindleClippingsParser.ParseTitleLine(line));
  }

  [Theory]
  [InlineData("Sunday, 10 March 2019 10:42:13", 2019, 3, 10, 10, 42, 13)]
  [InlineData("Wednesday, March 6, 2019 7:58:10 PM", 2019, 3, 6, 19, 58, 10)]
  [InlineData("Thursday, 7 November 2024 9:05:00 AM", 2024, 11, 7, 9, 5, 0)]
  public void Dates_WithEnglishMonthNamesAreParsed(string text, int y, int mo, int d, int h, int mi, int s)
  {
    Assert.Equal(new DateTime(y, mo, d, h, mi, s), KindleClippingsParser.ParseDate(text));
  }

  [Fact]
  public void AbbreviatedLocationEnd_IsExpanded()
  {
    var result = KindleClippingsParser.Parse("Book (A)\n- Highlight Loc. 1170-72 | Added on 1 May 2010\n\nText\n==========\n");

    Assert.Equal((1170, 1172), (result.Clippings[0].LocationStart, result.Clippings[0].LocationEnd));
  }

  [Fact]
  public void Grouper_AttachesOverlappingNotes_RemovesDuplicates_AndSortsByLocation()
  {
    var books = KindleGrouper.Group(KindleClippingsParser.Parse(Clippings).Clippings, new HashSet<string>());

    Assert.Equal(["Thinking, Fast and Slow", "Untitled Notes"], books.Select(b => b.Title));

    var book = books[0];
    Assert.Equal(1, book.DuplicateCount);
    Assert.Equal(3, book.Entries.Count);
    Assert.Equal([120, 300, 900], book.Entries.Select(e => e.Clipping.LocationStart ?? 0));
    Assert.Equal("My note on the first highlight.", Assert.Single(book.Entries[1].Notes).Text);
    Assert.Equal(KindleClippingKind.Note, book.Entries[2].Clipping.Kind);
    Assert.Empty(book.Entries[2].Notes);
  }

  [Fact]
  public void Planner_CreatesABookTopicWithOneChildPerHighlight()
  {
    var book = KindleGrouper.Group(KindleClippingsParser.Parse(Clippings).Clippings, new HashSet<string>())[0];
    var root = KindlePlanner.Plan(book, "My Clippings.txt", Options);

    Assert.Equal("Thinking, Fast and Slow", root.Title);
    Assert.Equal(25, root.Priority);
    Assert.Equal(3, root.Children.Count);
    Assert.Equal([25.5, 26.0, 26.5], root.Children.Select(c => c.Priority));

    var highlight = root.Children[1];
    Assert.Equal("The first highlight.", highlight.Title);
    Assert.Contains("<b>Note:</b> My note on the first highlight.", highlight.Html);
    Assert.Equal(new ReferenceInfo("Thinking, Fast and Slow", "Kahneman, Daniel", "Kindle highlight, page 20, location 300-302",
                                   "Sunday, 10 March 2019 10:42:13"), highlight.References);
    Assert.Equal(2, highlight.HighlightHashes.Count);
    Assert.Contains("An earlier passage,<br>on two lines.", root.Children[0].Html);
    Assert.StartsWith("Note: ", root.Children[2].Title);
  }

  [Fact]
  public void Hashes_AreStable_AndIgnoreDateAndWhitespace()
  {
    var a = new KindleClipping("Book", "Author", KindleClippingKind.Highlight, "1", 10, 12, "x", null, "Some  text\n here");
    var b = a with { AddedText = "y", Added = DateTime.Now, Text = "Some text here" };

    Assert.Equal(HighlightHasher.Hash(a), HighlightHasher.Hash(b));
    Assert.Equal("ec0bd14a214420d0c947501a94a7f592", HighlightHasher.Hash(a));
    Assert.NotEqual(HighlightHasher.Hash(a), HighlightHasher.Hash(a with { LocationStart = 11 }));
    Assert.NotEqual(HighlightHasher.Hash(a), HighlightHasher.Hash(a with { Kind = KindleClippingKind.Note }));
    Assert.NotEqual(HighlightHasher.Hash(a), HighlightHasher.Hash(a with { Title = "Other" }));
  }

  [Fact]
  public void ReImport_SkipsImportedClippings_AndKeepsNewOnes()
  {
    var clippings = KindleClippingsParser.Parse(Clippings).Clippings;
    var first     = KindleGrouper.Group(clippings, new HashSet<string>());
    var imported  = first.SelectMany(b => KindlePlanner.Plan(b, "My Clippings.txt", Options).DescendantsAndSelf())
                         .SelectMany(n => n.HighlightHashes)
                         .ToHashSet();

    var again = KindleGrouper.Group(clippings, imported);
    Assert.All(again, b => Assert.Empty(b.Entries));
    Assert.Empty(new KindleSource(KindleClippingsParser.Parse(Clippings), again, "My Clippings.txt").Plan(Options));

    var extended = Clippings + "Untitled Notes\r\n- Your Highlight on Location 20-21 | Added on Friday, 1 January 2021 00:00:02\r\n\r\nA new one.\r\n==========\r\n";
    var withNew  = KindleGrouper.Group(KindleClippingsParser.Parse(extended).Clippings, imported);

    Assert.Equal("A new one.", Assert.Single(withNew[1].Entries).Clipping.Text);
    Assert.Equal(1, withNew[1].DuplicateCount);
  }
}
