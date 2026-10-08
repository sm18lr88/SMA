namespace SuperMemoAssistant.Pdfium.Native;

using System;
using System.Runtime.InteropServices;

internal static unsafe partial class NativeMethods
{
  [LibraryImport(Library)]
  internal static partial IntPtr FPDFBookmark_GetFirstChild(IntPtr document, IntPtr bookmark);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFBookmark_GetNextSibling(IntPtr document, IntPtr bookmark);

  [LibraryImport(Library)]
  internal static partial uint FPDFBookmark_GetTitle(IntPtr bookmark, void* buffer, uint length);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFBookmark_GetDest(IntPtr document, IntPtr bookmark);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFBookmark_GetAction(IntPtr bookmark);

  [LibraryImport(Library)]
  internal static partial uint FPDFAction_GetType(IntPtr action);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFAction_GetDest(IntPtr document, IntPtr action);

  [LibraryImport(Library)]
  internal static partial uint FPDFAction_GetFilePath(IntPtr action, void* buffer, uint length);

  [LibraryImport(Library)]
  internal static partial uint FPDFAction_GetURIPath(IntPtr document, IntPtr action, void* buffer, uint length);

  [LibraryImport(Library)]
  internal static partial int FPDFDest_GetDestPageIndex(IntPtr document, IntPtr destination);

  [LibraryImport(Library)]
  internal static partial uint FPDFDest_GetView(IntPtr destination, out uint paramCount, float* parameters);

  [LibraryImport(Library)]
  internal static partial int FPDFDest_GetLocationInPage(IntPtr destination, out int hasX, out int hasY, out int hasZoom,
                                                         out float x, out float y, out float zoom);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDF_GetNamedDestByName(IntPtr document, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFLink_GetLinkAtPoint(IntPtr page, double x, double y);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFLink_GetDest(IntPtr document, IntPtr link);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFLink_GetAction(IntPtr link);

  [LibraryImport(Library)]
  internal static partial int FPDFPage_CountObjects(IntPtr page);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFPage_GetObject(IntPtr page, int index);

  [LibraryImport(Library)]
  internal static partial int FPDFFormObj_CountObjects(IntPtr formObject);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFFormObj_GetObject(IntPtr formObject, uint index);

  [LibraryImport(Library)]
  internal static partial int FPDFPageObj_GetType(IntPtr pageObject);

  [LibraryImport(Library)]
  internal static partial int FPDFPageObj_GetBounds(IntPtr pageObject, out float left, out float bottom, out float right,
                                                    out float top);

  [LibraryImport(Library)]
  internal static partial int FPDFPageObj_GetFillColor(IntPtr pageObject, out uint r, out uint g, out uint b, out uint a);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFTextObj_GetFont(IntPtr textObject);

  [LibraryImport(Library)]
  internal static partial int FPDFTextObj_GetFontSize(IntPtr textObject, out float size);

  [LibraryImport(Library)]
  internal static partial int FPDFFont_GetFlags(IntPtr font);

  [LibraryImport(Library)]
  internal static partial int FPDFFont_GetWeight(IntPtr font);

  [LibraryImport(Library)]
  internal static partial nuint FPDFFont_GetFamilyName(IntPtr font, byte* buffer, nuint length);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFImageObj_GetBitmap(IntPtr imageObject);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFImageObj_GetRenderedBitmap(IntPtr document, IntPtr page, IntPtr imageObject);
}
