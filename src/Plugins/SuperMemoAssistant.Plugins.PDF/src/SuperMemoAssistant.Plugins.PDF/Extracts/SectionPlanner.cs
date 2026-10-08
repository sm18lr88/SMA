#nullable enable

namespace SuperMemoAssistant.Plugins.PDF.Extracts
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>Plans PDF extracts of several outline sections at once.</summary>
  public static class SectionPlanner
  {
    #region Methods

    /// <summary>The direct children of <paramref name="node" /> that have a page range, in document order.</summary>
    public static IReadOnlyList<OutlineNode> EachSubsection(OutlineTree tree, OutlineNode node) =>
      WithRange(tree, node.Children);

    /// <summary>The node and its siblings that have a page range, in document order.</summary>
    public static IReadOnlyList<OutlineNode> ThisLevel(OutlineTree tree, OutlineNode node) =>
      WithRange(tree, tree.GetLevel(node));

    /// <summary>
    ///   Maps each section to the text range of whole pages that a PDF extract covers, and marks the sections whose range
    ///   is already in <paramref name="existingExtracts" />.
    /// </summary>
    /// <param name="pageCharCount">Returns the number of characters of a page. A section ends after the last character of its last page.</param>
    public static IReadOnlyList<PlannedSection> Plan(OutlineTree                tree,
                                                     IEnumerable<OutlineNode>   sections,
                                                     Func<int, int>             pageCharCount,
                                                     IEnumerable<TextRange>     existingExtracts)
    {
      var existing = existingExtracts.ToHashSet();
      var planned  = new List<PlannedSection>();

      foreach (var node in sections)
      {
        if (tree.GetRange(node) is not PageRange pages)
          continue;

        var range = new TextRange(pages.StartPage, 0, pages.EndPage, pageCharCount(pages.EndPage));

        planned.Add(new PlannedSection(node, range, existing.Contains(range)));
      }

      return planned;
    }

    private static IReadOnlyList<OutlineNode> WithRange(OutlineTree tree, IEnumerable<OutlineNode> nodes) =>
      nodes.Where(n => tree.GetRange(n) != null).ToList();

    #endregion
  }
}
