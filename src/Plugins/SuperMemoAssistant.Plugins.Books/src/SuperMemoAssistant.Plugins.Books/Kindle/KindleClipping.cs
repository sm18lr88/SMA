namespace SuperMemoAssistant.Plugins.Books.Kindle
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;

  /// <summary>The kind of a "My Clippings.txt" entry.</summary>
  public enum KindleClippingKind
  {
    /// <summary>A highlighted passage.</summary>
    Highlight,

    /// <summary>A note that the reader typed.</summary>
    Note,
  }

  /// <summary>One highlight or note of a Kindle "My Clippings.txt" file.</summary>
  public sealed record KindleClipping(
    string             Title,
    string?            Author,
    KindleClippingKind Kind,
    string?            Page,
    int?               LocationStart,
    int?               LocationEnd,
    string?            AddedText,
    DateTime?          Added,
    string             Text)
  {
    /// <summary>A short human-readable position, for example "page 12, location 170-172".</summary>
    public string Position
    {
      get
      {
        var parts = new List<string>();

        if (string.IsNullOrWhiteSpace(Page) == false)
          parts.Add("page " + Page);

        if (LocationStart.HasValue)
          parts.Add(LocationEnd.HasValue && LocationEnd != LocationStart
                      ? string.Create(CultureInfo.InvariantCulture, $"location {LocationStart}-{LocationEnd}")
                      : string.Create(CultureInfo.InvariantCulture, $"location {LocationStart}"));

        return string.Join(", ", parts);
      }
    }

    /// <summary>Whether the location ranges of two clippings overlap.</summary>
    public bool Overlaps(KindleClipping other) =>
      LocationStart.HasValue && other.LocationStart.HasValue
      && LocationStart <= (other.LocationEnd ?? other.LocationStart)
      && other.LocationStart <= (LocationEnd ?? LocationStart);
  }

  /// <summary>The result of parsing a clippings file.</summary>
  /// <param name="Clippings">Highlights and notes in file order.</param>
  /// <param name="SkippedBookmarks">Bookmarks, which are not imported.</param>
  /// <param name="SkippedInvalid">Entries that could not be read or have no text.</param>
  public sealed record KindleParseResult(IReadOnlyList<KindleClipping> Clippings, int SkippedBookmarks, int SkippedInvalid);
}
