// EPUB 2 and EPUB 3 reading, chapter merging, and sanitizing, on packages built in memory.
namespace SuperMemoAssistant.Tests.Books;

using SuperMemoAssistant.Plugins.Books.Epub;
using SuperMemoAssistant.Plugins.Books.Planning;
using Xunit;

public sealed class EpubImportTests
{
  private const string Epub2Metadata =
    """
    <dc:title>The Old Book</dc:title><dc:creator opf:role="aut">Ann Writer</dc:creator><dc:creator>Bob Helper</dc:creator>
    <dc:language>en</dc:language><dc:date>1999-05-01</dc:date>
    <dc:identifier id="BookId">urn:uuid:0b5c7c9e-7d39-4f0e-9a43-2b1f7c6a1e11</dc:identifier>
    <dc:identifier opf:scheme="ISBN">0-306-40615-2</dc:identifier>
    """;

  private const string Epub3Metadata =
    """
    <dc:identifier id="uid">urn:isbn:9780306406157</dc:identifier><dc:title>The New Book</dc:title>
    <dc:creator>Cy Author</dc:creator><dc:language>fr</dc:language>
    """;

  private static readonly string Long = $"<h1 id=\"start\">Heading</h1><p>{EpubFixture.Words(2000)}</p>";

  private static EpubBook Read(EpubFixture fixture)
  {
    using var stream = fixture.Build();
    return EpubReader.Read(stream, "fallback", TestContext.Current.CancellationToken);
  }

  [Fact]
  public void Epub2_ReadsMetadataSpineAndNcxTitles()
  {
    var book = Read(EpubFixture.Epub2(
      [new("c1", "One", Long), new("c2", null, $"<h2>Second heading</h2><p>{EpubFixture.Words(2000)}</p>"), new("c3", "Three", Long)],
      Epub2Metadata));

    Assert.Equal("2.0", book.Version);
    Assert.Equal("The Old Book", book.Metadata.Title);
    Assert.Equal(["Ann Writer", "Bob Helper"], book.Metadata.Creators);
    Assert.Equal("en", book.Metadata.Language);
    Assert.Equal("1999-05-01", book.Metadata.Date);
    Assert.StartsWith("urn:uuid:", book.Metadata.Identifier);
    Assert.Equal("0306406152", book.Metadata.Isbn);
    Assert.Equal(["OEBPS/text/c1.xhtml", "OEBPS/text/c2.xhtml", "OEBPS/text/c3.xhtml"], book.Documents.Select(d => d.Path));
    Assert.Equal(["One", null, "Three"], book.Documents.Select(d => d.TocTitle));
  }

  [Fact]
  public void MissingTocEntry_UsesTheFirstHeading()
  {
    var book     = Read(EpubFixture.Epub2([new("c1", "One", Long), new("c2", null, $"<h2>Second heading</h2><p>{EpubFixture.Words(2000)}</p>")], Epub2Metadata));
    var chapters = ChapterBuilder.Build(book, 1500, TestContext.Current.CancellationToken);

    Assert.Equal(["One", "Second heading"], chapters.Select(c => c.Title));
  }

  [Fact]
  public void Epub3_UsesTheTocNavAndSkipsTheNavigationDocument()
  {
    var book = Read(EpubFixture.Epub3([new("a", "Alpha", Long), new("b", "Beta", Long)], Epub3Metadata));

    Assert.Equal("3.0", book.Version);
    Assert.Equal("The New Book", book.Metadata.Title);
    Assert.Equal("9780306406157", book.Metadata.Isbn);
    Assert.Equal(["OEBPS/a.xhtml", "OEBPS/b.xhtml"], book.Documents.Select(d => d.Path));
    Assert.Equal(["Alpha", "Beta"], book.Documents.Select(d => d.TocTitle));
  }

  [Fact]
  public void SmallCopyrightPage_IsMergedIntoThePreviousChapter_AndEmptyDocumentsAreSkipped()
  {
    var book = Read(EpubFixture.Epub3(
      [
        new("cover", "Cover", "<p>The New Book</p>"),
        new("one", "One", Long),
        new("copyright", "Copyright", "<p>Copyright 2020 Cy Author. All rights reserved.</p>"),
        new("empty", "Empty", "<div>  </div>"),
        new("two", "Two", Long),
      ],
      Epub3Metadata));

    var chapters = ChapterBuilder.Build(book, 1500, TestContext.Current.CancellationToken);

    Assert.Equal(["One", "Two"], chapters.Select(c => c.Title));
    Assert.Contains("The New Book", chapters[0].Html);
    Assert.Contains("All rights reserved", chapters[0].Html);
    Assert.Equal(3, chapters[0].DocumentCount);
    Assert.Equal(1, chapters[1].DocumentCount);
  }

  [Fact]
  public void MergeThresholdZero_KeepsEveryNonEmptyDocument()
  {
    var book = Read(EpubFixture.Epub3([new("one", "One", Long), new("copyright", "Copyright", "<p>Short.</p>")], Epub3Metadata));

    Assert.Equal(2, ChapterBuilder.Build(book, 0, TestContext.Current.CancellationToken).Count);
  }

