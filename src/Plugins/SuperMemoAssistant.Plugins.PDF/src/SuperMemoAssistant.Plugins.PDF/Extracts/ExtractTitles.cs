#nullable enable

namespace SuperMemoAssistant.Plugins.PDF.Extracts
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Text.RegularExpressions;

  /// <summary>Builds the titles of new extracts. Has no SuperMemo or WPF dependency.</summary>
  public static class ExtractTitles
  {
    #region Constants & Statics

    public const string Ellipsis = "\u2026";

    private static readonly Regex RE_ScriptOrStyle = new Regex(@"<(script|style)\b[^>]*>.*?</\1\s*>",
                                                               RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex RE_BlockTag = new Regex(@"<\s*(br|hr|/?(p|div|li|ul|ol|tr|td|th|table|h[1-6]|blockquote|pre))\b[^>]*>",
                                                          RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RE_Tag        = new Regex(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex RE_Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

    #endregion




    #region Methods

    /// <summary>Converts HTML to one line of plain text: tags removed, entities decoded, whitespace collapsed.</summary>
    public static string HtmlToPlainText(string? html)
    {
      if (string.IsNullOrEmpty(html))
        return string.Empty;

      var text = RE_ScriptOrStyle.Replace(html, " ");
      text = RE_BlockTag.Replace(text, " ");
      text = RE_Tag.Replace(text, string.Empty);
      text = WebUtility.HtmlDecode(text);

      return RE_Whitespace.Replace(text, " ").Trim();
    }

    /// <summary>
    ///   Shortens <paramref name="text" /> to at most <paramref name="maxLength" /> characters, ellipsis included. Cuts at
    ///   the last word boundary, or between characters when the text has no usable word boundary (for example CJK text).
    /// </summary>
    public static string Truncate(string text, int maxLength)
    {
      ArgumentNullException.ThrowIfNull(text);
      ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 2);

      if (text.Length <= maxLength)
        return text;

      int available = maxLength - Ellipsis.Length;
      int cut       = available;

      while (cut > 0 && char.IsWhiteSpace(text[cut]) == false)
        cut--;

      // A boundary far before the limit wastes most of the title: cut between characters instead.
      if (cut < available / 2)
        cut = available;

      if (char.IsHighSurrogate(text[cut - 1]))
        cut--;

      return text.Substring(0, cut).TrimEnd().TrimEnd(',', ';', ':') + Ellipsis;
    }

    /// <summary>The title of a text extract: the beginning of its text, or <paramref name="fallbackTitle" /> when it has no text.</summary>
    public static string TextExtractTitle(string? html, int maxLength, string fallbackTitle)
    {
      var text = HtmlToPlainText(html);

      return text.Length == 0
        ? fallbackTitle
        : Truncate(text, maxLength);
    }

    /// <summary>Formats zero-based page indices as one-based page numbers: "p. 3" or "p. 3-7".</summary>
    public static string FormatPageRange(int startPageIndex, int endPageIndex)
    {
      return endPageIndex <= startPageIndex
        ? $"p. {startPageIndex + 1}"
        : $"p. {startPageIndex + 1}-{endPageIndex + 1}";
    }

    /// <summary>The title of a sub-PDF extract that has no explicit title.</summary>
    public static string SubPdfTitle(string? bookmarkTitle, string documentTitle, int startPageIndex, int endPageIndex)
    {
      var name = string.IsNullOrWhiteSpace(bookmarkTitle)
        ? documentTitle
        : bookmarkTitle.Trim();

      return $"{name} ({FormatPageRange(startPageIndex, endPageIndex)})";
    }

    /// <summary>The title of an extract that contains only images, for example "Chapter 1 -- Image extract: 2 images from p3, p4".</summary>
    public static string ImageExtractTitle(string prefix, int imageCount, IEnumerable<int> pageIndices)
    {
      var imageString = $"{imageCount} image{(imageCount == 1 ? "" : "s")}";
      var pageString  = "p" + string.Join(", p", pageIndices.Select(p => p + 1));

      return $"{prefix} -- Image extract: {imageString} from {pageString}";
    }

    #endregion
  }
}
