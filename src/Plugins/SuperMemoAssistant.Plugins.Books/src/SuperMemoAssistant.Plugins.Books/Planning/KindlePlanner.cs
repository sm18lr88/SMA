namespace SuperMemoAssistant.Plugins.Books.Planning
{
  using System.Globalization;
  using System.Linq;
  using System.Net;
  using System.Text;
  using Kindle;

  /// <summary>Plans the SuperMemo tree of one Kindle book: a book topic with one child topic per highlight or note.</summary>
  public static class KindlePlanner
  {
    private const int TitleLength = 80;

    /// <summary>Plans the tree.</summary>
    /// <param name="book">The grouped book.</param>
    /// <param name="fileName">The clippings file name.</param>
    /// <param name="options">Priority and children limit.</param>
    public static ImportNode Plan(KindleBook book, string fileName, PlanOptions options)
    {
      var priority = PriorityCalculator.Clamp(options.Priority);
      var bookRefs = new ReferenceInfo(book.Title, book.Author, $"Kindle: {fileName}", null);
      var root     = new ImportNode(book.Title, BookHtml(book), priority, bookRefs);

      var children = book.Entries.Select((entry, i) => PlanEntry(book, entry, PriorityCalculator.ForChild(priority, options.PriorityStep, i)))
                         .ToList();

      TreeNester.AddChildren(root, children, options.ChildrenLimit, "highlights");

      return root;
    }

    private static ImportNode PlanEntry(KindleBook book, KindleEntry entry, double priority)
    {
      var clipping = entry.Clipping;
      var isNote   = clipping.Kind == KindleClippingKind.Note;
      var html     = new StringBuilder();

      if (isNote)
        AppendNote(html, clipping);
      else
        html.Append("<blockquote><p>").Append(Encode(clipping.Text)).Append("</p></blockquote>");

      foreach (var note in entry.Notes)
        AppendNote(html, note);

      var kind   = isNote ? "Kindle note" : "Kindle highlight";
      var source = clipping.Position.Length == 0 ? kind : $"{kind}, {clipping.Position}";
      var refs   = new ReferenceInfo(book.Title, book.Author, source, clipping.AddedText);
      var title  = Shorten((isNote ? "Note: " : string.Empty) + clipping.Text);
      var node   = new ImportNode(title, html.ToString(), priority, refs);

      node.HighlightHashes.AddRange(entry.Hashes);
      return node;
    }

    private static void AppendNote(StringBuilder html, KindleClipping note) =>
      html.Append("<p><b>Note:</b> ").Append(Encode(note.Text)).Append("</p>");

    private static string BookHtml(KindleBook book)
    {
      var highlights = book.Entries.Count(e => e.Clipping.Kind == KindleClippingKind.Highlight);
      var notes      = book.Entries.Sum(e => e.Notes.Count + (e.Clipping.Kind == KindleClippingKind.Note ? 1 : 0));
      var html       = new StringBuilder();

      html.Append("<h1>").Append(Encode(book.Title)).Append("</h1>");
      if (book.Author != null)
        html.Append("<p>").Append(Encode(book.Author)).Append("</p>");

      html.Append(string.Create(CultureInfo.InvariantCulture, $"<p>Kindle highlights: {highlights}. Notes: {notes}.</p>"));
      return html.ToString();
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text).Replace("\n", "<br>", System.StringComparison.Ordinal);

    private static string Shorten(string text)
    {
      var flat = string.Join(' ', text.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries));
      return flat.Length <= TitleLength ? flat : flat[..(TitleLength - 3)].TrimEnd() + "...";
    }
  }
}
