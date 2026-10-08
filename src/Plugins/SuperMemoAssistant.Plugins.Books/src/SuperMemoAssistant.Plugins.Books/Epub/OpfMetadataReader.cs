namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Linq;
  using System.Text.RegularExpressions;
  using System.Xml.Linq;

  /// <summary>Reads the Dublin Core metadata of an OPF package element.</summary>
  internal static partial class OpfMetadataReader
  {
    public static EpubMetadata Read(XElement package, string fallbackTitle)
    {
      var metadata = EpubReader.Child(package, "metadata");

      string? First(string name) => EpubReader.Children(metadata, name)
                                               .Select(e => Normalize(e.Value))
                                               .FirstOrDefault(v => v.Length > 0);

      var creators = EpubReader.Children(metadata, "creator")
                               .Select(e => Normalize(e.Value))
                               .Where(v => v.Length > 0)
                               .Distinct(StringComparer.Ordinal)
                               .ToList();

      var identifiers = EpubReader.Children(metadata, "identifier").ToList();
      var uniqueId    = package.Attribute("unique-identifier")?.Value;
      var identifier  = identifiers.FirstOrDefault(e => uniqueId != null && e.Attribute("id")?.Value == uniqueId) ?? identifiers.FirstOrDefault();

      return new EpubMetadata(First("title") ?? fallbackTitle,
                              creators,
                              First("language"),
                              First("date"),
                              identifier == null ? null : Normalize(identifier.Value),
                              identifiers.Select(FindIsbn).FirstOrDefault(i => i != null));
    }

    /// <summary>Returns the ISBN digits of an identifier element, or <see langword="null" />.</summary>
    private static string? FindIsbn(XElement identifier)
    {
      var value  = Normalize(identifier.Value);
      var scheme = identifier.Attributes().FirstOrDefault(a => a.Name.LocalName == "scheme")?.Value;
      var isIsbnScheme = string.Equals(scheme, "ISBN", StringComparison.OrdinalIgnoreCase);

      var match = IsbnRegex().Match(value);
      if (match.Success == false)
        return null;

      var digits = match.Groups[1].Value.Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
      var prefixed = value.StartsWith("urn:isbn:", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("isbn", StringComparison.OrdinalIgnoreCase);
      var plausible = digits.Length == 10 || (digits.Length == 13 && (digits.StartsWith("978", StringComparison.Ordinal) || digits.StartsWith("979", StringComparison.Ordinal)));

      return plausible && (isIsbnScheme || prefixed || digits.Length == 13) ? digits : null;
    }

    internal static string Normalize(string value) => WhitespaceRegex().Replace(value, " ").Trim();

    [GeneratedRegex(@"([0-9][0-9\- ]{8,15}[0-9Xx])")]
    private static partial Regex IsbnRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
  }
}
