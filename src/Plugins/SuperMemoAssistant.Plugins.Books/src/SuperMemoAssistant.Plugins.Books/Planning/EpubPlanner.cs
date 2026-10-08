namespace SuperMemoAssistant.Plugins.Books.Planning
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Text;
  using Epub;

  /// <summary>Plans the SuperMemo tree of an EPUB book: one book topic with one child topic per selected chapter.</summary>
  public static class EpubPlanner
  {
    /// <summary>Plans the tree.</summary>
    /// <param name="metadata">The book metadata.</param>
    /// <param name="fileName">The EPUB file name, used as the source.</param>
    /// <param name="chapters">The selected chapters in reading order.</param>
    /// <param name="options">Priority and children limit.</param>
    public static ImportNode Plan(EpubMetadata metadata, string fileName, IReadOnlyList<BookChapter> chapters, PlanOptions options)
    {
      var author   = metadata.Creators.Count == 0 ? null : string.Join("; ", metadata.Creators);
      var source   = metadata.Isbn == null ? fileName : $"{fileName}, ISBN {metadata.Isbn}";
      var bookRefs = new ReferenceInfo(metadata.Title, author, source, metadata.Date);
      var priority = PriorityCalculator.Clamp(options.Priority);
      var book     = new ImportNode(metadata.Title, BookHtml(metadata, author, chapters), priority, bookRefs);

      var children = chapters.Select((chapter, i) => new ImportNode(
                                       chapter.Title,
                                       chapter.Html,
                                       PriorityCalculator.ForChild(priority, options.PriorityStep, i),
                                       bookRefs))
                             .ToList();

      TreeNester.AddChildren(book, children, options.ChildrenLimit, "chapters");

      return book;
    }

    private static string BookHtml(EpubMetadata metadata, string? author, IReadOnlyList<BookChapter> chapters)
    {
      var html = new StringBuilder();

      html.Append("<h1>").Append(WebUtility.HtmlEncode(metadata.Title)).Append("</h1>");

      if (author != null)
        html.Append("<p>").Append(WebUtility.HtmlEncode(author)).Append("</p>");

      var facts = new[] { metadata.Date, metadata.Language, metadata.Isbn == null ? null : "ISBN " + metadata.Isbn }
                  .Where(f => string.IsNullOrWhiteSpace(f) == false);
      html.Append("<p>").Append(WebUtility.HtmlEncode(string.Join(" | ", facts))).Append("</p>");

      html.Append("<h2>Contents</h2><ol>");
      foreach (var chapter in chapters)
        html.Append("<li>").Append(WebUtility.HtmlEncode(chapter.Title)).Append("</li>");
      html.Append("</ol>");

      return html.ToString();
    }
  }
}
