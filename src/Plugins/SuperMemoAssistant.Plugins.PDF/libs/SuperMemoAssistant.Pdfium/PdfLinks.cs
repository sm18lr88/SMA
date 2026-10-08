namespace SuperMemoAssistant.Pdfium;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Native;

/// <summary>A link annotation: it either names a destination or carries an action.</summary>
public sealed class PdfLink
{
  internal PdfLink(PdfDocument document, IntPtr handle)
  {
    Destination = PdfDestination.FromHandle(document, NativeMethods.FPDFLink_GetDest(document.Handle, handle));
    Action = PdfAction.FromHandle(document, NativeMethods.FPDFLink_GetAction(handle));
  }

  public PdfDestination? Destination { get; }

  public PdfAction? Action { get; }
}

/// <summary>The link annotations of a page.</summary>
public sealed class PdfLinkCollection
{
  private readonly PdfPage _page;

  internal PdfLinkCollection(PdfPage page) => _page = page;

  public PdfLink? GetLinkAtPoint(float x, float y)
  {
    var handle = NativeMethods.FPDFLink_GetLinkAtPoint(_page.Handle, x, y);
    return handle == IntPtr.Zero ? null : new PdfLink(_page.Document, handle);
  }
}

/// <summary>A URL that appears as plain text on a page.</summary>
public sealed record PdfWebLink(string Url, IReadOnlyList<PdfRect> Rects);

/// <summary>The URLs that PDFium detects in the text of a page.</summary>
public sealed unsafe class PdfWebLinkCollection : IReadOnlyList<PdfWebLink>, IDisposable
{
  private readonly List<PdfWebLink> _links = [];
  private IntPtr _handle;

  internal PdfWebLinkCollection(PdfText text)
  {
    _handle = NativeMethods.FPDFLink_LoadWebLinks(text.Handle);
    if (_handle == IntPtr.Zero)
      return;

    for (int i = 0, count = NativeMethods.FPDFLink_CountWebLinks(_handle); i < count; i++)
      _links.Add(new PdfWebLink(GetUrl(i), GetRects(i)));
  }

  public int Count => _links.Count;

  public PdfWebLink this[int index] => _links[index];

  public PdfWebLink? GetWebLinkAtPoint(float x, float y) =>
    _links.FirstOrDefault(link => link.Rects.Any(rect => rect.Contains(x, y)));

  public IEnumerator<PdfWebLink> GetEnumerator() => _links.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void Dispose()
  {
    if (_handle == IntPtr.Zero)
      return;

    NativeMethods.FPDFLink_CloseWebLinks(_handle);
    _handle = IntPtr.Zero;
  }

  private string GetUrl(int index)
  {
    var length = NativeMethods.FPDFLink_GetURL(_handle, index, null, 0);
    if (length <= 1)
      return string.Empty;

    var buffer = new char[length];
    fixed (char* url = buffer)
      NativeMethods.FPDFLink_GetURL(_handle, index, url, length);

    return new string(buffer, 0, length - 1);
  }

  private PdfRect[] GetRects(int index)
  {
    var rects = new PdfRect[Math.Max(NativeMethods.FPDFLink_CountRects(_handle, index), 0)];
    for (var i = 0; i < rects.Length; i++)
    {
      NativeMethods.FPDFLink_GetRect(_handle, index, i, out var left, out var top, out var right, out var bottom);
      rects[i] = new PdfRect((float)left, (float)top, (float)right, (float)bottom);
    }

    return rects;
  }
}
