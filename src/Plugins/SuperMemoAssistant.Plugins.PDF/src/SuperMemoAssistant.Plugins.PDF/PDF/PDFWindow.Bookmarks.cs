namespace SuperMemoAssistant.Plugins.PDF.PDF
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Windows;
  using System.Windows.Controls;
  using Anotar.Serilog;
  using SuperMemoAssistant.Pdfium;
  using Viewer;

  /// <summary>The bookmark menu items that extract several sections at once.</summary>
  partial class PDFWindow
  {
    #region Properties & Fields - Non-Public

    private CancellationTokenSource _sectionExtractCts;

    #endregion




    #region Methods

    private void TvBookmarks_ContextMenu_Opened(object          sender,
                                                RoutedEventArgs e)
    {
      var menu     = (ContextMenu)sender;
      var bookmark = (menu.PlacementTarget as FrameworkElement)?.DataContext as PdfBookmark;

      foreach (var item in menu.Items.OfType<MenuItem>())
      {
        if (item.Tag is not BookmarkSectionScope scope)
          continue;

        int count = bookmark == null ? 0 : IPDFViewer.CountBookmarkSections(bookmark, scope);

        // "This level" with a single entry is the same as the plain PDF Extract.
        int minimum = scope == BookmarkSectionScope.ThisLevel ? 2 : 1;

        item.Header = scope == BookmarkSectionScope.EachSubsection
          ? $"PDF Extract each subsection ({count})"
          : $"PDF Extract this level ({count})";
        item.Visibility = count >= minimum ? Visibility.Visible : Visibility.Collapsed;
      }
    }

    private async void TvBookmark_MenuItem_PDFExtractSections(object          sender,
                                                              RoutedEventArgs e)
    {
      if (sender is not MenuItem { Tag: BookmarkSectionScope scope } || tvBookmarks.SelectedItem is not PdfBookmark bookmark)
        return;

      using var cts = new CancellationTokenSource();
      _sectionExtractCts = cts;

      try
      {
        await IPDFViewer.ExtractBookmarkSectionsAsync(bookmark, scope, cts.Token).ConfigureAwait(true);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception caught while extracting several bookmark sections.");
      }
      finally
      {
        _sectionExtractCts = null;
      }
    }

    private void CancelSectionExtracts()
    {
      _sectionExtractCts?.Cancel();
    }

    #endregion
  }
}
