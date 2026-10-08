namespace SuperMemoAssistant.Tests.Pdfium;

using SuperMemoAssistant.Pdfium;
using Xunit;

public sealed class PdfRenderingTests : IDisposable
{
  private const int White = unchecked((int)0xFFFFFFFF);

  private readonly PdfDocument _document = PdfDocument.Load(TestPdf.Build());

  public void Dispose() => _document.Dispose();

  [Fact]
  public void RendersTextAndImagePixels()
  {
    using var bitmap = RenderPage(_document.Pages[0]);

    // The image is red, green, blue, and yellow, scaled to 100 x 100 points at (300, 500).
    var image = bitmap.GetImage();
    Assert.True(CountDarkPixels(bitmap, 72, 70, 180, 30) > 50);
    var red = image.GetPixel(325, 217);
    Assert.True(red.R > 200 && red.G < 60 && red.B < 60, $"The top-left image cell should be red, but it is {red}.");
    Assert.Equal(System.Drawing.Color.FromArgb(255, 255, 255), image.GetPixel(500, 700));
  }

  [Fact]
  public void ProgressiveRenderPausesAndCompletes()
  {
    var page = _document.Pages[0];
    var pauseRequests = 0;
    _document.Pages.ProgressiveRender += (_, e) => e.NeedPause = ++pauseRequests == 1;

    using var bitmap = new PdfBitmap(612, 792, false);
    bitmap.FillRect(0, 0, 612, 792, White);

    var status = page.StartProgressiveRender(bitmap, 0, 0, 612, 792, PageRotate.Normal, RenderFlags.Annotations);
    for (var steps = 0; status == ProgressiveStatus.ToBeContinued && steps < 1000; steps++)
      status = page.ContinueProgressiveRender();

    page.CancelProgressiveRender();

    Assert.Equal(ProgressiveStatus.Done, status);
    Assert.True(pauseRequests > 0);
    Assert.True(CountDarkPixels(bitmap, 72, 70, 180, 30) > 50);
  }

  [Fact]
  public void FormEngineFindsAndDrawsFields()
  {
    var forms = new PdfForms();
    forms.SetHighlightColor(unchecked((int)0x8000FF00));

    using var document = PdfDocument.Load(TestPdf.Build(), forms);
    var page = document.Pages[0];
    var (left, bottom, right, top) = TestPdf.FieldBox;

    Assert.Equal(FormFieldType.TextField, page.GetFormFieldAtPoint((left + right) / 2, (bottom + top) / 2));
    Assert.Equal(FormFieldType.None, page.GetFormFieldAtPoint(500, 100));
    Assert.Equal(FormFieldType.None, _document.Pages[0].GetFormFieldAtPoint((left + right) / 2, (bottom + top) / 2));

    using var bitmap = RenderPage(page);
    var fieldPixel = bitmap.GetImage().GetPixel((int)left + 150, 792 - (int)bottom - 5);
    Assert.True(fieldPixel.G > fieldPixel.R, $"The field highlight should tint the field green, but it is {fieldPixel}.");

    forms.ForceToKillFocus();
  }

  [Fact]
  public void FormEngineServesOneDocumentAtATime()
  {
    var forms = new PdfForms();
    using var first = PdfDocument.Load(TestPdf.Build(), forms);

    Assert.Throws<InvalidOperationException>(() => PdfDocument.Load(TestPdf.Build(), forms));

    first.Dispose();
    using var second = PdfDocument.Load(TestPdf.Build(), forms);
    Assert.Same(forms, second.FormFill);
  }

  private static PdfBitmap RenderPage(PdfPage page)
  {
    var bitmap = new PdfBitmap(612, 792, false);
    bitmap.FillRect(0, 0, 612, 792, White);
    page.Render(bitmap, 0, 0, 612, 792, PageRotate.Normal, RenderFlags.Annotations);
    return bitmap;
  }

  private static int CountDarkPixels(PdfBitmap bitmap, int x, int y, int width, int height)
  {
    var image = bitmap.GetImage();
    var dark = 0;
    for (var row = y; row < y + height; row++)
      for (var col = x; col < x + width; col++)
        if (image.GetPixel(col, row).GetBrightness() < 0.5f)
          dark++;

    return dark;
  }
}
