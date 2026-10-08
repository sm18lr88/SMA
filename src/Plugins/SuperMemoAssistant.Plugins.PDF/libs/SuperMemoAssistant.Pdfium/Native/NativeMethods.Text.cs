namespace SuperMemoAssistant.Pdfium.Native;

using System;
using System.Runtime.InteropServices;

internal static unsafe partial class NativeMethods
{
  [LibraryImport(Library)]
  internal static partial IntPtr FPDFText_LoadPage(IntPtr page);

  [LibraryImport(Library)]
  internal static partial void FPDFText_ClosePage(IntPtr textPage);

  [LibraryImport(Library)]
  internal static partial int FPDFText_CountChars(IntPtr textPage);

  [LibraryImport(Library)]
  internal static partial uint FPDFText_GetUnicode(IntPtr textPage, int index);

  [LibraryImport(Library)]
  internal static partial int FPDFText_GetCharIndexAtPos(IntPtr textPage, double x, double y, double xTolerance,
                                                         double yTolerance);

  [LibraryImport(Library)]
  internal static partial int FPDFText_GetText(IntPtr textPage, int startIndex, int count, char* result);

  [LibraryImport(Library)]
  internal static partial int FPDFText_CountRects(IntPtr textPage, int startIndex, int count);

  [LibraryImport(Library)]
  internal static partial int FPDFText_GetRect(IntPtr textPage, int rectIndex, out double left, out double top,
                                               out double right, out double bottom);

  [LibraryImport(Library)]
  internal static partial int FPDFText_GetCharBox(IntPtr textPage, int index, out double left, out double right,
                                                  out double bottom, out double top);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFText_GetTextObject(IntPtr textPage, int index);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFText_FindStart(IntPtr textPage, [MarshalAs(UnmanagedType.LPWStr)] string findWhat,
                                                    FindFlags flags, int startIndex);

  [LibraryImport(Library)]
  internal static partial int FPDFText_FindNext(IntPtr search);

  [LibraryImport(Library)]
  internal static partial int FPDFText_GetSchResultIndex(IntPtr search);

  [LibraryImport(Library)]
  internal static partial int FPDFText_GetSchCount(IntPtr search);

  [LibraryImport(Library)]
  internal static partial void FPDFText_FindClose(IntPtr search);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFLink_LoadWebLinks(IntPtr textPage);

  [LibraryImport(Library)]
  internal static partial int FPDFLink_CountWebLinks(IntPtr linkPage);

  [LibraryImport(Library)]
  internal static partial int FPDFLink_GetURL(IntPtr linkPage, int linkIndex, char* buffer, int length);

  [LibraryImport(Library)]
  internal static partial int FPDFLink_CountRects(IntPtr linkPage, int linkIndex);

  [LibraryImport(Library)]
  internal static partial int FPDFLink_GetRect(IntPtr linkPage, int linkIndex, int rectIndex, out double left,
                                               out double top, out double right, out double bottom);

  [LibraryImport(Library)]
  internal static partial void FPDFLink_CloseWebLinks(IntPtr linkPage);
}
