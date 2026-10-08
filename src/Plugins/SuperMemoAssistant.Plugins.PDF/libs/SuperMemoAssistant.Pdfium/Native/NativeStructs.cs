namespace SuperMemoAssistant.Pdfium.Native;

using System;
using System.Runtime.InteropServices;

/// <summary>FPDF_LIBRARY_CONFIG, version 2 (this PDFium build has no JavaScript engine).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LibraryConfig
{
  public int Version;
  public IntPtr UserFontPaths;
  public IntPtr Isolate;
  public uint V8EmbedderSlot;
}

/// <summary>FPDF_FILEACCESS: PDFium reads the document through <see cref="GetBlock" />.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FileAccessCallbacks
{
  public uint FileLength;
  public delegate* unmanaged<IntPtr, uint, byte*, uint, int> GetBlock;
  public IntPtr Param;
}

/// <summary>IFSDK_PAUSE: PDFium asks <see cref="NeedToPauseNow" /> whether a progressive render should yield.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Pause
{
  public int Version;
  public delegate* unmanaged<Pause*, int> NeedToPauseNow;
  public IntPtr User;
}

/// <summary>FPDF_FORMFILLINFO, version 1. PDFium skips every callback that is null.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FormFillInfo
{
  public int Version;
  public IntPtr Release;
  public delegate* unmanaged<FormFillInfo*, IntPtr, double, double, double, double, void> Invalidate;
  public delegate* unmanaged<FormFillInfo*, IntPtr, double, double, double, double, void> OutputSelectedRect;
  public delegate* unmanaged<FormFillInfo*, int, void> SetCursor;
  public delegate* unmanaged<FormFillInfo*, int, IntPtr, int> SetTimer;
  public delegate* unmanaged<FormFillInfo*, int, void> KillTimer;
  public IntPtr GetLocalTime;
  public IntPtr OnChange;
  public delegate* unmanaged<FormFillInfo*, IntPtr, int, IntPtr> GetPage;
  public delegate* unmanaged<FormFillInfo*, IntPtr, IntPtr> GetCurrentPage;
  public delegate* unmanaged<FormFillInfo*, IntPtr, int> GetRotation;
  public delegate* unmanaged<FormFillInfo*, byte*, void> ExecuteNamedAction;
  public IntPtr SetTextFieldFocus;
  public IntPtr DoUriAction;
  public delegate* unmanaged<FormFillInfo*, int, int, float*, int, void> DoGoToAction;
  public IntPtr JsPlatform;
}
