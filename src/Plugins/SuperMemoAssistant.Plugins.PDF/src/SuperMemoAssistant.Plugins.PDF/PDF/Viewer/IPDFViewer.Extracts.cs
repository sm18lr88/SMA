namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Threading;
  using Anotar.Serilog;
  using Extensions;
  using Extracts;
  using Forge.Forms;
  using Models;
  using Services;
  using SuperMemoAssistant.Pdfium;
  using SuperMemoAssistant.Pdfium.Wpf;

  /// <summary>Extract titles and PDF extracts of several bookmark sections at once.</summary>
  public partial class IPDFViewer
  {
    #region Properties & Fields - Non-Public

    private PdfOutline _outline;
    private bool       _isBatchExtracting;

    /// <summary>The outline of the current document, built once per document.</summary>
    protected PdfOutline Outline => _outline?.Document == Document
      ? _outline
      : _outline = new PdfOutline(Document);

    private bool UseContentTitles => Config.ExtractTitleSource == ExtractTitleSource.Content;

    #endregion




    #region Methods

    /// <summary>The number of sections that <see cref="ExtractBookmarkSectionsAsync" /> would extract.</summary>
    public int CountBookmarkSections(PdfBookmark bookmark, BookmarkSectionScope scope)
    {
      return Document == null ? 0 : GetBookmarkSections(bookmark, scope).Count;
    }

    /// <summary>Creates one sub-PDF element per section, after a confirmation when there are several, and reports the result.</summary>
    public async Task ExtractBookmarkSectionsAsync(PdfBookmark bookmark, BookmarkSectionScope scope, CancellationToken ct)
    {
      if (_isBatchExtracting || Document == null || PDFElement == null)
        return;

      var sections = GetBookmarkSections(bookmark, scope);

      if (sections.Count == 0)
        return;

      if (sections.Count > 1)
      {
        var confirm = await Show.Window()
                                .For(new Confirmation($"Create {sections.Count} PDF extracts, one per section?", "PDF Extract"))
                                .ConfigureAwait(true);

        if (confirm.Model.Confirmed == false)
          return;
      }

      _isBatchExtracting = true;

      try
      {
        string report = await CreateSectionExtractsAsync(sections, ct).ConfigureAwait(true);

        Window.GetWindow(this)?.Activate();

        await Show.Window().For(new Alert(report, "PDF Extract")).ConfigureAwait(true);
      }
      finally
      {
        _isBatchExtracting = false;
      }
    }

    private async Task<string> CreateSectionExtractsAsync(IReadOnlyList<OutlineNode> sections, CancellationToken ct)
    {
      var document   = Document;
      var pdfElement = PDFElement;
      var existing   = pdfElement.PDFExtracts.Select(e => new TextRange(e.StartPage, e.StartIndex, e.EndPage, e.EndIndex));
      var plan       = SectionPlanner.Plan(Outline.Tree, sections, PageCharCount, existing);

      int created = 0;
      int skipped = plan.Count(s => s.AlreadyExtracted);
      string stopReason = null;

      Save(false);

      try
      {
        foreach (var section in plan.Where(s => s.AlreadyExtracted == false))
        {
          if (ct.IsCancellationRequested || Document != document || PDFElement != pdfElement)
          {
            stopReason = "The document was closed or changed.";
            break;
          }

          var range   = section.Range;
          var selInfo = new SelectInfo { StartPage = range.StartPage, StartIndex = range.StartIndex, EndPage = range.EndPage, EndIndex = range.EndIndex };

          if (CreatePDFExtractNoSave(selInfo, section.Node.Title) == false)
          {
            LogTo.Warning("Batch PDF extract: SuperMemo did not create the element for section {Title}", section.Node.Title);
            stopReason = $"SuperMemo did not create the element for \"{section.Node.Title}\".";
            break;
          }

          created++;

          await Dispatcher.Yield(DispatcherPriority.Background);
        }
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Batch PDF extract failed after {Created} extracts", created);
        stopReason = $"An error occurred: {ex.Message}";
      }
      finally
      {
        if (PDFElement == pdfElement)
          Save(false);
      }

      var report = $"Created {created} PDF extract{(created == 1 ? "" : "s")}. Skipped {skipped} section{(skipped == 1 ? "" : "s")} that were already extracted.";

      return stopReason == null
        ? report
        : $"Stopped. {stopReason}\n{report}";
    }

    private IReadOnlyList<OutlineNode> GetBookmarkSections(PdfBookmark bookmark, BookmarkSectionScope scope)
    {
      if (bookmark == null || Outline.GetNode(bookmark) is not OutlineNode node)
        return Array.Empty<OutlineNode>();

      return scope == BookmarkSectionScope.EachSubsection
        ? SectionPlanner.EachSubsection(Outline.Tree, node)
        : SectionPlanner.ThisLevel(Outline.Tree, node);
    }

    private int PageCharCount(int pageIdx) => GetTextLength(pageIdx);

    /// <summary>The title of a text extract, or null to keep the earlier behavior.</summary>
    protected string GetTextExtractTitle(string html)
    {
      if (UseContentTitles == false)
        return null;

      int maxLength   = Math.Clamp(Config.ExtractTitleMaxLength, PDFConst.MinExtractTitleLength, PDFConst.MaxExtractTitleLength);
      var parentTitle = Svc.SM.Registry.Element[PDFElement.ElementId].Title;

      return ExtractTitles.TextExtractTitle(html, maxLength, parentTitle);
    }

    /// <summary>The bookmark hierarchy of the first page of an image extract, or <paramref name="parentTitle" />.</summary>
    protected string GetImageExtractTitlePrefix(IEnumerable<int> pageIndices, string parentTitle)
    {
      var bookmark = UseContentTitles && pageIndices.Any()
        ? FindBookmark(pageIndices.Min())
        : null;

      return bookmark?.ToHierarchyString() ?? parentTitle;
    }

    /// <summary>The title of a sub-PDF extract without an explicit title, or null to keep the earlier behavior.</summary>
    protected string GetDefaultPDFExtractTitle(SelectInfo selInfo)
    {
      if (UseContentTitles == false)
        return null;

      return ExtractTitles.SubPdfTitle(FindBookmark(selInfo.StartPage)?.Title, TitleOrFileName, selInfo.StartPage, selInfo.EndPage);
    }

    #endregion
  }

  /// <summary>Which bookmark sections a batch PDF extract covers.</summary>
  public enum BookmarkSectionScope
  {
    /// <summary>Each direct child of the bookmark.</summary>
    EachSubsection,

    /// <summary>The bookmark and its siblings.</summary>
    ThisLevel,
  }
}
