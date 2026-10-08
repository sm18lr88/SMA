namespace SuperMemoAssistant.Plugins.Books.Kindle
{
  using System;
  using System.Globalization;
  using System.Security.Cryptography;
  using System.Text;
  using System.Text.RegularExpressions;

  /// <summary>
  ///   Computes a stable identity for a clipping, so that re-importing a clippings file does not duplicate it. The hash
  ///   covers the book, the kind, the location, and the whitespace-normalized text. The date is excluded, because Kindle
  ///   can write the same passage again with a new date.
  /// </summary>
  public static partial class HighlightHasher
  {
    /// <summary>Returns a 32-character lowercase hexadecimal SHA-256 prefix.</summary>
    public static string Hash(KindleClipping clipping)
    {
      var key = string.Join(
        "\n",
        Normalize(clipping.Title),
        Normalize(clipping.Author ?? string.Empty),
        clipping.Kind.ToString(),
        clipping.LocationStart?.ToString(CultureInfo.InvariantCulture) ?? Normalize(clipping.Page ?? string.Empty),
        clipping.LocationEnd?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        Normalize(clipping.Text));

      var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
      return Convert.ToHexStringLower(hash.AsSpan(0, 16));
    }

    private static string Normalize(string text) => WhitespaceRegex().Replace(text, " ").Trim().ToUpperInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
  }
}
