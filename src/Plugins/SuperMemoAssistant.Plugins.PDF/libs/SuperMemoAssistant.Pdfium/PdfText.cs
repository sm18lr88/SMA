namespace SuperMemoAssistant.Pdfium;

using System;
using System.Collections.Generic;
using Native;

/// <summary>The text layer of a page. Character indexes follow PDFium's reading order and include generated line breaks.</summary>
public sealed unsafe class PdfText : IDisposable
{
  private PdfWebLinkCollection? _webLinks;
  private Dictionary<IntPtr, (int First, int Count)>? _objectRanges;

  internal PdfText(PdfPage page)
  {
    Page = page;
    Handle = NativeMethods.FPDFText_LoadPage(page.Handle);
    if (Handle == IntPtr.Zero)
      throw new PdfiumException(PdfiumError.Page);
  }

  internal IntPtr Handle { get; private set; }

  internal PdfPage Page { get; }

  public int CountChars => NativeMethods.FPDFText_CountChars(Handle);

  public PdfWebLinkCollection WebLinks => _webLinks ??= new PdfWebLinkCollection(this);

  public void Dispose()
  {
    if (Handle == IntPtr.Zero)
      return;

    _webLinks?.Dispose();
    _webLinks = null;
    NativeMethods.FPDFText_ClosePage(Handle);
    Handle = IntPtr.Zero;
  }

  public char GetCharacter(int index) => (char)NativeMethods.FPDFText_GetUnicode(Handle, index);

  /// <summary>Returns the text of <paramref name="count" /> characters from <paramref name="start" />, clipped to the page.</summary>
  public string GetText(int start, int count)
  {
    count = Math.Min(count, CountChars - start);
    if (start < 0 || count <= 0)
      return string.Empty;

    var buffer = new char[count + 1];
    int written;
    fixed (char* text = buffer)
      written = NativeMethods.FPDFText_GetText(Handle, start, count, text);

    return written > 1 ? new string(buffer, 0, written - 1) : string.Empty;
  }

  /// <summary>Returns the index of the character at a page point, or -1 when no character is within the tolerance.</summary>
  public int GetCharIndexAtPos(float x, float y, float xTolerance, float yTolerance) =>
    Math.Max(NativeMethods.FPDFText_GetCharIndexAtPos(Handle, x, y, xTolerance, yTolerance), -1);

  /// <summary>Returns the bounding box of one character.</summary>
  public PdfRect GetCharBox(int index) =>
    NativeMethods.FPDFText_GetCharBox(Handle, index, out var left, out var right, out var bottom, out var top) != 0
      ? new PdfRect((float)left, (float)top, (float)right, (float)bottom)
      : default;

  /// <summary>Returns the text and the line rectangles of a character range.</summary>
  public PdfTextInfo GetTextInfo(int start, int count)
  {
    var rectCount = NativeMethods.FPDFText_CountRects(Handle, start, count);
    var rects = new PdfRect[Math.Max(rectCount, 0)];

    for (var i = 0; i < rects.Length; i++)
    {
      NativeMethods.FPDFText_GetRect(Handle, i, out var left, out var top, out var right, out var bottom);
      rects[i] = new PdfRect((float)left, (float)top, (float)right, (float)bottom);
    }

    return new PdfTextInfo(GetText(start, count), rects);
  }

  /// <summary>Finds every occurrence of <paramref name="text" /> on the page.</summary>
  public IReadOnlyList<(int CharIndex, int CharCount)> FindAll(string text, FindFlags flags)
  {
    ArgumentException.ThrowIfNullOrEmpty(text);

    var matches = new List<(int, int)>();
    var search = NativeMethods.FPDFText_FindStart(Handle, text, flags, 0);
    if (search == IntPtr.Zero)
      return matches;

    try
    {
      while (NativeMethods.FPDFText_FindNext(search) != 0)
        matches.Add((NativeMethods.FPDFText_GetSchResultIndex(search), NativeMethods.FPDFText_GetSchCount(search)));
    }
    finally
    {
      NativeMethods.FPDFText_FindClose(search);
    }

    return matches;
  }

  /// <summary>Returns the character range of a text object, or (-1, 0) when the object has no characters in the text layer.</summary>
  internal (int First, int Count) GetObjectRange(IntPtr textObject)
  {
    if (_objectRanges is null)
    {
      _objectRanges = new Dictionary<IntPtr, (int First, int Count)>();
      for (int i = 0, count = CountChars; i < count; i++)
      {
        var owner = NativeMethods.FPDFText_GetTextObject(Handle, i);
        if (owner == IntPtr.Zero)
          continue;

        _objectRanges[owner] = _objectRanges.TryGetValue(owner, out var range)
          ? (range.First, i - range.First + 1)
          : (i, 1);
      }
    }

    return _objectRanges.TryGetValue(textObject, out var found) ? found : (-1, 0);
  }
}

/// <summary>The text of a character range and the rectangles that cover it, one per line segment.</summary>
public sealed record PdfTextInfo(string Text, IReadOnlyList<PdfRect> Rects);
