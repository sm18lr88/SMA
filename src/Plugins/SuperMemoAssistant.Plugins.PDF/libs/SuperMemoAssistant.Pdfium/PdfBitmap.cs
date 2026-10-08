namespace SuperMemoAssistant.Pdfium;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using Native;

/// <summary>A PDFium device-independent bitmap. Pixels are stored as BGRA (or BGRx when the bitmap has no alpha).</summary>
public sealed partial class PdfBitmap : IDisposable
{
  private const int FormatGray = 1;
  private const int FormatBgr = 2;
  private const int FormatBgrx = 3;
  private const int FormatBgra = 4;
  private const int FormatBgraPremultiplied = 5;

  private Bitmap? _image;

  /// <summary>Creates a bitmap. PDFium does not initialize the pixels: fill or render the whole bitmap before reading it.</summary>
  public PdfBitmap(int width, int height, bool hasAlpha)
    : this(CreateHandle(width, height, hasAlpha)) { }

  /// <summary>Takes ownership of a bitmap handle that PDFium returned.</summary>
  internal PdfBitmap(IntPtr handle)
  {
    Handle = handle;
    Width = NativeMethods.FPDFBitmap_GetWidth(handle);
    Height = NativeMethods.FPDFBitmap_GetHeight(handle);
    Stride = NativeMethods.FPDFBitmap_GetStride(handle);
    Buffer = NativeMethods.FPDFBitmap_GetBuffer(handle);
    Format = NativeMethods.FPDFBitmap_GetFormat(handle);
  }

  public IntPtr Handle { get; private set; }

  public int Width { get; }

  public int Height { get; }

  public int Stride { get; }

  /// <summary>Address of the first scan line.</summary>
  public IntPtr Buffer { get; }

  public bool HasAlpha => Format is FormatBgra or FormatBgraPremultiplied;

  internal int Format { get; }

  public void Dispose()
  {
    _image?.Dispose();
    _image = null;

    if (Handle == IntPtr.Zero)
      return;

    NativeMethods.FPDFBitmap_Destroy(Handle);
    Handle = IntPtr.Zero;
  }

  /// <summary>Replaces the pixels of a rectangle with <paramref name="argb" />.</summary>
  public void FillRect(int x, int y, int width, int height, int argb)
  {
    NativeMethods.FPDFBitmap_FillRect(Handle, x, y, width, height, unchecked((uint)argb));
  }

  /// <summary>
  ///   Returns a <see cref="Bitmap" /> that shares this bitmap's pixels, so drawing on it changes this bitmap. The image is
  ///   cached and disposed with this bitmap.
  /// </summary>
  public Bitmap GetImage()
  {
    if (_image != null)
      return _image;

    var pixelFormat = Format switch
    {
      FormatGray => PixelFormat.Format8bppIndexed,
      FormatBgr => PixelFormat.Format24bppRgb,
      FormatBgrx => PixelFormat.Format32bppRgb,
      FormatBgra => PixelFormat.Format32bppArgb,
      FormatBgraPremultiplied => PixelFormat.Format32bppPArgb,
      _ => throw new NotSupportedException($"PDFium bitmap format {Format} is not supported."),
    };

    _image = new Bitmap(Width, Height, Stride, pixelFormat, Buffer);

    if (Format == FormatGray)
    {
      var palette = _image.Palette;
      for (var i = 0; i < 256; i++)
        palette.Entries[i] = Color.FromArgb(i, i, i);

      _image.Palette = palette;
    }

    return _image;
  }

  private static IntPtr CreateHandle(int width, int height, bool hasAlpha)
  {
    PdfLibrary.Initialize();

    var handle = NativeMethods.FPDFBitmap_Create(width, height, hasAlpha ? 1 : 0);
    return handle != IntPtr.Zero
      ? handle
      : throw new PdfiumException($"PDFium could not create a {width} x {height} bitmap.");
  }
}
