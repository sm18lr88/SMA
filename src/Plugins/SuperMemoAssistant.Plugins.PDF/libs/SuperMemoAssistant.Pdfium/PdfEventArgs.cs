namespace SuperMemoAssistant.Pdfium;

using System;

/// <summary>Event data that carries one value.</summary>
public class EventArgs<T>(T value) : EventArgs
{
  public T Value { get; } = value;
}

/// <summary>A page asks whether a progressive render should yield to the caller.</summary>
public sealed class ProgressiveRenderEventArgs(PdfPage page) : EventArgs
{
  public PdfPage Page { get; } = page;

  /// <summary>Set to true to pause the render; the caller resumes it with <see cref="PdfPage.ContinueProgressiveRender" />.</summary>
  public bool NeedPause { get; set; }
}

/// <summary>The form engine asks to repaint, or to highlight, a rectangle of a page.</summary>
public sealed class InvalidatePageEventArgs(PdfPage page, PdfRect rect) : EventArgs
{
  public PdfPage Page { get; } = page;

  public PdfRect Rect { get; } = rect;
}

/// <summary>The form engine asks for another mouse cursor.</summary>
public sealed class SetCursorEventArgs(CursorType cursor) : EventArgs
{
  public CursorType Cursor { get; } = cursor;
}

/// <summary>The form engine asks to show a page.</summary>
public sealed class GoToPageEventArgs(int pageIndex) : EventArgs
{
  public int PageIndex { get; } = pageIndex;
}
