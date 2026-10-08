namespace SuperMemoAssistant.Tests.PDF;

using SuperMemoAssistant.Plugins.PDF.Extracts;
using Xunit;

public sealed class ExtractTitlesTests
{
  [Fact]
  public void HtmlToPlainTextStripsTagsDecodesEntitiesAndCollapsesWhitespace()
  {
    var html = "<p><span style=\"color:red\">Photo</span>synthesis &amp; light</p>\r\n<p>&quot;Energy&quot;&nbsp;&#233;t\u00E9<br/>end</p>"
               + "<style>p { color: red; }</style>";

    Assert.Equal("Photosynthesis & light \"Energy\" \u00E9t\u00E9 end", ExtractTitles.HtmlToPlainText(html));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("<p> </p><br>")]
  public void HtmlToPlainTextReturnsEmptyForNoText(string? html)
  {
    Assert.Equal(string.Empty, ExtractTitles.HtmlToPlainText(html));
  }

  [Fact]
  public void TruncateKeepsShortText()
  {
    Assert.Equal("Short text", ExtractTitles.Truncate("Short text", 10));
  }

  [Fact]
  public void TruncateCutsAtTheLastWordBoundaryAndAddsAnEllipsis()
  {
    var title = ExtractTitles.Truncate("The quick brown fox jumps over the lazy dog", 20);

    Assert.Equal("The quick brown fox\u2026", title);
    Assert.True(title.Length <= 20);
  }

  [Fact]
  public void TruncateRemovesTrailingPunctuationBeforeTheEllipsis()
  {
    Assert.Equal("First clause\u2026", ExtractTitles.Truncate("First clause, second clause", 16));
  }

  [Fact]
  public void TruncateCutsCjkTextWithoutSpacesByCharacters()
  {
    var title = ExtractTitles.Truncate("光合作用是植物利用光能把二氧化碳和水转化为有机物的过程", 10);

    Assert.Equal("光合作用是植物利用\u2026", title);
  }

  [Fact]
  public void TruncateCutsALongWordByCharactersInsteadOfLeavingATinyTitle()
  {
    Assert.Equal("A Pneumonoultram\u2026", ExtractTitles.Truncate("A Pneumonoultramicroscopicsilicovolcanoconiosis case", 17));
  }

  [Fact]
  public void TruncateDoesNotSplitASurrogatePair()
  {
    var title = ExtractTitles.Truncate("abcd\U0001F600efgh", 6);

    Assert.Equal("abcd\u2026", title);
  }

  [Fact]
  public void TextExtractTitleUsesTheBeginningOfTheText()
  {
    Assert.Equal("Mitochondria are the\u2026",
                 ExtractTitles.TextExtractTitle("<p>Mitochondria are the powerhouse of the cell.</p>", 22, "Biology"));
  }

  [Fact]
  public void TextExtractTitleFallsBackToTheArticleTitleWhenThereIsNoText()
  {
    Assert.Equal("Biology", ExtractTitles.TextExtractTitle("<p>&nbsp;</p>", 80, "Biology"));
  }

  [Theory]
  [InlineData(0, 0, "p. 1")]
  [InlineData(2, 6, "p. 3-7")]
  public void FormatPageRangeUsesOneBasedPageNumbers(int start, int end, string expected)
  {
    Assert.Equal(expected, ExtractTitles.FormatPageRange(start, end));
  }

  [Fact]
  public void SubPdfTitleUsesTheBookmarkOrTheDocumentTitle()
  {
    Assert.Equal("Chapter 2 (p. 10-14)", ExtractTitles.SubPdfTitle(" Chapter 2 ", "Book", 9, 13));
    Assert.Equal("Book (p. 4)", ExtractTitles.SubPdfTitle(null, "Book", 3, 3));
  }

  [Fact]
  public void ImageExtractTitleListsTheImagesAndPages()
  {
    Assert.Equal("Book::Chapter 1 -- Image extract: 2 images from p3, p5",
                 ExtractTitles.ImageExtractTitle("Book::Chapter 1", 2, [2, 4]));
    Assert.Equal("Book -- Image extract: 1 image from p1", ExtractTitles.ImageExtractTitle("Book", 1, [0]));
  }
}
