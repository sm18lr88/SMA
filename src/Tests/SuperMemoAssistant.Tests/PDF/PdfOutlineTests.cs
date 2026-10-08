namespace SuperMemoAssistant.Tests.PDF;

using SuperMemoAssistant.Pdfium;
using SuperMemoAssistant.Plugins.PDF.Extensions;
using SuperMemoAssistant.Tests.Pdfium;
using Xunit;

/// <summary>Maps the real outline of <see cref="TestPdf" />: "Chapter 1" (page 1) with the child "Section 1.1" (page 2).</summary>
public sealed class PdfOutlineTests : IDisposable
{
  private readonly PdfDocument _document = PdfDocument.Load(TestPdf.Build());

  public void Dispose() => _document.Dispose();

  [Fact]
  public void SelectionsCoverWholePagesUntilTheNextSectionOrTheEnd()
  {
    var outline = new PdfOutline(_document);
    var chapter = _document.Bookmarks[0];
    var section = chapter.Children[0];
    int lastPageChars = _document.Pages[1].Text.CountChars;

    var chapterSel = Assert.NotNull(outline.GetSelection(chapter));
    var sectionSel = Assert.NotNull(outline.GetSelection(section));

    Assert.Equal((0, 0, 1, lastPageChars), (chapterSel.StartPage, chapterSel.StartIndex, chapterSel.EndPage, chapterSel.EndIndex));
    Assert.Equal((1, 0, 1, lastPageChars), (sectionSel.StartPage, sectionSel.StartIndex, sectionSel.EndPage, sectionSel.EndIndex));
  }

  [Fact]
  public void FindsTheDeepestBookmarkOfAPageAndMapsNodesBack()
  {
    var outline = new PdfOutline(_document);
    var chapter = _document.Bookmarks[0];
    var section = chapter.Children[0];

    Assert.Same(chapter, outline.FindBookmark(0));
    Assert.Same(section, outline.FindBookmark(1));

    var node = Assert.IsType<SuperMemoAssistant.Plugins.PDF.Extracts.OutlineNode>(outline.GetNode(section));
    Assert.Equal("Section 1.1", node.Title);
    Assert.Same(section, outline.GetBookmark(node));
  }
}
