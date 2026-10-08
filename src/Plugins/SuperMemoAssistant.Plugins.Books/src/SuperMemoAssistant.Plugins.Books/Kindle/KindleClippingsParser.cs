namespace SuperMemoAssistant.Plugins.Books.Kindle
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Text.RegularExpressions;

  /// <summary>
  ///   Parses Kindle "My Clippings.txt". Entries are separated by "==========" lines. The first line is
  ///   "Title (Author)", the second line describes the kind, page, location, and date, and the text follows.
  /// </summary>
  public static partial class KindleClippingsParser
  {
    private const string Separator = "==========";

    private static readonly string[] DateFormats =
    [
      "MMMM d, yyyy h:mm:ss tt", "MMMM d, yyyy H:mm:ss", "d MMMM yyyy H:mm:ss", "d MMMM yyyy h:mm:ss tt",
      "MMMM d, yyyy", "d MMMM yyyy",
    ];

    /// <summary>Parses the full text of a clippings file.</summary>
    public static KindleParseResult Parse(string content)
    {
      var lines = content.Replace("\uFEFF", string.Empty, StringComparison.Ordinal)
                         .Replace("\r\n", "\n", StringComparison.Ordinal)
                         .Replace('\r', '\n')
                         .Split('\n');

      var clippings = new List<KindleClipping>();
      var bookmarks = 0;
      var invalid   = 0;
      var block     = new List<string>();

      void Flush()
      {
        switch (ParseEntry(block))
        {
          case (true, null):
            bookmarks++;
            break;

          case (false, null):
            invalid += block.Any(l => l.Trim().Length > 0) ? 1 : 0;
            break;

          case (_, { } clipping):
            clippings.Add(clipping);
            break;
        }

        block.Clear();
      }

      foreach (var line in lines)
        if (line.Trim() == Separator)
          Flush();
        else
          block.Add(line);

      Flush();

      return new KindleParseResult(clippings, bookmarks, invalid);
    }

    /// <summary>Parses one entry. Returns (isBookmark, clipping).</summary>
    private static (bool, KindleClipping?) ParseEntry(List<string> block)
    {
      var lines = block.SkipWhile(l => l.Trim().Length == 0).ToList();
      if (lines.Count < 2)
        return (false, null);

      var segments  = lines[1].Trim().TrimStart('-').Split('|').Select(s => s.Trim()).ToList();
      var kindMatch = KindRegex().Match(segments[0]);
      if (kindMatch.Success == false)
        return (false, null);

      var kindName = kindMatch.Groups[1].Value.ToLowerInvariant();
      if (kindName == "bookmark")
        return (true, null);

      var text = string.Join("\n", lines.Skip(2)).Trim();
      if (text.Length == 0)
        return (false, null);

      string? page = null, added = null;
      int?    start = null, end = null;

      foreach (var segment in segments)
      {
        if (PageRegex().Match(segment) is { Success: true } pm)
          page = pm.Groups[1].Value;

        if (LocationRegex().Match(segment) is { Success: true } lm)
          (start, end) = ParseLocation(lm.Groups[1].Value, lm.Groups[2].Value);

        if (AddedRegex().Match(segment) is { Success: true } am)
          added = am.Groups[1].Value.Trim();
      }

      var (title, author) = ParseTitleLine(lines[0]);
      var kind = kindName == "note" ? KindleClippingKind.Note : KindleClippingKind.Highlight;

      return (false, new KindleClipping(title, author, kind, page, start, end, added, ParseDate(added), text));
    }

    /// <summary>Splits "Title (Author)". The last parenthesized group at the end of the line is the author.</summary>
    public static (string Title, string? Author) ParseTitleLine(string line)
    {
      line = line.Trim();
      var m = TitleRegex().Match(line);

      if (m.Success == false || m.Groups["title"].Value.Trim().Length == 0)
        return (line, null);

      var author = m.Groups["author"].Value.Trim();
      return (m.Groups["title"].Value.Trim(), author.Length == 0 ? null : author);
    }

    /// <summary>Parses an English Kindle date such as "Sunday, 10 March 2019 10:42:13" or "Wednesday, March 6, 2019 7:58:10 PM".</summary>
    public static DateTime? ParseDate(string? text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return null;

      var comma = text.IndexOf(',');
      if (comma > 0 && CultureInfo.InvariantCulture.DateTimeFormat.DayNames.Contains(text[..comma].Trim(), StringComparer.OrdinalIgnoreCase))
        text = text[(comma + 1)..];

      return DateTime.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
        ? date
        : null;
    }

    private static (int?, int?) ParseLocation(string startText, string endText)
    {
      var start = int.Parse(startText, CultureInfo.InvariantCulture);
      if (endText.Length == 0)
        return (start, start);

      var end = int.Parse(endText, CultureInfo.InvariantCulture);

      // Old Kindles abbreviate the end, for example "1170-72" for 1170-1172.
      if (end < start && endText.Length < startText.Length)
        end = int.Parse(startText[..^endText.Length] + endText, CultureInfo.InvariantCulture);

      return (start, Math.Max(start, end));
    }

    [GeneratedRegex(@"^(?:Your\s+)?(Highlight|Note|Bookmark)\b", RegexOptions.IgnoreCase)]
    private static partial Regex KindRegex();

    [GeneratedRegex(@"\bpage\s+([^\s|]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PageRegex();

    [GeneratedRegex(@"\b(?:location|loc\.)\s*(\d{1,9})(?:\s*-\s*(\d{1,9}))?", RegexOptions.IgnoreCase)]
    private static partial Regex LocationRegex();

    [GeneratedRegex(@"^Added on\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex AddedRegex();

    [GeneratedRegex(@"^(?<title>.*?)\s*\((?<author>[^()]*)\)$")]
    private static partial Regex TitleRegex();
  }
}
