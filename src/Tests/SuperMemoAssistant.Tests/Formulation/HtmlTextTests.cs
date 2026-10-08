// HTML-to-text conversion, cloze markup, and reference parsing of the formulation advisor.
namespace SuperMemoAssistant.Tests.Formulation;

using System.Globalization;
using SuperMemoAssistant.Interop;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using SuperMemoAssistant.Plugins.Formulation.Text;
using Xunit;

public sealed class HtmlTextTests
{
  [Fact]
  public void PlainTextStripsTagsAndDecodesEntities()
  {
    var text = HtmlText.ToPlainText("<P>Fish &amp; <B>chips</B>&nbsp;are &lt;cheap&gt; &#233;t&eacute;</P>");

    Assert.Equal("Fish & chips are <cheap> été", text);
  }

  [Fact]
  public void PlainTextPutsBlockElementsOnSeparateLines()
  {
    var text = HtmlText.ToPlainText("first<br>second<div>third</div><ul><li>a</li><li>b</li></ul>");

    Assert.Equal("first\nsecond\nthird\na\nb", text);
  }

  [Fact]
  public void PlainTextDropsHeadScriptsStylesAndComments()
  {
    var html = "<html><head><title>T</title><style>p{}</style></head><body><script>x()</script>Visible<!-- hidden --></body></html>";

    Assert.Equal("Visible", HtmlText.ToPlainText(html));
  }

  [Fact]
  public void PlainTextDropsTheSuperMemoReferenceBlock()
  {
    var references = new References().WithTitle("Book").WithSource("Shelf").ToString();

    Assert.Equal("Answer", HtmlText.ToPlainText("Answer" + references));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("  <br> ")]
  public void PlainTextOfEmptyHtmlIsEmpty(string? html) => Assert.Equal(string.Empty, HtmlText.ToPlainText(html));

  [Fact]
  public void ListItemsReturnsEachEntry()
  {
    var items = HtmlText.ListItems("<OL><LI>red<LI>green</LI><li><b>blue</b></li></OL>");

    Assert.Equal(["red", "green", "blue"], items);
  }

  [Fact]
  public void ClozeSpansFindSuperMemoClozeMarkup()
  {
    var spans = HtmlText.ClozeSpans("The capital is <SPAN class=cloze>Paris</SPAN> and <span class=\"cloze\">[...]</span>.");

    Assert.Equal(["Paris", "[...]"], spans);
  }

  [Fact]
  public void ClozeSpansIgnoreOtherSpans() => Assert.Empty(HtmlText.ClozeSpans("<span class=reference>Paris</span>"));

  [Theory]
  [InlineData("Paris is the capital of [...].", 1)]
  [InlineData("[...] is the capital of [...].", 2)]
  [InlineData("Unicode [\u2026] placeholder", 1)]
  [InlineData("A [link] is not a placeholder", 0)]
  public void CountPlaceholders(string text, int expected) => Assert.Equal(expected, HtmlText.CountPlaceholders(text));

  [Theory]
  [InlineData("[...]", true)]
  [InlineData(" [...] ", true)]
  [InlineData("[year]", false)]
  [InlineData("x [...]", false)]
  public void IsPlaceholder(string text, bool expected) => Assert.Equal(expected, HtmlText.IsPlaceholder(text));

  [Fact]
  public void ReferenceParserReadsTheBlockThatSmaWrites()
  {
    var html = "Q" + new References().WithTitle("Biology").WithSource("Campbell").WithLink("https://example.org/a")
                                     .WithDate("2001").ToString();

    var references = ReferenceParser.Parse(html);

    Assert.NotNull(references);
    Assert.Equal("Biology", references.Title);
    Assert.Equal("Campbell", references.Source);
    Assert.Equal("https://example.org/a", references.Link);
    Assert.Equal("2001", references.Date);
    Assert.True(references.HasSource);
    Assert.True(references.HasDate);
  }

  [Fact]
  public void ReferenceParserReturnsNullWithoutABlock() => Assert.Null(ReferenceParser.Parse("<p>No references</p>"));

  [Fact]
  public void ReferenceParserReturnsEmptyReferencesForABlockWithoutSource()
  {
    var references = ReferenceParser.Parse(
      string.Format(CultureInfo.InvariantCulture, SMConst.Elements.ReferenceFormat, "#Title: Only a title"));

    Assert.NotNull(references);
    Assert.Equal("Only a title", references.Title);
    Assert.False(references.HasSource);
    Assert.False(references.HasDate);
  }
}
