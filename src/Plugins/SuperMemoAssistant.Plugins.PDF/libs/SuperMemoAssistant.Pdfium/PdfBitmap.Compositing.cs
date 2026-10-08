namespace SuperMemoAssistant.Pdfium;

using System;

public sealed partial class PdfBitmap
{
  /// <summary>Mixes <paramref name="argb" /> into a rectangle, weighted by the color's alpha.</summary>
  public unsafe void BlendRect(int x, int y, int width, int height, int argb, BlendMode mode = BlendMode.Normal)
  {
    RequireFourBytesPerPixel();
    if (!Clip(ref x, ref y, ref width, ref height))
      return;

    int sa = (argb >> 24) & 0xFF, sr = (argb >> 16) & 0xFF, sg = (argb >> 8) & 0xFF, sb = argb & 0xFF;
    if (sa == 0)
      return;

    for (var row = y; row < y + height; row++)
    {
      var pixel = (byte*)Buffer + (long)row * Stride + x * 4;
      for (var col = 0; col < width; col++, pixel += 4)
      {
        pixel[0] = Mix(pixel[0], mode == BlendMode.Multiply ? pixel[0] * sb / 255 : sb, sa);
        pixel[1] = Mix(pixel[1], mode == BlendMode.Multiply ? pixel[1] * sg / 255 : sg, sa);
        pixel[2] = Mix(pixel[2], mode == BlendMode.Multiply ? pixel[2] * sr / 255 : sr, sa);
        pixel[3] = HasAlpha ? (byte)(sa + pixel[3] * (255 - sa) / 255) : (byte)0xFF;
      }
    }
  }

  /// <summary>
  ///   Draws a rectangle of <paramref name="source" /> over the same rectangle of this bitmap. Both bitmaps must have the
  ///   same size. An opaque source is copied; a source with alpha is composited over this bitmap.
  /// </summary>
  public unsafe void CompositeOver(PdfBitmap source, int x, int y, int width, int height)
  {
    ArgumentNullException.ThrowIfNull(source);
    RequireFourBytesPerPixel();
    source.RequireFourBytesPerPixel();
    if (source.Width != Width || source.Height != Height)
      throw new ArgumentException("The source bitmap must have the same size as this bitmap.", nameof(source));
    if (!Clip(ref x, ref y, ref width, ref height))
      return;

    for (var row = y; row < y + height; row++)
    {
      var src = (byte*)source.Buffer + (long)row * source.Stride + x * 4;
      var dst = (byte*)Buffer + (long)row * Stride + x * 4;

      if (!source.HasAlpha)
      {
        System.Buffer.MemoryCopy(src, dst, width * 4L, width * 4L);
        for (var col = 0; col < width; col++)
          dst[col * 4 + 3] = 0xFF;

        continue;
      }

      for (var col = 0; col < width; col++, src += 4, dst += 4)
      {
        int sa = src[3];
        dst[0] = Mix(dst[0], src[0], sa);
        dst[1] = Mix(dst[1], src[1], sa);
        dst[2] = Mix(dst[2], src[2], sa);
        dst[3] = HasAlpha ? (byte)(sa + dst[3] * (255 - sa) / 255) : (byte)0xFF;
      }
    }
  }

  /// <summary>Returns a new bitmap that shows this bitmap turned clockwise by <paramref name="rotation" />.</summary>
  public unsafe PdfBitmap Rotate(PageRotate rotation)
  {
    RequireFourBytesPerPixel();

    var quarterTurn = rotation is PageRotate.Rotate90 or PageRotate.Rotate270;
    var result = new PdfBitmap(quarterTurn ? Height : Width, quarterTurn ? Width : Height, HasAlpha);

    for (var y = 0; y < Height; y++)
    {
      var src = (uint*)((byte*)Buffer + (long)y * Stride);
      for (var x = 0; x < Width; x++)
      {
        var (dx, dy) = rotation switch
        {
          PageRotate.Rotate90 => (Height - 1 - y, x),
          PageRotate.Rotate180 => (Width - 1 - x, Height - 1 - y),
          PageRotate.Rotate270 => (y, Width - 1 - x),
          _ => (x, y),
        };

        *(uint*)((byte*)result.Buffer + (long)dy * result.Stride + dx * 4) = src[x];
      }
    }

    return result;
  }

  private static byte Mix(int destination, int source, int alpha) =>
    (byte)(destination + (source - destination) * alpha / 255);

  private bool Clip(ref int x, ref int y, ref int width, ref int height)
  {
    var right = Math.Min(x + width, Width);
    var bottom = Math.Min(y + height, Height);
    x = Math.Max(x, 0);
    y = Math.Max(y, 0);
    width = right - x;
    height = bottom - y;
    return width > 0 && height > 0;
  }

  private void RequireFourBytesPerPixel()
  {
    if (Format is not (FormatBgrx or FormatBgra or FormatBgraPremultiplied))
      throw new NotSupportedException("This operation needs a 32-bit BGRx or BGRA bitmap.");
  }
}
