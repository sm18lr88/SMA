using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperMemoAssistant.Plugins.Writing.Import
{
  /// <summary>Keeps every parent within the SuperMemo limit of children per element by nesting children in parts.</summary>
  public static class ChildLimitPlanner
  {
    /// <summary>
    ///   Returns <paramref name="children" /> unchanged if they fit in <paramref name="capacity" />. Otherwise groups
    ///   them in parts titled "[n] parent title" (the SMA subfolder style) of at most <paramref name="limit" />
    ///   children, and nests the parts again until they fit.
    /// </summary>
    public static IReadOnlyList<ImportTopic> Fit(IReadOnlyList<ImportTopic> children,
                                                 int                        capacity,
                                                 int                        limit,
                                                 string                     parentTitle)
    {
      if (limit < 2)
        throw new ArgumentOutOfRangeException(nameof(limit), limit, "The SuperMemo child limit must be at least 2.");

      if (children.Count <= capacity)
        return children;

      if (capacity < 1)
        throw new InvalidOperationException(
          $"\"{parentTitle}\" cannot take another child: SuperMemo allows {limit} children per element.");

      var parts = children.Chunk(limit)
                          .Select((chunk, i) => ImportTopic.Part($"[{i + 1}] {parentTitle}", chunk))
                          .ToList();

      return Fit(parts, capacity, limit, parentTitle);
    }
  }
}