  [Fact]
  public void Sanitizer_RemovesScriptsStylesHandlersFormsAndExternalResources()
  {
    var html = """
               <script>alert('x')</script><style>p { color: red }</style>
               <h2 onclick="steal()" style="color:red" class="c">Title</h2>
               <p onmouseover="x()">Text with <em>emphasis</em> and <a href="chapter2.xhtml#n1" onclick="y()">a note</a>
               and <a href="https://example.org/">a site</a> and <a href="javascript:evil()">bad</a>.</p>
               <form action="https://evil.example/"><input name="q"/>Form text</form>
               <iframe src="https://evil.example/"></iframe><img src="https://example.org/x.png" alt="Remote"/>
               <ul><li>Item</li></ul><table><tr><td colspan="2">Cell</td></tr></table><blockquote>Quote</blockquote>
               """;

    var result = ChapterSanitizer.Sanitize(EpubFixture.Chapter(html), "OEBPS/c1.xhtml", _ => null);

    Assert.DoesNotContain("script", result.Html, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("alert", result.Html);
    Assert.DoesNotContain("color", result.Html);
    Assert.DoesNotContain("onclick", result.Html);
    Assert.DoesNotContain("onmouseover", result.Html);
    Assert.DoesNotContain("<form", result.Html);
    Assert.DoesNotContain("Form text", result.Html);
    Assert.DoesNotContain("iframe", result.Html);
    Assert.DoesNotContain("javascript", result.Html);
    Assert.DoesNotContain("stylesheet", result.Html);
    Assert.DoesNotContain("src=", result.Html);
    Assert.Contains("<h2>Title</h2>", result.Html);
    Assert.Contains("<em>emphasis</em>", result.Html);
    Assert.Contains("<a href=\"#n1\">a note</a>", result.Html);
    Assert.Contains("<a href=\"https://example.org/\">a site</a>", result.Html);
    Assert.Contains("<ul><li>Item</li></ul>", result.Html);
    Assert.Contains("<td colspan=\"2\">Cell</td>", result.Html);
    Assert.Contains("<blockquote>Quote</blockquote>", result.Html);
    Assert.Contains("[Image: Remote]", result.Html);
    Assert.Equal("Title", result.Heading);
  }

  [Fact]
  public void Images_AreEmbeddedAsBase64_OrReplacedByTheirAltText()
  {
    byte[] png = [0x89, (byte)'P', (byte)'N', (byte)'G', 13, 10, 26, 10, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R', 0, 0, 0x03, 0x20, 0, 0, 0x01, 0x90];
    var images = new Dictionary<string, EpubResource>
    {
      ["OEBPS/img/wide.png"] = new("OEBPS/img/wide.png", "image/png", png),
      ["OEBPS/img/pic.svg"]  = new("OEBPS/img/pic.svg", "image/svg+xml", "<svg/>"u8.ToArray()),
    };

    var result = ChapterSanitizer.Sanitize(
      EpubFixture.Chapter("""<p><img src="../img/wide.png" alt="Map"/><img src="../img/pic.svg" alt="Diagram"/><img src="missing.png"/></p>"""),
      "OEBPS/text/c1.xhtml",
      images.GetValueOrDefault);

    Assert.Contains("data:image/png;base64," + Convert.ToBase64String(png), result.Html);
    Assert.Contains("width=\"800\" height=\"400\" alt=\"Map\"", result.Html);
    Assert.Contains("[Image: Diagram]", result.Html);
    Assert.Contains("[Image]", result.Html);
    Assert.Equal(3, result.ImageCount);
  }

  [Fact]
  public void EpubWithMoreChaptersThanTheChildrenLimit_IsPlannedInParts()
  {
    var chapters = Enumerable.Range(1, 12).Select(i => new FixtureChapter($"c{i}", $"Part {i}", Long)).ToList();
    var book     = Read(EpubFixture.Epub2(chapters, Epub2Metadata));
    var built    = ChapterBuilder.Build(book, 1500, TestContext.Current.CancellationToken);
    var root     = EpubPlanner.Plan(book.Metadata, "old.epub", built, new PlanOptions(40, 0.1, 5));

    Assert.Equal(12, built.Count);
    Assert.Equal(3, root.Children.Count);
    Assert.All(root.Children, part => Assert.InRange(part.Children.Count, 1, 5));
    Assert.Equal(["Part 1", "Part 6", "Part 11"], root.Children.Select(p => p.Children[0].Title));
  }

  [Fact]
  public void InvalidFiles_ThrowUserFacingErrors()
  {
    using var notZip = new MemoryStream("not a zip"u8.ToArray());
    var zipError = Assert.Throws<EpubFormatException>(() => EpubReader.Read(notZip, "x", TestContext.Current.CancellationToken));
    Assert.Contains("ZIP", zipError.Message);

    using var noContainer = new EpubFixture().Add("OEBPS/a.xhtml", "<p/>").Build();
    var containerError = Assert.Throws<EpubFormatException>(() => EpubReader.Read(noContainer, "x", TestContext.Current.CancellationToken));
    Assert.Contains("container.xml", containerError.Message);
  }

  [Theory]
  [InlineData("OEBPS/text/", "../img/a.png", "OEBPS/img/a.png")]
  [InlineData("OEBPS/", "text/ch%201.xhtml#p3", "OEBPS/text/ch 1.xhtml")]
  [InlineData("", "../outside.xhtml", null)]
  [InlineData("OEBPS/", "https://example.org/a.png", null)]
  public void Paths_ResolveInsideThePackage(string dir, string href, string? expected)
  {
    Assert.Equal(expected, EpubPath.Resolve(dir, href));
  }
}
