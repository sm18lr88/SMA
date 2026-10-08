namespace SuperMemoAssistant.Tests.Pdfium;

using SuperMemoAssistant.Pdfium;
using Xunit;

public sealed class PdfBitmapTests
{
  [Fact]
  public void BlendRectMixesByAlphaAndClipsToTheBitmap()
  {
    using var bitmap = Solid(4, 4, unchecked((int)0xFF204060));

    bitmap.BlendRect(-2, -2, 4, 4, unchecked((int)0x80FFFFFF));
    bitmap.BlendRect(2, 2, 10, 10, unchecked((int)0xFF808080), BlendMode.Multiply);

    Assert.Equal(unchecked((int)0xFF8F9FAF), Pixel(bitmap, 1, 1));
    Assert.Equal(unchecked((int)0xFF204060), Pixel(bitmap, 2, 1));
    Assert.Equal(unchecked((int)0xFF102030), Pixel(bitmap, 3, 3));
  }

  [Fact]
  public void CompositeOverCopiesOpaqueSourcesAndBlendsTranslucentOnes()
  {
    using var target = Solid(2, 1, unchecked((int)0xFF000000));
    using var opaque = Solid(2, 1, unchecked((int)0xFFFF0000), hasAlpha: false);
    using var translucent = Solid(2, 1, unchecked((int)0x8000FF00));

    target.CompositeOver(opaque, 0, 0, 1, 1);
    target.CompositeOver(translucent, 1, 0, 1, 1);

    Assert.Equal(unchecked((int)0xFFFF0000), Pixel(target, 0, 0));
    Assert.Equal(unchecked((int)0xFF008000), Pixel(target, 1, 0));
  }

  [Theory]
  [InlineData(PageRotate.Rotate90, 1, 0)]
  [InlineData(PageRotate.Rotate180, 2, 1)]
  [InlineData(PageRotate.Rotate270, 0, 2)]
  public void RotateTurnsClockwise(PageRotate rotation, int markerX, int markerY)
  {
    using var source = Solid(3, 2, unchecked((int)0xFF000000));
    source.FillRect(0, 0, 1, 1, unchecked((int)0xFFFFFFFF));

    using var rotated = source.Rotate(rotation);

    Assert.Equal(rotation == PageRotate.Rotate180 ? (3, 2) : (2, 3), (rotated.Width, rotated.Height));
    Assert.Equal(unchecked((int)0xFFFFFFFF), Pixel(rotated, markerX, markerY));
  }

  private static PdfBitmap Solid(int width, int height, int argb, bool hasAlpha = true)
  {
    var bitmap = new PdfBitmap(width, height, hasAlpha);
    bitmap.FillRect(0, 0, width, height, argb);
    return bitmap;
  }

  private static int Pixel(PdfBitmap bitmap, int x, int y) => bitmap.GetImage().GetPixel(x, y).ToArgb();
}
