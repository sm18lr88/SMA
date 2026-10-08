namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.IO.Compression;
  using System.Linq;
  using System.Threading;
  using System.Xml;
  using System.Xml.Linq;

  /// <summary>Reads EPUB 2 and EPUB 3 packages: container, OPF metadata, manifest, spine, and table of contents.</summary>
  public static class EpubReader
  {
    private const string ContainerPath = "META-INF/container.xml";
    private const string NcxMediaType  = "application/x-dtbncx+xml";

    private sealed record ManifestItem(string Id, string Path, string MediaType, string[] Properties);

    /// <summary>Reads a book from an EPUB stream.</summary>
    /// <param name="stream">The EPUB (ZIP) content.</param>
    /// <param name="fallbackTitle">The title to use when the OPF has no title, for example the file name.</param>
    /// <param name="ct">Cancels the reading.</param>
    /// <exception cref="EpubFormatException">The stream is not a readable EPUB.</exception>
    public static EpubBook Read(Stream stream, string fallbackTitle, CancellationToken ct)
    {
      try
      {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        return Read(new EpubArchive(zip), fallbackTitle, ct);
      }
      catch (InvalidDataException ex)
      {
        throw new EpubFormatException("The file is not a valid EPUB: it is not a readable ZIP archive.", ex);
      }
      catch (XmlException ex)
      {
        throw new EpubFormatException($"The EPUB package files contain invalid XML: {ex.Message}", ex);
      }
    }

    private static EpubBook Read(EpubArchive zip, string fallbackTitle, CancellationToken ct)
    {
      var container = zip.ReadXml(ContainerPath)
        ?? throw new EpubFormatException("The file is not an EPUB: META-INF/container.xml is missing.");

      var opfPath = container.Descendants().FirstOrDefault(e => e.Name.LocalName == "rootfile")?.Attribute("full-path")?.Value;
      var opf     = opfPath == null ? null : zip.ReadXml(opfPath);

      if (opfPath == null || opf?.Root == null)
        throw new EpubFormatException("The EPUB package document (OPF) is missing.");

      var opfDir   = EpubPath.Directory(opfPath);
      var root     = opf.Root;
      var manifest = ReadManifest(root, opfDir);
      var spine    = Child(root, "spine");
      var navItem  = manifest.Values.FirstOrDefault(i => i.Properties.Contains("nav"));
      var tocTitles = ReadTocTitles(zip, manifest, spine, navItem);

      var documents = new List<EpubDocument>();

      foreach (var idref in Children(spine, "itemref").Select(e => e.Attribute("idref")?.Value))
      {
        ct.ThrowIfCancellationRequested();

        if (idref == null || manifest.TryGetValue(idref, out var item) == false || item == navItem || IsHtml(item) == false)
          continue;

        var html = zip.ReadText(item.Path);
        if (html != null)
          documents.Add(new EpubDocument(item.Path, tocTitles.GetValueOrDefault(item.Path), html));
      }

      if (documents.Count == 0)
        throw new EpubFormatException("The EPUB has no readable chapters in its spine.");

      var resources = new Dictionary<string, EpubResource>(StringComparer.Ordinal);
      foreach (var item in manifest.Values.Where(i => i.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
      {
        ct.ThrowIfCancellationRequested();

        var data = zip.ReadBytes(item.Path);
        if (data != null)
          resources[item.Path] = new EpubResource(item.Path, item.MediaType.ToLowerInvariant(), data);
      }

      return new EpubBook(root.Attribute("version")?.Value ?? string.Empty,
                          OpfMetadataReader.Read(root, fallbackTitle),
                          documents,
                          resources);
    }

    private static Dictionary<string, ManifestItem> ReadManifest(XElement root, string opfDir)
    {
      var items = new Dictionary<string, ManifestItem>(StringComparer.Ordinal);

      foreach (var e in Children(Child(root, "manifest"), "item"))
      {
        var id    = e.Attribute("id")?.Value;
        var href  = e.Attribute("href")?.Value;
        var path  = href == null ? null : EpubPath.Resolve(opfDir, href);

        if (id == null || path == null)
          continue;

        var props = (e.Attribute("properties")?.Value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        items.TryAdd(id, new ManifestItem(id, path, e.Attribute("media-type")?.Value ?? string.Empty, props));
      }

      return items;
    }

    private static Dictionary<string, string> ReadTocTitles(EpubArchive                      zip,
                                                            Dictionary<string, ManifestItem> manifest,
                                                            XElement?                        spine,
                                                            ManifestItem?                    navItem)
    {
      if (navItem != null && zip.ReadText(navItem.Path) is { } navHtml)
        return EpubTocReader.ReadNav(navHtml, navItem.Path);

      var tocId   = spine?.Attribute("toc")?.Value;
      var ncxItem = tocId != null && manifest.TryGetValue(tocId, out var byId)
        ? byId
        : manifest.Values.FirstOrDefault(i => i.MediaType == NcxMediaType);

      return ncxItem != null && zip.ReadXml(ncxItem.Path) is { } ncx
        ? EpubTocReader.ReadNcx(ncx, ncxItem.Path)
        : new Dictionary<string, string>();
    }

    private static bool IsHtml(ManifestItem item) =>
      item.MediaType is "application/xhtml+xml" or "text/html"
      || item.Path.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase)
      || item.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
      || item.Path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase);

    internal static XElement? Child(XElement? parent, string localName) =>
      parent?.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    internal static IEnumerable<XElement> Children(XElement? parent, string localName) =>
      parent?.Elements().Where(e => e.Name.LocalName == localName) ?? [];
  }
}
