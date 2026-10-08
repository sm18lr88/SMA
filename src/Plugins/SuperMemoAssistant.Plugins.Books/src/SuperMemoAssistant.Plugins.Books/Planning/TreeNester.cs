namespace SuperMemoAssistant.Plugins.Books.Planning
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Net;
  using System.Text;

  /// <summary>
  ///   Keeps every element within the SuperMemo children limit. When a parent gets more children than the limit, the
  ///   children are grouped in part topics of at most that many children, and the parts are grouped again if needed.
  /// </summary>
  public static class TreeNester
  {
    /// <summary>Adds <paramref name="children" /> to <paramref name="parent" />, nested in parts where the limit requires it.</summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="children">The children in order.</param>
    /// <param name="limit">The children limit, at least 2.</param>
    /// <param name="itemName">The plural name of the children for part titles, for example "chapters".</param>
    public static void AddChildren(ImportNode parent, IReadOnlyList<ImportNode> children, int limit, string itemName)
    {
      if (limit < 2)
        throw new ArgumentOutOfRangeException(nameof(limit), limit, "The children limit must be at least 2.");

      var level = children.Select((node, i) => (Node: node, First: i + 1, Last: i + 1)).ToList();

      while (level.Count > limit)
        level = level.Chunk(limit)
                     .Select(chunk => (Node: CreatePart(parent, chunk.Select(c => c.Node).ToList(), chunk[0].First, chunk[^1].Last, itemName),
                                       chunk[0].First,
                                       chunk[^1].Last))
                     .ToList();

      parent.Children.AddRange(level.Select(l => l.Node));
    }

    private static ImportNode CreatePart(ImportNode parent, List<ImportNode> members, int first, int last, string itemName)
    {
      var label = string.Create(CultureInfo.InvariantCulture, $"{parent.Title}: {itemName} {first}-{last}");
      var html  = new StringBuilder();

      html.Append("<h2>").Append(WebUtility.HtmlEncode(label)).Append("</h2><ul>");
      foreach (var member in members)
        html.Append("<li>").Append(WebUtility.HtmlEncode(member.Title)).Append("</li>");
      html.Append("</ul>");

      var part = new ImportNode(label, html.ToString(), members[0].Priority, parent.References);
      part.Children.AddRange(members);

      return part;
    }
  }
}
