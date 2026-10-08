namespace SuperMemoAssistant.Plugins.Books.Kindle
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>A highlight with the notes that annotate it, or a note on its own.</summary>
  public sealed record KindleEntry(KindleClipping Clipping, IReadOnlyList<KindleClipping> Notes)
  {
    /// <summary>The hashes of the clipping and its notes.</summary>
    public IEnumerable<string> Hashes => Notes.Prepend(Clipping).Select(HighlightHasher.Hash);
  }

  /// <summary>The new entries of one book, and how many entries were skipped as duplicates.</summary>
  public sealed record KindleBook(string Title, string? Author, IReadOnlyList<KindleEntry> Entries, int DuplicateCount);

  /// <summary>
  ///   Groups clippings by book, removes duplicates (repeated in the file or imported before), and attaches each note to
  ///   the highlight whose location range overlaps it.
  /// </summary>
  public static class KindleGrouper
  {
    /// <summary>Groups the clippings. Books keep the order of their first clipping in the file.</summary>
    /// <param name="clippings">The parsed clippings in file order.</param>
    /// <param name="importedHashes">Hashes of clippings that earlier imports created.</param>
    public static IReadOnlyList<KindleBook> Group(IReadOnlyList<KindleClipping> clippings, IReadOnlySet<string> importedHashes)
    {
      return clippings.GroupBy(c => (c.Title, c.Author))
                      .Select(g => GroupBook(g.Key.Title, g.Key.Author, g.ToList(), importedHashes))
                      .ToList();
    }

    private static KindleBook GroupBook(string title, string? author, List<KindleClipping> clippings, IReadOnlySet<string> importedHashes)
    {
      var seen       = new HashSet<string>(StringComparer.Ordinal);
      var fresh      = new List<KindleClipping>();
      var duplicates = 0;

      foreach (var clipping in clippings)
      {
        var hash = HighlightHasher.Hash(clipping);

        if (importedHashes.Contains(hash) || seen.Add(hash) == false)
          duplicates++;
        else
          fresh.Add(clipping);
      }

      var highlights = fresh.Where(c => c.Kind == KindleClippingKind.Highlight).ToList();
      var notesOf    = highlights.Select(_ => new List<KindleClipping>()).ToList();
      var standalone = new List<KindleClipping>();

      foreach (var note in fresh.Where(c => c.Kind == KindleClippingKind.Note))
      {
        var overlapping = Enumerable.Range(0, highlights.Count).Where(i => highlights[i].Overlaps(note)).ToList();
        var exact       = overlapping.Where(i => highlights[i].LocationEnd == note.LocationStart).ToList();
        var target      = exact.Count > 0 ? exact[0] : overlapping.Count > 0 ? overlapping[^1] : -1;

        if (target < 0)
          standalone.Add(note);
        else
          notesOf[target].Add(note);
      }

      var entries = highlights.Select((h, i) => new KindleEntry(h, notesOf[i]))
                              .Concat(standalone.Select(n => new KindleEntry(n, [])))
                              .OrderBy(e => e.Clipping.LocationStart ?? int.MaxValue)
                              .ToList();

      return new KindleBook(title, author, entries, duplicates);
    }
  }
}
