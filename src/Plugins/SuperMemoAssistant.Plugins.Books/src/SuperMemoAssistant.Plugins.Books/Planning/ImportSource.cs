namespace SuperMemoAssistant.Plugins.Books.Planning
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using Epub;
  using Kindle;

  /// <summary>One row of the import preview: a chapter of a book, or a book of a clippings file.</summary>
  public sealed class PreviewItem : INotifyPropertyChanged
  {
    /// <summary>Creates a row.</summary>
    public PreviewItem(int index, string title, string details, bool isSelected)
    {
      Index      = index;
      Title      = title;
      Details    = details;
      IsSelected = isSelected;
    }

    /// <summary>The position of the chapter or book in its source.</summary>
    public int Index { get; }

    /// <summary>The chapter or book title.</summary>
    public string Title { get; }

    /// <summary>The size or count, for example "12,345 characters".</summary>
    public string Details { get; }

    /// <summary>Whether the user wants to import this row.</summary>
    public bool IsSelected { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
  }

  /// <summary>A loaded file that the dialog previews and then plans.</summary>
  public abstract class ImportSource
  {
    /// <summary>The preview rows.</summary>
    public abstract IReadOnlyList<PreviewItem> Items { get; }

    /// <summary>A one-line description of the file.</summary>
    public abstract string Summary { get; }

    /// <summary>Plans the trees for the selected rows.</summary>
    public abstract IReadOnlyList<ImportNode> Plan(PlanOptions options);
  }

  /// <summary>An EPUB book. Each preview row is a chapter.</summary>
  public sealed class EpubSource : ImportSource
  {
    private readonly EpubBook                   _book;
    private readonly string                     _fileName;
    private readonly IReadOnlyList<BookChapter> _chapters;

    /// <summary>Creates the source.</summary>
    public EpubSource(EpubBook book, string fileName, IReadOnlyList<BookChapter> chapters)
    {
      _book     = book;
      _fileName = fileName;
      _chapters = chapters;
      Items = chapters.Select((c, i) => new PreviewItem(i, c.Title, string.Format(CultureInfo.InvariantCulture, "{0:N0} characters", c.TextLength), true))
                      .ToList();
    }

    /// <inheritdoc />
    public override IReadOnlyList<PreviewItem> Items { get; }

    /// <inheritdoc />
    public override string Summary =>
      string.Format(CultureInfo.InvariantCulture, "{0} by {1} (EPUB {2}): {3} chapters.",
                    _book.Metadata.Title,
                    _book.Metadata.Creators.Count == 0 ? "an unknown author" : string.Join("; ", _book.Metadata.Creators),
                    _book.Version,
                    _chapters.Count);

    /// <inheritdoc />
    public override IReadOnlyList<ImportNode> Plan(PlanOptions options)
    {
      var selected = Items.Where(i => i.IsSelected).Select(i => _chapters[i.Index]).ToList();
      return selected.Count == 0 ? [] : [EpubPlanner.Plan(_book.Metadata, _fileName, selected, options)];
    }
  }

  /// <summary>A Kindle clippings file. Each preview row is a book.</summary>
  public sealed class KindleSource : ImportSource
  {
    private readonly KindleParseResult         _parsed;
    private readonly IReadOnlyList<KindleBook> _books;
    private readonly string                    _fileName;

    /// <summary>Creates the source.</summary>
    public KindleSource(KindleParseResult parsed, IReadOnlyList<KindleBook> books, string fileName)
    {
      _parsed   = parsed;
      _books    = books;
      _fileName = fileName;
      Items = books.Select((b, i) => new PreviewItem(
                             i,
                             b.Author == null ? b.Title : $"{b.Title} ({b.Author})",
                             string.Format(CultureInfo.InvariantCulture, "{0} new, {1} already imported or repeated", b.Entries.Count, b.DuplicateCount),
                             b.Entries.Count > 0))
                   .ToList();
    }

    /// <inheritdoc />
    public override IReadOnlyList<PreviewItem> Items { get; }

    /// <inheritdoc />
    public override string Summary =>
      string.Format(CultureInfo.InvariantCulture, "{0} highlights and notes in {1} books. Skipped: {2} bookmarks, {3} unreadable entries.",
                    _parsed.Clippings.Count, _books.Count, _parsed.SkippedBookmarks, _parsed.SkippedInvalid);

    /// <inheritdoc />
    public override IReadOnlyList<ImportNode> Plan(PlanOptions options) =>
      Items.Where(i => i.IsSelected && _books[i.Index].Entries.Count > 0)
           .Select(i => KindlePlanner.Plan(_books[i.Index], _fileName, options))
           .ToList();
  }

  /// <summary>Loads an EPUB or a Kindle clippings file for the preview.</summary>
  public static class ImportSourceLoader
  {
    /// <summary>Loads the file. EPUB files are recognized by the .epub extension; other files are read as clippings.</summary>
    /// <exception cref="EpubFormatException">The EPUB cannot be read.</exception>
    /// <exception cref="InvalidDataException">The text file has no Kindle clippings.</exception>
    public static ImportSource Load(string path, int mergeThreshold, IReadOnlySet<string> importedHashes, CancellationToken ct)
    {
      var fileName = Path.GetFileName(path);

      if (path.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
      {
        using var stream = File.OpenRead(path);
        var book = EpubReader.Read(stream, Path.GetFileNameWithoutExtension(path), ct);
        return new EpubSource(book, fileName, ChapterBuilder.Build(book, mergeThreshold, ct));
      }

      var parsed = KindleClippingsParser.Parse(File.ReadAllText(path));
      if (parsed.Clippings.Count == 0)
        throw new InvalidDataException("The file has no Kindle highlights or notes. Choose an EPUB file or a Kindle \"My Clippings.txt\" file.");

      return new KindleSource(parsed, KindleGrouper.Group(parsed.Clippings, importedHashes), fileName);
    }
  }
}
