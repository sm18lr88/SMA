namespace SuperMemoAssistant.Pdfium.Native;

using System;
using System.Runtime.InteropServices;

/// <summary>Bindings to the PDFium C API. Names and parameters mirror the pdfium.dll headers, so the PDFium documentation applies.</summary>
internal static unsafe partial class NativeMethods
{
  private const string Library = "pdfium";

  [LibraryImport(Library)]
  internal static partial void FPDF_InitLibraryWithConfig(LibraryConfig* config);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDF_LoadCustomDocument(FileAccessCallbacks* fileAccess,
                                                         [MarshalAs(UnmanagedType.LPUTF8Str)] string? password);

  [LibraryImport(Library)]
  internal static partial void FPDF_CloseDocument(IntPtr document);

  [LibraryImport(Library)]
  internal static partial uint FPDF_GetLastError();

  [LibraryImport(Library)]
  internal static partial int FPDF_GetPageCount(IntPtr document);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDF_LoadPage(IntPtr document, int pageIndex);

  [LibraryImport(Library)]
  internal static partial void FPDF_ClosePage(IntPtr page);

  [LibraryImport(Library)]
  internal static partial float FPDF_GetPageWidthF(IntPtr page);

  [LibraryImport(Library)]
  internal static partial float FPDF_GetPageHeightF(IntPtr page);

  [LibraryImport(Library)]
  internal static partial int FPDF_GetPageSizeByIndex(IntPtr document, int pageIndex, out double width, out double height);

  [LibraryImport(Library)]
  internal static partial int FPDF_RenderPage(IntPtr hdc, IntPtr page, int startX, int startY, int sizeX, int sizeY,
                                            PageRotate rotate, RenderFlags flags);

  [LibraryImport(Library)]
  internal static partial void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page, int startX, int startY, int sizeX,
                                                     int sizeY, PageRotate rotate, RenderFlags flags);

  [LibraryImport(Library)]
  internal static partial int FPDF_DeviceToPage(IntPtr page, int startX, int startY, int sizeX, int sizeY,
                                                PageRotate rotate, int deviceX, int deviceY,
                                                out double pageX, out double pageY);

  [LibraryImport(Library)]
  internal static partial int FPDF_PageToDevice(IntPtr page, int startX, int startY, int sizeX, int sizeY,
                                                PageRotate rotate, double pageX, double pageY,
                                                out int deviceX, out int deviceY);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFBitmap_Create(int width, int height, int alpha);

  [LibraryImport(Library)]
  internal static partial int FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);

  [LibraryImport(Library)]
  internal static partial IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

  [LibraryImport(Library)]
  internal static partial int FPDFBitmap_GetWidth(IntPtr bitmap);

  [LibraryImport(Library)]
  internal static partial int FPDFBitmap_GetHeight(IntPtr bitmap);

  [LibraryImport(Library)]
  internal static partial int FPDFBitmap_GetStride(IntPtr bitmap);

  [LibraryImport(Library)]
  internal static partial int FPDFBitmap_GetFormat(IntPtr bitmap);

  [LibraryImport(Library)]
  internal static partial void FPDFBitmap_Destroy(IntPtr bitmap);

  [LibraryImport(Library)]
  internal static partial int FPDF_RenderPageBitmap_Start(IntPtr bitmap, IntPtr page, int startX, int startY, int sizeX,
                                                          int sizeY, PageRotate rotate, RenderFlags flags, Pause* pause);

  [LibraryImport(Library)]
  internal static partial int FPDF_RenderPage_Continue(IntPtr page, Pause* pause);

  [LibraryImport(Library)]
  internal static partial void FPDF_RenderPage_Close(IntPtr page);

  [LibraryImport(Library)]
  internal static partial uint FPDF_GetMetaText(IntPtr document, [MarshalAs(UnmanagedType.LPUTF8Str)] string tag,
                                                void* buffer, uint length);

  [LibraryImport(Library)]
  internal static partial int FPDFPage_GetRotation(IntPtr page);

  [LibraryImport(Library)]
  internal static partial void FPDFPage_SetRotation(IntPtr page, int rotate);

  [LibraryImport(Library)]
  internal static partial int FPDFPage_HasTransparency(IntPtr page);
}
