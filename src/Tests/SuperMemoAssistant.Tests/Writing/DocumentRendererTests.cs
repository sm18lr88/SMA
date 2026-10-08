using Xunit;
using SuperMemoAssistant.Plugins.Writing.Export;

namespace SuperMemoAssistant.Tests.Writing;

public sealed class DocumentRendererTests
{
  private const string References =
    "<br><br><hr SuperMemo><SuperMemoReference><H5 dir=ltr align=left><FONT style=\"COLOR: transparent\" size=1>"
    + "#SuperMemo Reference:</FONT><BR><FONT class=reference>#Title: Incremental writing<br>#Author: Piotr Wozniak</FONT>"
    + "</SuperMemoReference>";

  private static BranchDocument Document(params (int Depth, string Title, string Html)[] sections)
  {
    return new BranchDocument(sections[0].Title,
                              sections.Select((s, i) => new Section(i + 1, s.Depth, s.Title, NodeKind.Topic, s.Html)).ToList());
  }

  private static string Render(OutputFormat format, BranchDocument document, bool titlePage = true, ImageLinker? images = null)
  {
    return DocumentRenderer.For(format).Render(document, titlePage, images ?? new ImageLinker("out_files"));
  }

  [Fact]
  public void Markdown_HeadingLevelsFollowTheDepth_WithATitlePage()
  {
    var document = Document((0, "Article", "<P>Intro" + References), (1, "Part", "<P>Body</P>"), (2, "Detail", ""));

    var markdown = Render(OutputFormat.Markdown, document);

    Assert.Equal(
      "# Article\n\n- Title: Incremental writing\n- Author: Piotr Wozniak\n\nIntro\n\n## Part\n\nBody\n\n### Detail\n",
      markdown);
  }

  [Fact]
  public void Markdown_WithoutATitlePage_StartsTheChildrenAtLevelOne()
  {
    var document = Document((0, "Article", "<P>Intro" + References), (1, "Part", "<P>Body</P>"));

    Assert.Equal("Intro\n\n# Part\n\nBody\n", Render(OutputFormat.Markdown, document, titlePage: false));
  }

  [Fact]
  public void Markdown_LevelsDeeperThanSix_BecomeBoldParagraphs()
  {
    var document = Document((0, "L1", ""), (1, "L2", ""), (2, "L3", ""), (3, "L4", ""), (4, "L5", ""), (5, "L6", ""),
                            (6, "L7", ""), (7, "L8", ""));

    var lines = Render(OutputFormat.Markdown, document).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    Assert.Equal(["# L1", "## L2", "### L3", "#### L4", "##### L5", "###### L6", "**L7**", "**L8**"], lines);
  }

  [Fact]
  public void Markdown_EscapesTitles()
  {
    var markdown = Render(OutputFormat.Markdown, Document((0, "C# *notes* [1]", "")));

    Assert.Equal("# C\\# \\*notes\\* \\[1\\]\n", markdown);
  }

  [Fact]
  public void Html_HeadingLevelsFollowTheDepth_AndDeepLevelsBecomeBoldParagraphs()
  {
    var document = Document((0, "Article <1>", "<P>Intro" + References), (1, "Part", "<P>Body</P>"), (6, "Deep", ""));

    var html = Render(OutputFormat.Html, document);

    Assert.StartsWith("<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<title>Article &lt;1&gt;</title>", html);
    Assert.Contains("<h1>Article &lt;1&gt;</h1>\n<ul class=\"references\">\n<li>Title: Incremental writing</li>\n"
                  + "<li>Author: Piotr Wozniak</li>\n</ul>\n<p>Intro</p>\n<h2>Part</h2>\n<p>Body</p>\n"
                  + "<p><strong>Deep</strong></p>\n</body>\n</html>\n", html);
    Assert.DoesNotContain("SuperMemo", html);
  }

  [Fact]
  public void Html_WithoutATitlePage_LeavesOutTheRootTitleAndReferences()
  {
    var html = Render(OutputFormat.Html, Document((0, "Article", "<P>Intro" + References), (1, "Part", "")), titlePage: false);

    Assert.Contains("<body>\n<p>Intro</p>\n<h1>Part</h1>\n</body>", html);
    Assert.DoesNotContain("references", html);
  }

  [Theory]
  [InlineData(OutputFormat.Markdown, "![](out_files/photo.png)")]
  [InlineData(OutputFormat.Html, "<img src=\"out_files/photo.png\">")]
  public void LocalImages_AreRewrittenToTheImageFolder(OutputFormat format, string expected)
  {
    var images   = new ImageLinker("out_files");
    var document = Document((0, "Article", "<P><IMG src=\"file:///C:/collection/elements/photo.png\"></P>"));

    var output = Render(format, document, images: images);

    Assert.Contains(expected, output);
    Assert.Equal([new ImageCopy(@"C:\collection\elements\photo.png", Path.Combine("out_files", "photo.png"))], images.Copies);
  }

  [Fact]
  public void ImageLinker_GivesUniqueNames_AndKeepsWebImages()
  {
    var images = new ImageLinker("My notes_files");

    Assert.Equal("My notes_files/a%20b.png", images.Link(@"C:\one\a b.png"));
    Assert.Equal("My notes_files/a%20b-2.png", images.Link("file:///C:/two/a%20b.png"));
    Assert.Equal("My notes_files/a%20b.png", images.Link(@"C:\ONE\a b.png"));
    Assert.Equal("https://example.org/x.png", images.Link("https://example.org/x.png"));
    Assert.Equal("relative/x.png", images.Link("relative/x.png"));
    Assert.Equal(2, images.Copies.Count);
    Assert.Equal("My notes_files", ImageLinker.FolderNameFor(@"D:\out\My notes.md"));
  }

  [Fact]
  public void DocumentWriter_WritesTheFile_CopiesImages_AndReportsMissingOnes()
  {
    var folder = Path.Combine(Path.GetTempPath(), "sma-writing-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(folder);

    try
    {
      var image = Path.Combine(folder, "source.png");
      File.WriteAllBytes(image, [1, 2, 3]);

      var tree = new InMemoryTree();
      var root = tree.Add(null, "Article", $"<P><IMG src=\"{new Uri(image).AbsoluteUri}\"><IMG src=\"{Path.Combine(folder, "gone.png")}\"></P>");
      var output = Path.Combine(Directory.CreateDirectory(Path.Combine(folder, "out")).FullName, "Article.md");

      var (document, result) = DocumentWriter.CompileToFile(tree, root, new CompileOptions(), output, null, TestContext.Current.CancellationToken);

      Assert.Single(document.Sections);
      Assert.Equal(1, result.ImagesCopied);
      Assert.Equal([Path.Combine(folder, "gone.png")], result.MissingImages);
      Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(folder, "out", "Article_files", "source.png")));
      Assert.Contains("![](Article_files/source.png)", File.ReadAllText(output));
    }
    finally
    {
      Directory.Delete(folder, true);
    }
  }
}
