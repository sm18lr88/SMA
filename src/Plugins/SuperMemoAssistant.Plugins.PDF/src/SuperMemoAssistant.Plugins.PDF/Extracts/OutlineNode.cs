#nullable enable

namespace SuperMemoAssistant.Plugins.PDF.Extracts
{
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>One entry of a document outline (bookmark), independent of the PDF engine.</summary>
  public sealed class OutlineNode
  {
    #region Constructors

    /// <param name="title">The bookmark title.</param>
    /// <param name="pageIndex">The zero-based page where the entry starts, or null when it has no destination in the document.</param>
    /// <param name="children">The child entries, in document order.</param>
    public OutlineNode(string title, int? pageIndex, IEnumerable<OutlineNode>? children = null)
    {
      Title     = title;
      PageIndex = pageIndex;
      Children  = (children ?? Enumerable.Empty<OutlineNode>()).ToList();

      foreach (var child in Children)
        child.Parent = this;
    }

    #endregion




    #region Properties & Fields - Public

    public string                     Title     { get; }
    public int?                       PageIndex { get; }
    public OutlineNode?               Parent    { get; private set; }
    public IReadOnlyList<OutlineNode> Children  { get; }

    #endregion
  }

  /// <summary>An inclusive range of zero-based page indices.</summary>
  public readonly record struct PageRange(int StartPage, int EndPage)
  {
    public bool Contains(int pageIndex) => pageIndex >= StartPage && pageIndex <= EndPage;
  }

  /// <summary>A character range of a document, with the same meaning as the PDF plugin's text extracts.</summary>
  public readonly record struct TextRange(int StartPage, int StartIndex, int EndPage, int EndIndex);

  /// <summary>A section that a batch PDF extract will create, or skip when it is already extracted.</summary>
  public sealed record PlannedSection(OutlineNode Node, TextRange Range, bool AlreadyExtracted);
}
