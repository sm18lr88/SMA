namespace SuperMemoAssistant.Pdfium;

using System;
using System.Collections;
using System.Collections.Generic;
using Native;

/// <summary>An entry of the document outline.</summary>
public sealed unsafe class PdfBookmark
{
  private readonly PdfDocument _document;
  private PdfBookmarkCollection? _children;

  internal PdfBookmark(PdfDocument document, IntPtr handle, PdfBookmark? parent)
  {
    _document = document;
    Handle = handle;
    Parent = parent;
    Title = ReadTitle(handle);
    Destination = PdfDestination.FromHandle(document, NativeMethods.FPDFBookmark_GetDest(document.Handle, handle));
    Action = PdfAction.FromHandle(document, NativeMethods.FPDFBookmark_GetAction(handle));
  }

  public string Title { get; }

  public PdfBookmark? Parent { get; }

  public PdfBookmarkCollection Children => _children ??= new PdfBookmarkCollection(_document, this);

  public PdfDestination? Destination { get; }

  public PdfAction? Action { get; }

  internal IntPtr Handle { get; }

  private static string ReadTitle(IntPtr handle)
  {
    var length = NativeMethods.FPDFBookmark_GetTitle(handle, null, 0);
    if (length <= 2)
      return string.Empty;

    var buffer = new char[length / 2];
    fixed (char* title = buffer)
      NativeMethods.FPDFBookmark_GetTitle(handle, title, length);

    return new string(buffer, 0, buffer.Length - 1);
  }
}

/// <summary>The top-level outline entries of a document, or the children of one entry.</summary>
public sealed class PdfBookmarkCollection : IReadOnlyList<PdfBookmark>
{
  private readonly List<PdfBookmark> _bookmarks = [];

  internal PdfBookmarkCollection(PdfDocument document, PdfBookmark? parent)
  {
    // A damaged outline can link its siblings in a cycle.
    var seen = new HashSet<IntPtr>();
    var handle = NativeMethods.FPDFBookmark_GetFirstChild(document.Handle, parent?.Handle ?? IntPtr.Zero);

    while (handle != IntPtr.Zero && seen.Add(handle))
    {
      _bookmarks.Add(new PdfBookmark(document, handle, parent));
      handle = NativeMethods.FPDFBookmark_GetNextSibling(document.Handle, handle);
    }
  }

  public int Count => _bookmarks.Count;

  public PdfBookmark this[int index] => _bookmarks[index];

  public int IndexOf(PdfBookmark bookmark) => _bookmarks.IndexOf(bookmark);

  public IEnumerator<PdfBookmark> GetEnumerator() => _bookmarks.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
