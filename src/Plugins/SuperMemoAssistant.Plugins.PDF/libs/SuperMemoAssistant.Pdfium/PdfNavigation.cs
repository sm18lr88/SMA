namespace SuperMemoAssistant.Pdfium;

using System;
using System.Text;
using Native;

/// <summary>A target view in a document. Coordinates are in page space; null means "keep the current value".</summary>
public sealed record PdfDestination(int PageIndex, DestinationType DestinationType, float? Left, float? Top, float? Right,
                                    float? Bottom, float? Zoom, string? Name = null)
{
  internal static unsafe PdfDestination? FromHandle(PdfDocument document, IntPtr handle, string? name = null)
  {
    if (handle == IntPtr.Zero)
      return null;

    var pageIndex = NativeMethods.FPDFDest_GetDestPageIndex(document.Handle, handle);
    var parameters = stackalloc float[4];
    var type = (DestinationType)NativeMethods.FPDFDest_GetView(handle, out var count, parameters);
    float? p0 = count > 0 ? parameters[0] : null, p1 = count > 1 ? parameters[1] : null;
    float? p2 = count > 2 ? parameters[2] : null, p3 = count > 3 ? parameters[3] : null;

    switch (type)
    {
      case DestinationType.Xyz:
        NativeMethods.FPDFDest_GetLocationInPage(handle, out var hasX, out var hasY, out var hasZoom,
                                                 out var x, out var y, out var zoom);
        return new PdfDestination(pageIndex, type, hasX != 0 ? x : null, hasY != 0 ? y : null, null, null,
                                  hasZoom != 0 && zoom > 0 ? zoom : null, name);

      case DestinationType.FitH or DestinationType.FitBH:
        return new PdfDestination(pageIndex, type, null, p0, null, null, null, name);

      case DestinationType.FitV or DestinationType.FitBV:
        return new PdfDestination(pageIndex, type, p0, null, null, null, null, name);

      case DestinationType.FitR:
        return new PdfDestination(pageIndex, type, p0, p3, p2, p1, null, name);

      default:
        return new PdfDestination(pageIndex, type, null, null, null, null, null, name);
    }
  }
}

/// <summary>The named destinations of a document.</summary>
public sealed class PdfDestinationCollection
{
  private readonly PdfDocument _document;

  internal PdfDestinationCollection(PdfDocument document) => _document = document;

  /// <summary>Returns the destination with this name, or null when the document does not define it.</summary>
  public PdfDestination? this[string name] =>
    PdfDestination.FromHandle(_document, NativeMethods.FPDF_GetNamedDestByName(_document.Handle, name), name);
}

/// <summary>An action of a link or bookmark.</summary>
public sealed class PdfAction
{
  private PdfAction(ActionType actionType, PdfDestination? destination, string? uri, string? filePath)
  {
    ActionType = actionType;
    Destination = destination;
    Uri = uri;
    FilePath = filePath;
  }

  public ActionType ActionType { get; }

  /// <summary>
  ///   The target of a go-to action. For <see cref="Pdfium.ActionType.RemoteGoTo" /> and
  ///   <see cref="Pdfium.ActionType.EmbeddedGoTo" />, the page index refers to the other document.
  /// </summary>
  public PdfDestination? Destination { get; }

  /// <summary>The target of a <see cref="Pdfium.ActionType.Uri" /> action.</summary>
  public string? Uri { get; }

  /// <summary>The file of a <see cref="Pdfium.ActionType.RemoteGoTo" /> or <see cref="Pdfium.ActionType.Launch" /> action.</summary>
  public string? FilePath { get; }

  internal static unsafe PdfAction? FromHandle(PdfDocument document, IntPtr handle)
  {
    if (handle == IntPtr.Zero)
      return null;

    var type = (ActionType)NativeMethods.FPDFAction_GetType(handle);

    var destination = type is ActionType.GoTo or ActionType.RemoteGoTo or ActionType.EmbeddedGoTo
      ? PdfDestination.FromHandle(document, NativeMethods.FPDFAction_GetDest(document.Handle, handle))
      : null;

    var uri = type == ActionType.Uri
      ? ReadUtf8((buffer, length) => NativeMethods.FPDFAction_GetURIPath(document.Handle, handle, (void*)buffer, length))
      : null;

    var filePath = type is ActionType.RemoteGoTo or ActionType.Launch
      ? ReadUtf8((buffer, length) => NativeMethods.FPDFAction_GetFilePath(handle, (void*)buffer, length))
      : null;

    return new PdfAction(type, destination, uri, filePath);
  }

  private static unsafe string? ReadUtf8(Func<IntPtr, uint, uint> read)
  {
    var length = read(IntPtr.Zero, 0);
    if (length <= 1)
      return null;

    var buffer = new byte[length];
    fixed (byte* bytes = buffer)
      read((IntPtr)bytes, length);

    return Encoding.UTF8.GetString(buffer, 0, (int)length - 1);
  }
}
