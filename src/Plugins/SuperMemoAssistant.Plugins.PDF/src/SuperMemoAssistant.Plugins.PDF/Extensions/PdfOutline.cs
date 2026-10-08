#nullable enable

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{
  using System.Collections.Generic;
  using Extracts;
  using SuperMemoAssistant.Pdfium;
  using SuperMemoAssistant.Pdfium.Wpf;

  /// <summary>Maps the bookmarks of a <see cref="PdfDocument" /> to an <see cref="OutlineTree" /> and back.</summary>
  public sealed class PdfOutline
  {
    #region Constants & Statics

    // A damaged outline can link a child back to one of its ancestors.
    private const int MaxDepth = 64;

    #endregion




    #region Properties & Fields - Non-Public

    private readonly Dictionary<PdfBookmark, OutlineNode> _nodes     = new Dictionary<PdfBookmark, OutlineNode>(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<OutlineNode, PdfBookmark> _bookmarks = new Dictionary<OutlineNode, PdfBookmark>(ReferenceEqualityComparer.Instance);

    #endregion




    #region Constructors

    public PdfOutline(PdfDocument document)
    {
      Document = document;
      Tree     = new OutlineTree(Convert(document.Bookmarks, 0), document.Pages.Count);
    }

    #endregion




    #region Properties & Fields - Public

    public PdfDocument Document { get; }
    public OutlineTree Tree     { get; }

    #endregion




    #region Methods

    public OutlineNode? GetNode(PdfBookmark bookmark) => _nodes.GetValueOrDefault(bookmark);

    public PdfBookmark GetBookmark(OutlineNode node) => _bookmarks[node];

    /// <summary>The whole pages of the section that starts at <paramref name="bookmark" />, or null when it has no page in this document.</summary>
    public SelectInfo? GetSelection(PdfBookmark bookmark)
    {
      if (GetNode(bookmark) is not OutlineNode node || Tree.GetRange(node) is not PageRange pages)
        return null;

      return new SelectInfo
      {
        StartPage  = pages.StartPage,
        StartIndex = 0,
        EndPage    = pages.EndPage,
        EndIndex   = Document.Pages[pages.EndPage].Text.CountChars,
      };
    }

    /// <summary>The deepest bookmark whose section contains <paramref name="pageIndex" />, or null.</summary>
    public PdfBookmark? FindBookmark(int pageIndex)
    {
      return Tree.FindDeepestContaining(pageIndex) is OutlineNode node
        ? _bookmarks[node]
        : null;
    }

    private List<OutlineNode> Convert(IEnumerable<PdfBookmark> bookmarks, int depth)
    {
      var nodes = new List<OutlineNode>();

      foreach (var bookmark in bookmarks)
      {
        var destination = bookmark.Action?.GetDestination() ?? bookmark.Destination;
        var children    = depth < MaxDepth ? Convert(bookmark.Children, depth + 1) : new List<OutlineNode>();
        var node        = new OutlineNode(bookmark.Title, destination?.PageIndex, children);

        _nodes[bookmark]  = node;
        _bookmarks[node]  = bookmark;
        nodes.Add(node);
      }

      return nodes;
    }

    #endregion
  }
}
