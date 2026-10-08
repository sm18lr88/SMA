namespace SuperMemoAssistant.Pdfium;

using System;
using System.Drawing;
using Native;

/// <summary>
///   A page of a <see cref="PdfDocument" />. The native page loads on first use. <see cref="Dispose" /> releases it to save
///   memory; the next use loads it again.
/// </summary>
public sealed partial class PdfPage : IDisposable
{
  private IntPtr _handle;
  private PdfText? _text;
  private PdfLinkCollection? _links;
  private PdfPageObjectCollection? _pageObjects;
  private PageRotate? _originalRotation;

  internal PdfPage(PdfDocument document, int index)
  {
    Document = document;
    Index = index;
  }

  /// <summary>Raised after the native page is released.</summary>
  public event EventHandler? Disposed;

  public PdfDocument Document { get; }

  public int Index { get; }

  public bool IsLoaded => _handle != IntPtr.Zero;

  public IntPtr Handle
  {
    get
    {
      if (_handle != IntPtr.Zero)
        return _handle;

      _handle = NativeMethods.FPDF_LoadPage(Document.Handle, Index);
      if (_handle == IntPtr.Zero)
        throw new PdfiumException((PdfiumError)NativeMethods.FPDF_GetLastError());

      _originalRotation ??= (PageRotate)NativeMethods.FPDFPage_GetRotation(_handle);
      Document.FormFill?.OnPageLoaded(_handle);
      return _handle;
    }
  }

  /// <summary>Width in points (1/72 inch), after the page's own rotation.</summary>
  public float Width => NativeMethods.FPDF_GetPageWidthF(Handle);

  /// <summary>Height in points (1/72 inch), after the page's own rotation.</summary>
  public float Height => NativeMethods.FPDF_GetPageHeightF(Handle);

  public PageRotate Rotation
  {
    get => (PageRotate)NativeMethods.FPDFPage_GetRotation(Handle);
    set => NativeMethods.FPDFPage_SetRotation(Handle, (int)value);
  }

  /// <summary>The rotation that the file defines, before any change through <see cref="Rotation" />.</summary>
  public PageRotate OriginalRotation
  {
    get
    {
      _ = Handle;
      return _originalRotation!.Value;
    }
  }

  public bool HasTransparency => NativeMethods.FPDFPage_HasTransparency(Handle) != 0;

  public PdfText Text => _text ??= new PdfText(this);

  public PdfLinkCollection Links => _links ??= new PdfLinkCollection(this);

  public PdfPageObjectCollection PageObjects => _pageObjects ??= PdfPageObjectCollection.ForPage(this);

  internal IntPtr LoadedHandle => _handle;

  public void Dispose()
  {
    if (_handle == IntPtr.Zero)
      return;

    CancelProgressiveRender();
    _pageObjects?.Dispose();
    _pageObjects = null;
    _text?.Dispose();
    _text = null;
    _links = null;

    Document.FormFill?.OnPageClosing(_handle);
    NativeMethods.FPDF_ClosePage(_handle);
    _handle = IntPtr.Zero;

    Disposed?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>Converts a device point, relative to a page drawn at the given bounds, into page coordinates.</summary>
  public void DeviceToPage(int startX, int startY, int sizeX, int sizeY, PageRotate rotate, int deviceX, int deviceY,
                           out double pageX, out double pageY)
  {
    NativeMethods.FPDF_DeviceToPage(Handle, startX, startY, sizeX, sizeY, rotate, deviceX, deviceY, out pageX, out pageY);
  }

  /// <summary>Converts a page point into device coordinates, for a page drawn at the given bounds.</summary>
  public Point PageToDevice(int startX, int startY, int sizeX, int sizeY, PageRotate rotate, double pageX, double pageY)
  {
    NativeMethods.FPDF_PageToDevice(Handle, startX, startY, sizeX, sizeY, rotate, pageX, pageY, out var x, out var y);
    return new Point(x, y);
  }
}
