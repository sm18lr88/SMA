namespace SuperMemoAssistant.Pdfium;

using Native;

/// <summary>Input for the document's form engine. Each method returns true when a form field handled the input.</summary>
public sealed partial class PdfPage
{
  public bool OnMouseMove(KeyboardModifiers modifiers, float x, float y) =>
    Document.FormFill is { } forms && NativeMethods.FORM_OnMouseMove(forms.Handle, Handle, modifiers, x, y) != 0;

  public bool OnLeftButtonDown(KeyboardModifiers modifiers, float x, float y) =>
    Document.FormFill is { } forms && NativeMethods.FORM_OnLButtonDown(forms.Handle, Handle, modifiers, x, y) != 0;

  public bool OnLeftButtonUp(KeyboardModifiers modifiers, float x, float y) =>
    Document.FormFill is { } forms && NativeMethods.FORM_OnLButtonUp(forms.Handle, Handle, modifiers, x, y) != 0;

  /// <summary>Sends a key to the focused field. <paramref name="virtualKey" /> is a Windows virtual-key code.</summary>
  public bool OnKeyDown(int virtualKey, KeyboardModifiers modifiers) =>
    Document.FormFill is { } forms && NativeMethods.FORM_OnKeyDown(forms.Handle, Handle, virtualKey, modifiers) != 0;

  /// <summary>Sends a key to the focused field. <paramref name="virtualKey" /> is a Windows virtual-key code.</summary>
  public bool OnKeyUp(int virtualKey, KeyboardModifiers modifiers) =>
    Document.FormFill is { } forms && NativeMethods.FORM_OnKeyUp(forms.Handle, Handle, virtualKey, modifiers) != 0;

  /// <summary>Sends a typed UTF-16 code unit to the focused field.</summary>
  public bool OnCharacter(char character, KeyboardModifiers modifiers) =>
    Document.FormFill is { } forms && NativeMethods.FORM_OnChar(forms.Handle, Handle, character, modifiers) != 0;

  public FormFieldType GetFormFieldAtPoint(float x, float y) =>
    Document.FormFill is { } forms
      ? (FormFieldType)NativeMethods.FPDFPage_HasFormFieldAtPoint(forms.Handle, Handle, x, y)
      : FormFieldType.None;
}
