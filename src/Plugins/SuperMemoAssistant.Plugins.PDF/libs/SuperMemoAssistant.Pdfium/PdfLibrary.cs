namespace SuperMemoAssistant.Pdfium;

using System.Threading;
using Native;

/// <summary>Process-wide PDFium state. <see cref="PdfDocument" /> initializes the library on first use.</summary>
public static unsafe class PdfLibrary
{
  private static readonly Lock InitLock = new();

  public static bool IsInitialized { get; private set; }

  /// <summary>Initializes PDFium once per process. PDFium stays loaded until the process exits.</summary>
  public static void Initialize()
  {
    lock (InitLock)
    {
      if (IsInitialized)
        return;

      var config = new LibraryConfig { Version = 2 };
      NativeMethods.FPDF_InitLibraryWithConfig(&config);
      IsInitialized = true;
    }
  }
}
