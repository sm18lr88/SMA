#nullable enable

namespace SuperMemoAssistant.Plugins.PDF.Extracts
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>
  ///   A document outline with the page range of each section. A section starts at its own page. It ends on the page
  ///   before the next entry that follows it and is not one of its descendants (an entry at the same or a higher level, or
  ///   a deeper entry of a later section). When that entry starts on the same page, the section is that one page. The
  ///   last section ends at the end of the document. Entries without a valid page, or with a page before the section
  ///   start (an out-of-order outline), do not end a section.
  /// </summary>
  public sealed class OutlineTree
  {
    #region Properties & Fields - Non-Public

    private readonly List<OutlineNode>            _preOrder = new List<OutlineNode>();
    private readonly List<int>                    _levels   = new List<int>();
    private readonly List<PageRange?>             _ranges   = new List<PageRange?>();
    private readonly Dictionary<OutlineNode, int> _indices  = new Dictionary<OutlineNode, int>(ReferenceEqualityComparer.Instance);

    #endregion




    #region Constructors

    public OutlineTree(IEnumerable<OutlineNode> roots, int pageCount)
    {
      ArgumentOutOfRangeException.ThrowIfNegative(pageCount);

      Roots     = roots.ToList();
      PageCount = pageCount;

      foreach (var root in Roots)
        Add(root, 0);

      for (int i = 0; i < _preOrder.Count; i++)
        _ranges.Add(ComputeRange(i));
    }

    #endregion




    #region Properties & Fields - Public

    public IReadOnlyList<OutlineNode> Roots     { get; }
    public int                        PageCount { get; }

    /// <summary>All entries in document (pre-order) order.</summary>
    public IReadOnlyList<OutlineNode> Nodes => _preOrder;

    #endregion




    #region Methods

    /// <summary>The page range of the section that starts at <paramref name="node" />, or null when it has no valid page.</summary>
    public PageRange? GetRange(OutlineNode node)
    {
      return _indices.TryGetValue(node, out int index)
        ? _ranges[index]
        : null;
    }

    /// <summary>The entry and its siblings (the entries with the same parent), in document order.</summary>
    public IReadOnlyList<OutlineNode> GetLevel(OutlineNode node) => node.Parent?.Children ?? Roots;

    /// <summary>
    ///   The last entry, in document order, whose section contains <paramref name="pageIndex" />. In a well-formed outline
    ///   this is the deepest such entry.
    /// </summary>
    public OutlineNode? FindDeepestContaining(int pageIndex)
    {
      for (int i = _preOrder.Count - 1; i >= 0; i--)
        if (_ranges[i]?.Contains(pageIndex) == true)
          return _preOrder[i];

      return null;
    }

    private PageRange? ComputeRange(int index)
    {
      if (_preOrder[index].PageIndex is not int start || IsValidPage(start) == false)
        return null;

      int level = _levels[index];
      int next  = index + 1;

      while (next < _preOrder.Count && _levels[next] > level)
        next++;

      for (; next < _preOrder.Count; next++)
        if (_preOrder[next].PageIndex is int nextPage && IsValidPage(nextPage) && nextPage >= start)
          return new PageRange(start, Math.Max(start, nextPage - 1));

      return new PageRange(start, PageCount - 1);
    }

    private bool IsValidPage(int pageIndex) => pageIndex >= 0 && pageIndex < PageCount;

    private void Add(OutlineNode node, int level)
    {
      if (_indices.TryAdd(node, _preOrder.Count) == false)
        return;

      _preOrder.Add(node);
      _levels.Add(level);

      foreach (var child in node.Children)
        Add(child, level + 1);
    }

    #endregion
  }
}
