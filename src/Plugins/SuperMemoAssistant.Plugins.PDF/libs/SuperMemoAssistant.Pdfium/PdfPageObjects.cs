namespace SuperMemoAssistant.Pdfium;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Native;

/// <summary>A graphic object of a page: text, image, path, shading, or form XObject.</summary>
public class PdfPageObject
{
  internal PdfPageObject(PdfPage page, IntPtr handle, PageObjectType type)
  {
    Page = page;
    Handle = handle;
    ObjectType = type;
  }

  public PdfPage Page { get; }

  public IntPtr Handle { get; }

  public PageObjectType ObjectType { get; }

  public PdfRect BoundingBox =>
    NativeMethods.FPDFPageObj_GetBounds(Handle, out var left, out var bottom, out var right, out var top) != 0
      ? new PdfRect(left, top, right, bottom)
      : default;

  public Color FillColor =>
    NativeMethods.FPDFPageObj_GetFillColor(Handle, out var r, out var g, out var b, out var a) != 0
      ? Color.FromArgb((int)a, (int)r, (int)g, (int)b)
      : Color.Black;

  internal virtual void Release() { }

  internal static PdfPageObject Create(PdfPage page, IntPtr handle)
  {
    var type = (PageObjectType)NativeMethods.FPDFPageObj_GetType(handle);
    return type switch
    {
      PageObjectType.Text => new PdfTextObject(page, handle),
      PageObjectType.Image => new PdfImageObject(page, handle),
      PageObjectType.Form => new PdfFormObject(page, handle),
      _ => new PdfPageObject(page, handle, type),
    };
  }
}

public sealed class PdfTextObject : PdfPageObject
{
  internal PdfTextObject(PdfPage page, IntPtr handle)
    : base(page, handle, PageObjectType.Text) =>
    Font = new PdfFont(NativeMethods.FPDFTextObj_GetFont(handle));

  public PdfFont Font { get; }

  public float FontSize => NativeMethods.FPDFTextObj_GetFontSize(Handle, out var size) != 0 ? size : 0;

  /// <summary>The index of this object's first character in the page text, or -1 when the text layer has none.</summary>
  public int FirstCharIndex => Page.Text.GetObjectRange(Handle).First;

  public int CharsCount => Page.Text.GetObjectRange(Handle).Count;
}

public sealed class PdfImageObject : PdfPageObject
{
  private PdfBitmap? _bitmap;

  internal PdfImageObject(PdfPage page, IntPtr handle)
    : base(page, handle, PageObjectType.Image) { }

  /// <summary>The image as drawn on the page, with its mask and transform applied. The page owns the bitmap.</summary>
  public PdfBitmap? Bitmap
  {
    get
    {
      if (_bitmap != null)
        return _bitmap;

      var handle = NativeMethods.FPDFImageObj_GetRenderedBitmap(Page.Document.Handle, Page.Handle, Handle);
      if (handle == IntPtr.Zero)
        handle = NativeMethods.FPDFImageObj_GetBitmap(Handle);

      return _bitmap = handle == IntPtr.Zero ? null : new PdfBitmap(handle);
    }
  }

  internal override void Release()
  {
    _bitmap?.Dispose();
    _bitmap = null;
  }
}

public sealed class PdfFormObject : PdfPageObject
{
  internal PdfFormObject(PdfPage page, IntPtr handle)
    : base(page, handle, PageObjectType.Form) =>
    PageObjects = PdfPageObjectCollection.ForForm(page, handle);

  public PdfPageObjectCollection PageObjects { get; }

  internal override void Release() => PageObjects.Dispose();
}

public sealed unsafe class PdfFont
{
  private readonly IntPtr _handle;

  internal PdfFont(IntPtr handle) => _handle = handle;

  public FontFlags Flags => _handle == IntPtr.Zero ? FontFlags.None : (FontFlags)NativeMethods.FPDFFont_GetFlags(_handle);

  /// <summary>The weight, from 100 (thin) to 900 (heavy), or 0 when the font does not say.</summary>
  public int Weight => _handle == IntPtr.Zero ? 0 : Math.Max(NativeMethods.FPDFFont_GetWeight(_handle), 0);

  public string FamilyName
  {
    get
    {
      if (_handle == IntPtr.Zero)
        return string.Empty;

      var length = NativeMethods.FPDFFont_GetFamilyName(_handle, null, 0);
      if (length <= 1)
        return string.Empty;

      var buffer = new byte[length];
      fixed (byte* name = buffer)
        NativeMethods.FPDFFont_GetFamilyName(_handle, name, length);

      return System.Text.Encoding.UTF8.GetString(buffer, 0, (int)length - 1);
    }
  }
}

/// <summary>The objects of a page or of a form XObject, in drawing order.</summary>
public sealed class PdfPageObjectCollection : IReadOnlyList<PdfPageObject>, IDisposable
{
  private readonly List<PdfPageObject> _objects;

  private PdfPageObjectCollection(List<PdfPageObject> objects) => _objects = objects;

  public int Count => _objects.Count;

  public PdfPageObject this[int index] => _objects[index];

  public int IndexOf(PdfPageObject pageObject) => _objects.IndexOf(pageObject);

  /// <summary>Returns the smallest rectangle that contains every object, or an empty rectangle when there is none.</summary>
  public PdfRect CalculateBoundingBox() =>
    _objects.Count == 0 ? default : _objects.Select(o => o.BoundingBox).Aggregate((a, b) => a.Union(b));

  public IEnumerator<PdfPageObject> GetEnumerator() => _objects.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void Dispose()
  {
    foreach (var pageObject in _objects)
      pageObject.Release();
  }

  internal static PdfPageObjectCollection ForPage(PdfPage page) =>
    new(Enumerable.Range(0, NativeMethods.FPDFPage_CountObjects(page.Handle))
                  .Select(i => PdfPageObject.Create(page, NativeMethods.FPDFPage_GetObject(page.Handle, i)))
                  .ToList());

  internal static PdfPageObjectCollection ForForm(PdfPage page, IntPtr form) =>
    new(Enumerable.Range(0, NativeMethods.FPDFFormObj_CountObjects(form))
                  .Select(i => PdfPageObject.Create(page, NativeMethods.FPDFFormObj_GetObject(form, (uint)i)))
                  .ToList());
}
