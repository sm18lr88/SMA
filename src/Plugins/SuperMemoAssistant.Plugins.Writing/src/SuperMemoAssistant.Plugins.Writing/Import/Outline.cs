using System.Collections.Generic;

namespace SuperMemoAssistant.Plugins.Writing.Import
{
  /// <summary>One heading of a Markdown outline, with the HTML of its content and its sub-headings.</summary>
  public sealed class OutlineNode(string title, int level)
  {
    public string Title { get; } = title;

    /// <summary>The Markdown heading level (1 to 6).</summary>
    public int Level { get; } = level;

    /// <summary>The HTML of the content between this heading and the next heading.</summary>
    public string Html { get; set; } = string.Empty;

    public List<OutlineNode> Children { get; } = [];
  }

  /// <summary>A parsed Markdown outline.</summary>
  /// <param name="Title">The "title" of the YAML front matter, if any.</param>
  /// <param name="PreambleHtml">The HTML of the content before the first heading.</param>
  /// <param name="Roots">The top-level headings in document order.</param>
  public sealed record Outline(string? Title, string PreambleHtml, IReadOnlyList<OutlineNode> Roots);
}
