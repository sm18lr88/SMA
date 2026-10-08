namespace SuperMemoAssistant.Tests.Pdfium;

using SuperMemoAssistant.Pdfium;
using Xunit;

public sealed class PdfDocumentTests : IDisposable
{
  private readonly PdfDocument _document = PdfDocument.Load(TestPdf.Build());

  public void Dispose() => _document.Dispose();

  [Fact]
  public void ReadsMetadataAndPageGeometry()
  {
    Assert.Equal(TestPdf.Title, _document.Title);
    Assert.Equal(TestPdf.Author, _document.Author);
    Assert.StartsWith("D:2026", _document.CreationDate);
    Assert.Equal(2, _document.Pages.Count);
    Assert.Equal((612d, 792d), _document.Pages.GetPageSize(1));
    Assert.Equal(612f, _document.Pages[0].Width);
    Assert.Equal(PageRotate.Normal, _document.Pages[0].OriginalRotation);
  }

  [Fact]
  public void ExtractsTextAndLocatesCharacters()
  {
    var text = _document.Pages[0].Text;

    Assert.StartsWith(TestPdf.Sentence, text.GetText(0, text.CountChars));
    Assert.Equal('H', text.GetCharacter(0));

    var box = text.GetCharBox(0);
    Assert.True(box.Width > 0 && box.Height > 0);
    Assert.Equal(0, text.GetCharIndexAtPos((box.Left + box.Right) / 2, (box.Top + box.Bottom) / 2, 1, 1));
    Assert.Equal(-1, text.GetCharIndexAtPos(500, 100, 1, 1));

    var info = text.GetTextInfo(0, TestPdf.Sentence.Length);
    Assert.Equal(TestPdf.Sentence, info.Text);
    Assert.Single(info.Rects);
    Assert.True(info.Rects[0].Contains(TestPdf.TextX + 10, TestPdf.TextY + 5));

    var match = Assert.Single(text.FindAll("pdfium", FindFlags.None));
    Assert.Equal((6, 6), match);
    Assert.Empty(text.FindAll("pdfium", FindFlags.MatchCase));
  }

  [Fact]
  public void ReadsTheOutlineAndNamedDestinations()
  {
    var chapter = Assert.Single(_document.Bookmarks);
    Assert.Equal("Chapter 1", chapter.Title);
    Assert.Null(chapter.Parent);
    Assert.Equal(new PdfDestination(0, DestinationType.Xyz, 72, 720, null, null, 2), chapter.Destination);

    var section = Assert.Single(chapter.Children);
    Assert.Equal("Section 1.1", section.Title);
    Assert.Same(chapter, section.Parent);
    Assert.Equal(1, section.Destination!.PageIndex);
    Assert.Equal(DestinationType.Fit, section.Destination.DestinationType);
    Assert.Equal(0, _document.Bookmarks.IndexOf(chapter));

    var intro = _document.NamedDestinations["intro"];
    Assert.Equal(new PdfDestination(1, DestinationType.FitH, null, 500, null, null, null, "intro"), intro);
    Assert.Null(_document.NamedDestinations["missing"]);
  }

  [Fact]
  public void FindsLinkAnnotationsAndWebLinks()
  {
    var page = _document.Pages[0];

    var link = page.Links.GetLinkAtPoint(100, 700);
    Assert.NotNull(link);
    Assert.Equal(1, link.Destination!.PageIndex);
    Assert.Null(page.Links.GetLinkAtPoint(500, 100));

    var webLink = Assert.Single(page.Text.WebLinks);
    Assert.Equal(TestPdf.Url, webLink.Url);
    var rect = webLink.Rects[0];
    Assert.Same(webLink, page.Text.WebLinks.GetWebLinkAtPoint((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2));
  }

  [Fact]
  public void EnumeratesPageObjectsWithFontsAndImages()
  {
    var objects = _document.Pages[0].PageObjects;

    var title = Assert.IsType<PdfTextObject>(objects[0]);
    Assert.Equal(0, title.FirstCharIndex);
    Assert.Equal(TestPdf.Sentence.Length, title.CharsCount);
    // Helvetica is a standard font that the file does not embed, so PDFium reports the system substitute it draws with.
    Assert.Contains(title.Font.FamilyName, new[] { "Helvetica", "Arial" });
    Assert.Equal(24f, title.FontSize);
    Assert.Equal(System.Drawing.Color.Black.ToArgb(), title.FillColor.ToArgb());

    var image = Assert.IsType<PdfImageObject>(objects.Single(o => o.ObjectType == PageObjectType.Image));
    Assert.Equal(2, objects.IndexOf(image));
    Assert.True(image.BoundingBox.Contains(350, 550));
    Assert.NotNull(image.Bitmap);
    Assert.True(image.Bitmap.Width >= 2);
    Assert.NotNull(image.Bitmap.GetImage());

    var bounds = objects.CalculateBoundingBox();
    Assert.True(bounds.Contains(TestPdf.TextX + 1, TestPdf.TextY + 1) && bounds.Contains(350, 550));
  }

  [Fact]
  public void DisposedPagesReloadOnNextUse()
  {
    var page = _document.Pages[0];
    var disposed = 0;
    page.Disposed += (_, _) => disposed++;

    _ = page.Text.CountChars;
    page.Dispose();

    Assert.False(page.IsLoaded);
    Assert.Equal(1, disposed);
    Assert.StartsWith("Hello", page.Text.GetText(0, 5));
    Assert.True(page.IsLoaded);
  }

  [Fact]
  public void LoadsFromAUnicodePath()
  {
    var folder = Directory.CreateTempSubdirectory("sma-pdfium-");
    try
    {
      var path = Path.Combine(folder.FullName, "中文 ドキュメント.pdf");
      File.WriteAllBytes(path, TestPdf.Build());

      using var document = PdfDocument.Load(path);
      Assert.Equal(2, document.Pages.Count);
    }
    finally
    {
      folder.Delete(true);
    }
  }

  [Fact]
  public void RejectsDataThatIsNotAPdf()
  {
    var ex = Assert.Throws<PdfiumException>(() => PdfDocument.Load("not a pdf"u8.ToArray()));
    Assert.Equal(PdfiumError.Format, ex.Error);
  }
}
