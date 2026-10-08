namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml.Linq;
  using HtmlAgilityPack;

  /// <summary>
  ///   Maps spine document paths to chapter titles, from the EPUB 3 navigation document or the EPUB 2 NCX. The first
  ///   entry that points into a document gives its title.
  /// </summary>
  public static class EpubTocReader
  {
    /// <summary>Reads the "toc" nav element of an EPUB 3 navigation document.</summary>
    public static Dictionary<string, string> ReadNav(string navHtml, string navPath)
    {
      var doc = new HtmlDocument();
      doc.LoadHtml(navHtml);

      var navs = doc.DocumentNode.Descendants("nav").ToList();
      var toc  = navs.FirstOrDefault(n => n.GetAttributeValue("epub:type", string.Empty).Split(' ').Contains("toc")) ?? navs.FirstOrDefault();
      var dir  = EpubPath.Directory(navPath);
      var map  = new Dictionary<string, string>(StringComparer.Ordinal);

      if (toc == null)
        return map;

      foreach (var link in toc.Descendants("a"))
        Add(map, dir, link.GetAttributeValue("href", string.Empty), HtmlEntity.DeEntitize(link.InnerText));

      return map;
    }

    /// <summary>Reads the navMap of an EPUB 2 NCX document, in document order.</summary>
    public static Dictionary<string, string> ReadNcx(XDocument ncx, string ncxPath)
    {
      var dir = EpubPath.Directory(ncxPath);
      var map = new Dictionary<string, string>(StringComparer.Ordinal);

      foreach (var point in ncx.Descendants().Where(e => e.Name.LocalName == "navPoint"))
      {
        var label = EpubReader.Child(EpubReader.Child(point, "navLabel"), "text")?.Value ?? string.Empty;
        var src   = EpubReader.Child(point, "content")?.Attribute("src")?.Value ?? string.Empty;

        Add(map, dir, src, label);
      }

      return map;
    }

    private static void Add(Dictionary<string, string> map, string dir, string href, string label)
    {
      var path  = EpubPath.Resolve(dir, href);
      var title = OpfMetadataReader.Normalize(label);

      if (path != null && title.Length > 0)
        map.TryAdd(path, title);
    }
  }
}
