namespace SuperMemoAssistant.Pdfium.Native;

using System;
using System.Runtime.InteropServices;

internal static unsafe partial class NativeMethods
{
  [LibraryImport(Library)]
  internal static partial IntPtr FPDFDOC_InitFormFillEnvironment(IntPtr document, FormFillInfo* formInfo);

  [LibraryImport(Library)]
  internal static partial void FPDFDOC_ExitFormFillEnvironment(IntPtr form);

  [LibraryImport(Library)]
  internal static partial void FORM_DoDocumentOpenAction(IntPtr form);

  [LibraryImport(Library)]
  internal static partial void FORM_OnAfterLoadPage(IntPtr page, IntPtr form);

  [LibraryImport(Library)]
  internal static partial void FORM_OnBeforeClosePage(IntPtr page, IntPtr form);

  [LibraryImport(Library)]
  internal static partial void FPDF_FFLDraw(IntPtr form, IntPtr bitmap, IntPtr page, int startX, int startY, int sizeX,
                                            int sizeY, PageRotate rotate, RenderFlags flags);

  [LibraryImport(Library)]
  internal static partial void FPDF_SetFormFieldHighlightColor(IntPtr form, FormFieldType fieldType, uint color);

  [LibraryImport(Library)]
  internal static partial void FPDF_SetFormFieldHighlightAlpha(IntPtr form, byte alpha);

  [LibraryImport(Library)]
  internal static partial int FPDFPage_HasFormFieldAtPoint(IntPtr form, IntPtr page, double x, double y);

  [LibraryImport(Library)]
  internal static partial int FORM_OnMouseMove(IntPtr form, IntPtr page, KeyboardModifiers modifiers, double x, double y);

  [LibraryImport(Library)]
  internal static partial int FORM_OnLButtonDown(IntPtr form, IntPtr page, KeyboardModifiers modifiers, double x, double y);

  [LibraryImport(Library)]
  internal static partial int FORM_OnLButtonUp(IntPtr form, IntPtr page, KeyboardModifiers modifiers, double x, double y);

  [LibraryImport(Library)]
  internal static partial int FORM_OnKeyDown(IntPtr form, IntPtr page, int virtualKey, KeyboardModifiers modifiers);

  [LibraryImport(Library)]
  internal static partial int FORM_OnKeyUp(IntPtr form, IntPtr page, int virtualKey, KeyboardModifiers modifiers);

  [LibraryImport(Library)]
  internal static partial int FORM_OnChar(IntPtr form, IntPtr page, int character, KeyboardModifiers modifiers);

  [LibraryImport(Library)]
  internal static partial int FORM_ForceToKillFocus(IntPtr form);
}
