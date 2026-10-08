namespace SuperMemoAssistant.Plugins.Books.Planning
{
  using System.Collections.Generic;
  using System.Globalization;
  using System.Text;
  using System.Threading;
  using Epub;

  /// <summary>One chapter as the user sees it in the preview.</summary>
  /// <param name="Number">The 1-based position in the book.</param>
  /// <param name="Title">The chapter title.</param>
  /// <param name="Html">The sanitized HTML, merged documents included.</param>
  /// <param name="TextLength">The number of text characters.</param>
  /// <param name="DocumentCount">How many spine documents the chapter contains.</param>
  public sealed record BookChapter(int Number, string Title, string Html, int TextLength, int DocumentCount);

  /// <summary>
  ///   Turns spine documents into chapters. Empty documents are skipped. A document with less text than the merge
  ///   threshold is merged into the previous chapter; documents before the first chapter are merged into it.
  /// </summary>
  public static class ChapterBuilder
  {
    private sealed class Draft(string? title)
    {
      public string?       Title         { get; set; } = title;
      public StringBuilder Html          { get; }      = new();
      public int           TextLength    { get; set; }
      public int           DocumentCount { get; set; }

      public void Append(SanitizedChapter chapter)
      {
        Html.Append("<div>").Append(chapter.Html).Append("</div>\n");
        TextLength += chapter.TextLength;
        DocumentCount++;
      }
    }

    /// <summary>Builds the chapters of a book in spine order.</summary>
    /// <param name="book">The book.</param>
    /// <param name="mergeThreshold">Documents with fewer text characters are merged. 0 disables merging.</param>
    /// <param name="ct">Cancels the work.</param>
    public static IReadOnlyList<BookChapter> Build(EpubBook book, int mergeThreshold, CancellationToken ct)
    {
      var    drafts  = new List<Draft>();
      Draft? leading = null;

      foreach (var doc in book.Documents)
      {
        ct.ThrowIfCancellationRequested();

        var chapter = ChapterSanitizer.Sanitize(doc.Html, doc.Path, book.FindResource);
        if (chapter.TextLength == 0 && chapter.ImageCount == 0)
          continue;

        var title = doc.TocTitle ?? chapter.Heading ?? chapter.DocumentTitle;

        if (chapter.TextLength < mergeThreshold)
        {
          if (drafts.Count > 0)
            drafts[^1].Append(chapter);
          else
            (leading ??= new Draft(title)).Append(chapter);

          continue;
        }

        var draft = new Draft(title);
        if (leading != null)
        {
          draft.Html.Append(leading.Html);
          draft.TextLength    = leading.TextLength;
          draft.DocumentCount = leading.DocumentCount;
          leading             = null;
        }

        draft.Append(chapter);
        drafts.Add(draft);
      }

      if (leading != null)
        drafts.Add(leading);

      var chapters = new List<BookChapter>(drafts.Count);
      for (var i = 0; i < drafts.Count; i++)
      {
        var d     = drafts[i];
        var title = d.Title ?? string.Create(CultureInfo.InvariantCulture, $"Chapter {i + 1}");
        chapters.Add(new BookChapter(i + 1, title, d.Html.ToString(), d.TextLength, d.DocumentCount));
      }

      return chapters;
    }
  }
}
