namespace SuperMemoAssistant.Pdfium;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Native;

/// <summary>
///   The interactive form engine (AcroForm). Pass an instance to <see cref="PdfDocument.Load(string, PdfForms, string)" />
///   to fill forms; one instance can serve one open document at a time.
/// </summary>
public sealed unsafe partial class PdfForms
{
  private readonly Dictionary<int, Timer> _timers = [];
  private FormFillInfo* _info;
  private GCHandle _self;
  private PdfDocument? _document;
  private int _highlightArgb;
  private int _lastTimerId;

  /// <summary>The form engine asks to repaint a rectangle of a page.</summary>
  public event EventHandler<InvalidatePageEventArgs>? Invalidate;

  /// <summary>The form engine reports a selected text rectangle inside a field.</summary>
  public event EventHandler<InvalidatePageEventArgs>? OutputSelectedRect;

  public event EventHandler<SetCursorEventArgs>? SetCursor;

  /// <summary>A form action or a named action (for example, NextPage) asks to show a page.</summary>
  public event EventHandler<GoToPageEventArgs>? GoToPage;

  /// <summary>
  ///   The context that runs field timers, such as the caret blink. Without one, fields work but the caret does not blink.
  ///   Set it to the UI thread's context, because PDFium must not run on two threads at once.
  /// </summary>
  public SynchronizationContext? SynchronizationContext { get; set; }

  internal IntPtr Handle { get; private set; }

  internal bool IsAttached => _document != null;

  /// <summary>Sets the ARGB color that highlights every form field, and returns the previous color.</summary>
  public int SetHighlightColor(int argb)
  {
    var previous = _highlightArgb;
    _highlightArgb = argb;
    ApplyHighlightColor();
    return previous;
  }

  /// <summary>Removes the keyboard focus from the active field and commits its value.</summary>
  public void ForceToKillFocus()
  {
    if (Handle != IntPtr.Zero)
      NativeMethods.FORM_ForceToKillFocus(Handle);
  }

  internal void Attach(PdfDocument document)
  {
    _self = GCHandle.Alloc(this);
    _info = AllocateInfo(GCHandle.ToIntPtr(_self));
    Handle = NativeMethods.FPDFDOC_InitFormFillEnvironment(document.Handle, _info);
    _document = document;

    if (Handle == IntPtr.Zero)
      return;

    ApplyHighlightColor();
    NativeMethods.FORM_DoDocumentOpenAction(Handle);
  }

  internal void Detach(PdfDocument document)
  {
    if (!ReferenceEquals(_document, document))
      return;

    foreach (var timer in _timers.Values)
      timer.Dispose();

    _timers.Clear();

    if (Handle != IntPtr.Zero)
      NativeMethods.FPDFDOC_ExitFormFillEnvironment(Handle);

    Handle = IntPtr.Zero;
    NativeMemory.Free(_info);
    _info = null;
    _self.Free();
    _document = null;
  }

  internal void OnPageLoaded(IntPtr page)
  {
    if (Handle != IntPtr.Zero)
      NativeMethods.FORM_OnAfterLoadPage(page, Handle);
  }

  internal void OnPageClosing(IntPtr page)
  {
    if (Handle != IntPtr.Zero)
      NativeMethods.FORM_OnBeforeClosePage(page, Handle);
  }

  internal void Draw(PdfBitmap bitmap, PdfPage page, int x, int y, int width, int height, PageRotate rotate,
                     RenderFlags flags)
  {
    if (Handle != IntPtr.Zero)
      NativeMethods.FPDF_FFLDraw(Handle, bitmap.Handle, page.Handle, x, y, width, height, rotate, flags);
  }

  private void ApplyHighlightColor()
  {
    if (Handle == IntPtr.Zero)
      return;

    NativeMethods.FPDF_SetFormFieldHighlightColor(Handle, FormFieldType.Unknown, (uint)_highlightArgb & 0x00FF_FFFF);
    NativeMethods.FPDF_SetFormFieldHighlightAlpha(Handle, (byte)((uint)_highlightArgb >> 24));
  }
}
