// Builds small EPUB 2 (NCX) and EPUB 3 (nav) packages in memory for the Books plugin tests.
namespace SuperMemoAssistant.Tests.Books;

using System.IO.Compression;
using System.Text;

internal sealed record FixtureChapter(string Id, string? TocTitle, string Body);

internal sealed class EpubFixture
{
  private readonly List<(string Path, byte[] Data)> _files = [];

  public static string Words(int characters)
  {
    var sb = new StringBuilder();
    while (sb.Length < characters)
      sb.Append("lorem ");
    return sb.ToString(0, characters).Trim();
  }

  public static string Chapter(string body, string title = "Chapter") =>
    $"""
     <?xml version="1.0" encoding="utf-8"?>
     <!DOCTYPE html>
     <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
     <head><title>{title}</title><link rel="stylesheet" href="style.css"/></head>
     <body>{body}</body>
     </html>
     """;

  public EpubFixture Add(string path, string text) => Add(path, Encoding.UTF8.GetBytes(text));

  public EpubFixture Add(string path, byte[] data)
  {
    _files.Add((path, data));
    return this;
  }

  public MemoryStream Build()
  {
    var stream = new MemoryStream();

    using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
      var mimetype = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
      using (var writer = new StreamWriter(mimetype.Open(), new UTF8Encoding(false)))
        writer.Write("application/epub+zip");

      foreach (var (path, data) in _files)
      {
        using var entry = zip.CreateEntry(path).Open();
        entry.Write(data);
      }
    }

    stream.Position = 0;
    return stream;
  }

  public static EpubFixture Epub2(IReadOnlyList<FixtureChapter> chapters, string metadata)
  {
    var manifest = string.Concat(chapters.Select(c => $"""<item id="{c.Id}" href="text/{c.Id}.xhtml" media-type="application/xhtml+xml"/>"""));
    var spine    = string.Concat(chapters.Select(c => $"""<itemref idref="{c.Id}"/>"""));
    var points   = string.Concat(chapters.Where(c => c.TocTitle != null).Select((c, i) =>
      $"""<navPoint id="np{i}" playOrder="{i + 1}"><navLabel><text>{c.TocTitle}</text></navLabel><content src="text/{c.Id}.xhtml#start"/></navPoint>"""));

    var fixture = WithContainer("OEBPS/content.opf")
      .Add("OEBPS/content.opf",
           $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="BookId">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:opf="http://www.idpf.org/2007/opf">{metadata}</metadata>
              <manifest><item id="ncx" href="toc.ncx" media-type="application/x-dtbncx+xml"/>{manifest}</manifest>
              <spine toc="ncx">{spine}</spine>
            </package>
            """)
      .Add("OEBPS/toc.ncx",
           $"""
            <?xml version="1.0" encoding="utf-8"?>
            <!DOCTYPE ncx PUBLIC "-//NISO//DTD ncx 2005-1//EN" "http://www.daisy.org/z3986/2005/ncx-2005-1.dtd">
            <ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1"><navMap>{points}</navMap></ncx>
            """);

    foreach (var c in chapters)
      fixture.Add($"OEBPS/text/{c.Id}.xhtml", Chapter(c.Body));

    return fixture;
  }

  public static EpubFixture Epub3(IReadOnlyList<FixtureChapter> chapters, string metadata)
  {
    var manifest = string.Concat(chapters.Select(c => $"""<item id="{c.Id}" href="{c.Id}.xhtml" media-type="application/xhtml+xml"/>"""));
    var spine    = string.Concat(chapters.Select(c => $"""<itemref idref="{c.Id}"/>"""));
    var links    = string.Concat(chapters.Where(c => c.TocTitle != null).Select(c => $"""<li><a href="{c.Id}.xhtml">{c.TocTitle}</a></li>"""));

    var fixture = WithContainer("OEBPS/package.opf")
      .Add("OEBPS/package.opf",
           $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="uid">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">{metadata}</metadata>
              <manifest><item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>{manifest}</manifest>
              <spine><itemref idref="nav"/>{spine}</spine>
            </package>
            """)
      .Add("OEBPS/nav.xhtml",
           Chapter($"""<nav epub:type="landmarks"><ol><li><a href="nav.xhtml">Landmarks</a></li></ol></nav><nav epub:type="toc"><h1>Contents</h1><ol>{links}</ol></nav>""", "Contents"));

    foreach (var c in chapters)
      fixture.Add($"OEBPS/{c.Id}.xhtml", Chapter(c.Body));

    return fixture;
  }

  private static EpubFixture WithContainer(string opfPath)
  {
    return new EpubFixture().Add(
      "META-INF/container.xml",
      $"""
      <?xml version="1.0"?>
      <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
        <rootfiles><rootfile full-path="{opfPath}" media-type="application/oebps-package+xml"/></rootfiles>
      </container>
      """);
  }
}
