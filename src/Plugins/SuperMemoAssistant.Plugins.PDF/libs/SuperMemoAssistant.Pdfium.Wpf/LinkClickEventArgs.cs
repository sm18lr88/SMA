namespace SuperMemoAssistant.Pdfium.Wpf
{
  using System;
  using System.ComponentModel;

  /// <summary>The user clicked a link. Set <see cref="CancelEventArgs.Cancel" /> to stop the viewer from following it.</summary>
  public sealed class PdfBeforeLinkClickedEventArgs(PdfWebLink webLink, PdfLink link) : CancelEventArgs
  {
    /// <summary>The URL in the page text under the pointer, or null.</summary>
    public PdfWebLink WebLink { get; } = webLink;

    /// <summary>The link annotation under the pointer, or null.</summary>
    public PdfLink Link { get; } = link;
  }

  /// <summary>The viewer followed a link.</summary>
  public sealed class PdfAfterLinkClickedEventArgs(PdfWebLink webLink, PdfLink link) : EventArgs
  {
    public PdfWebLink WebLink { get; } = webLink;

    public PdfLink Link { get; } = link;
  }
}
