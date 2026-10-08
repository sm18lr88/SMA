namespace SuperMemoAssistant.Pdfium;

using System;
using System.Runtime.InteropServices;
using Native;

public sealed unsafe partial class PdfPage
{
  private Pause* _pause;
  private GCHandle _pauseOwner;
  private (PdfBitmap Bitmap, int X, int Y, int Width, int Height, PageRotate Rotate, RenderFlags Flags) _progressiveTarget;

  /// <summary>Renders the page, and its form fields when the document has a form engine, into a rectangle of the bitmap.</summary>
  public void Render(PdfBitmap bitmap, int x, int y, int width, int height, PageRotate rotate, RenderFlags flags)
  {
    ArgumentNullException.ThrowIfNull(bitmap);

    var pdfiumFlags = flags & ~RenderFlags.Thumbnail;
    NativeMethods.FPDF_RenderPageBitmap(bitmap.Handle, Handle, x, y, width, height, rotate, pdfiumFlags);
    Document.FormFill?.Draw(bitmap, this, x, y, width, height, rotate, pdfiumFlags);
  }

  /// <summary>Renders the page into a GDI device context, for example a printer.</summary>
  public void Render(IntPtr deviceContext, int x, int y, int width, int height, PageRotate rotate, RenderFlags flags)
  {
    if (NativeMethods.FPDF_RenderPage(deviceContext, Handle, x, y, width, height, rotate, flags & ~RenderFlags.Thumbnail) == 0)
      throw new PdfiumException(PdfiumError.Page);
  }

  /// <summary>
  ///   Starts a render that can pause. While it runs, <see cref="PdfPageCollection.ProgressiveRender" /> decides when to
  ///   yield. Call <see cref="ContinueProgressiveRender" /> until the render is done, then <see cref="CancelProgressiveRender" />.
  /// </summary>
  public ProgressiveStatus StartProgressiveRender(PdfBitmap bitmap, int x, int y, int width, int height,
                                                  PageRotate rotate, RenderFlags flags)
  {
    ArgumentNullException.ThrowIfNull(bitmap);
    CancelProgressiveRender();

    _pauseOwner = GCHandle.Alloc(this);
    _pause = (Pause*)NativeMemory.AllocZeroed((nuint)sizeof(Pause));
    _pause->Version = 1;
    _pause->NeedToPauseNow = &NeedToPauseNow;
    _pause->User = GCHandle.ToIntPtr(_pauseOwner);
    _progressiveTarget = (bitmap, x, y, width, height, rotate, flags & ~RenderFlags.Thumbnail);

    var status = (ProgressiveStatus)NativeMethods.FPDF_RenderPageBitmap_Start(
      bitmap.Handle, Handle, x, y, width, height, rotate, _progressiveTarget.Flags, _pause);

    return OnProgressiveStep(status);
  }

  public ProgressiveStatus ContinueProgressiveRender()
  {
    if (_pause == null)
      throw new InvalidOperationException("No progressive render is running for this page.");

    return OnProgressiveStep((ProgressiveStatus)NativeMethods.FPDF_RenderPage_Continue(Handle, _pause));
  }

  /// <summary>Ends a progressive render, whether it finished or not, and releases its resources.</summary>
  public void CancelProgressiveRender()
  {
    if (_pause == null)
      return;

    NativeMethods.FPDF_RenderPage_Close(_handle);
    NativeMemory.Free(_pause);
    _pause = null;
    _pauseOwner.Free();
    _progressiveTarget = default;
  }

  private ProgressiveStatus OnProgressiveStep(ProgressiveStatus status)
  {
    if (status == ProgressiveStatus.Done)
    {
      var (bitmap, x, y, width, height, rotate, flags) = _progressiveTarget;
      Document.FormFill?.Draw(bitmap, this, x, y, width, height, rotate, flags);
    }

    return status;
  }

  [UnmanagedCallersOnly]
  private static int NeedToPauseNow(Pause* pause)
  {
    try
    {
      var page = (PdfPage)GCHandle.FromIntPtr(pause->User).Target!;
      return page.Document.Pages.RaiseProgressiveRender(page) ? 1 : 0;
    }
    catch (Exception ex)
    {
      CallbackErrors.Report(ex);
      return 0;
    }
  }
}
