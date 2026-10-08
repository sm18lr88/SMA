// Planning of the SuperMemo tree: priorities, references, and the children limit.
namespace SuperMemoAssistant.Tests.Books;

using SuperMemoAssistant.Plugins.Books.Epub;
using SuperMemoAssistant.Plugins.Books.Planning;
using Xunit;

public sealed class BookPlanningTests
{
  private static readonly EpubMetadata Metadata = new("Big Book", ["Ann Writer", "Bob Helper"], "en", "2001", "id", "9780306406157");

  private static List<BookChapter> Chapters(int count) =>
    Enumerable.Range(1, count).Select(i => new BookChapter(i, $"Chapter {i}", $"<p>Text {i}</p>", 2000, 1)).ToList();

  private static void AssertWithinLimit(ImportNode node, int limit)
  {
    Assert.True(node.Children.Count <= limit, $"{node.Title} has {node.Children.Count} children");
    foreach (var child in node.Children)
      AssertWithinLimit(child, limit);
  }

  private static IEnumerable<ImportNode> Leaves(ImportNode node) =>
    node.Children.Count == 0 ? [node] : node.Children.SelectMany(Leaves);

  [Fact]
  public void BookWithMoreChaptersThanTheLimit_IsNestedInParts()
  {
    var book = EpubPlanner.Plan(Metadata, "big.epub", Chapters(30), new PlanOptions(30, 0.1, 5));

    AssertWithinLimit(book, 5);
    Assert.Equal(Enumerable.Range(1, 30).Select(i => $"Chapter {i}"), Leaves(book).Select(n => n.Title));
    Assert.Equal(2, book.Children.Count);
    Assert.Equal("Big Book: chapters 1-25", book.Children[0].Title);
    Assert.Equal("Big Book: chapters 26-30", book.Children[1].Title);
    Assert.Equal("Big Book: chapters 1-5", book.Children[0].Children[0].Title);
    Assert.Equal(1 + 30 + 6 + 2, book.Count);
  }

  [Fact]
  public void BookWithinTheLimit_IsFlat()
  {
    var book = EpubPlanner.Plan(Metadata, "small.epub", Chapters(5), new PlanOptions(30, 0.1, 5));

    Assert.Equal(5, book.Children.Count);
    Assert.All(book.Children, c => Assert.Empty(c.Children));
  }

  [Fact]
  public void References_AndPriorities_FollowTheBook()
  {
    var book = EpubPlanner.Plan(Metadata, "big.epub", Chapters(3), new PlanOptions(30, 0.5, 100));

    Assert.Equal(new ReferenceInfo("Big Book", "Ann Writer; Bob Helper", "big.epub, ISBN 9780306406157", "2001"), book.References);
    Assert.All(book.Children, c => Assert.Equal(book.References, c.References));
    Assert.Equal(30, book.Priority);
    Assert.Equal([30.5, 31.0, 31.5], book.Children.Select(c => c.Priority));
    Assert.Contains("<li>Chapter 2</li>", book.Html);
  }

  [Theory]
  [InlineData(30, 0.2, 0, 30.2)]
  [InlineData(30, 0.2, 9, 32)]
  [InlineData(99.9, 1, 5, 100)]
  [InlineData(-5, 0.5, 0, 0.5)]
  [InlineData(150, 0, 0, 100)]
  [InlineData(30, -1, 3, 30)]
  [InlineData(double.NaN, double.NaN, 1, 0)]
  public void ChildPriorities_GrowWithTheIndex_AndStayWithin0To100(double parent, double step, int index, double expected)
  {
    Assert.Equal(expected, PriorityCalculator.ForChild(parent, step, index), 6);
  }

  [Fact]
  public void ChildPriorities_AreMonotonic()
  {
    var values = Enumerable.Range(0, 2000).Select(i => PriorityCalculator.ForChild(10, 0.2, i)).ToList();

    Assert.All(values, v => Assert.InRange(v, 0, 100));
    Assert.Equal(values.Order(), values);
  }

  [Fact]
  public void LimitBelowTwo_IsRejected()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => EpubPlanner.Plan(Metadata, "x.epub", Chapters(3), new PlanOptions(30, 0.1, 1)));
  }
}
