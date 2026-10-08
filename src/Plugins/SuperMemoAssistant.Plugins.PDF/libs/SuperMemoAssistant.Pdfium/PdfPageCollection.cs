namespace SuperMemoAssistant.Pdfium;

using System;
using System.Collections;
using System.Collections.Generic;
using Native;

/// <summary>The pages of a document. Each index keeps one <see cref="PdfPage" />, which loads its native page on demand.</summary>
public sealed class PdfPageCollection : IReadOnlyList<PdfPage>
{
  private readonly PdfDocument _document;
  private readonly PdfPage?[] _pages;
  private int _currentIndex;

  internal PdfPageCollection(PdfDocument document)
  {
    _document = document;
    _pages = new PdfPage?[NativeMethods.FPDF_GetPageCount(document.Handle)];
  }

  public event EventHandler? CurrentPageChanged;

  /// <summary>Raised while a progressive render runs. Set <see cref="ProgressiveRenderEventArgs.NeedPause" /> to yield.</summary>
  public event EventHandler<ProgressiveRenderEventArgs>? ProgressiveRender;

  public int Count => _pages.Length;

  public PdfPage this[int index] => _pages[index] ??= new PdfPage(_document, index);

  /// <summary>The page that the user works on. Viewers and the form engine share it.</summary>
  public int CurrentIndex
  {
    get => _currentIndex;
    set
    {
      if (value < 0 || value >= Count)
        throw new ArgumentOutOfRangeException(nameof(value), value, "The page index is out of range.");
      if (value == _currentIndex)
        return;

      _currentIndex = value;
      CurrentPageChanged?.Invoke(this, EventArgs.Empty);
    }
  }

  public PdfPage CurrentPage => this[CurrentIndex];

  public int GetPageIndex(PdfPage page) => ReferenceEquals(page.Document, _document) ? page.Index : -1;

  /// <summary>Returns the page size in points without loading the page.</summary>
  public (double Width, double Height) GetPageSize(int index) =>
    NativeMethods.FPDF_GetPageSizeByIndex(_document.Handle, index, out var width, out var height) != 0
      ? (width, height)
      : throw new PdfiumException(PdfiumError.Page);

  public IEnumerator<PdfPage> GetEnumerator()
  {
    for (var i = 0; i < Count; i++)
      yield return this[i];
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  internal PdfPage? FindLoaded(IntPtr handle)
  {
    foreach (var page in _pages)
      if (page is not null && page.LoadedHandle == handle)
        return page;

    return null;
  }

  internal bool RaiseProgressiveRender(PdfPage page)
  {
    var args = new ProgressiveRenderEventArgs(page);
    ProgressiveRender?.Invoke(this, args);
    return args.NeedPause;
  }

  internal void CloseAll()
  {
    foreach (var page in _pages)
      page?.Dispose();
  }
}
