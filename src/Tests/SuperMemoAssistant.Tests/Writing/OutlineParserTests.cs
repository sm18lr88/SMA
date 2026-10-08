using Xunit;
using SuperMemoAssistant.Plugins.Writing.Import;

namespace SuperMemoAssistant.Tests.Writing;

public sealed class OutlineParserTests
{
  private static IEnumerable<string> Flatten(IEnumerable<OutlineNode> nodes, string indent = "")
  {
    return nodes.SelectMany(n => Flatten(n.Children, indent + "  ").Prepend(indent + n.Title));
  }

  [Fact]
  public void Headings_DefineANestedTree_InDocumentOrder()
  {
    var outline = OutlineParser.Parse("# A\ntext a\n## A.1\n## A.2\n### A.2.1\n# B\n## B.1\n");

    Assert.Equal(["A", "  A.1", "  A.2", "    A.2.1", "B", "  B.1"], Flatten(outline.Roots));
    Assert.Equal("<p>text a</p>", outline.Roots[0].Html);
    Assert.Equal(string.Empty, outline.Roots[0].Children[0].Html);
  }

  [Fact]
  public void ContentBeforeTheFirstHeading_IsThePreamble()
  {
    var outline = OutlineParser.Parse("Some *intro*.\n\n# First\nBody\n");

    Assert.Equal("<p>Some <em>intro</em>.</p>", outline.PreambleHtml);
    Assert.Equal("<p>Body</p>", Assert.Single(outline.Roots).Html);
  }

  [Fact]
  public void SkippedHeadingLevels_NestUnderTheNearestHigherHeading()
  {
    var outline = OutlineParser.Parse("# A\n### Deep\n## Mid\n#### Deeper\n# B\n");

    Assert.Equal(["A", "  Deep", "  Mid", "    Deeper", "B"], Flatten(outline.Roots));
  }

  [Fact]
  public void ADocumentThatStartsBelowLevelOne_KeepsLaterHigherHeadingsAtTheTop()
  {
    var outline = OutlineParser.Parse("## Two\n# One\n### Three\n");

    Assert.Equal(["Two", "One", "  Three"], Flatten(outline.Roots));
  }

  [Fact]
  public void SetextHeadings_AreHeadings()
  {
    var outline = OutlineParser.Parse("Title\n=====\n\nSub\n---\ntext\n");

    Assert.Equal(["Title", "  Sub"], Flatten(outline.Roots));
    Assert.Equal("<p>text</p>", outline.Roots[0].Children[0].Html);
  }

  [Fact]
  public void HashesInCodeBlocksQuotesAndLists_AreContent()
  {
    var outline = OutlineParser.Parse("# Code\n```bash\n# not a heading\necho hi\n```\n\n    # indented code\n\n> # quoted\n\n- # in a list\n");

    var node = Assert.Single(outline.Roots);
    Assert.Empty(node.Children);
    Assert.Contains("<pre><code class=\"language-bash\"># not a heading\necho hi\n</code></pre>", node.Html);
    Assert.Contains("# indented code", node.Html);
    Assert.Contains("<blockquote>", node.Html);
    Assert.Contains("<li>", node.Html);
  }

  [Fact]
  public void HeadingTitles_ArePlainText()
  {
    var outline = OutlineParser.Parse("# The *great* `code` &amp; [link](https://x.org) #\n");

    Assert.Equal("The great code & link", outline.Roots[0].Title);
  }

  [Fact]
  public void ContentKeepsTablesAndReferenceLinks()
  {
    var outline = OutlineParser.Parse("# T\n| a | b |\n|---|---|\n| 1 | 2 |\n\nSee [docs][d].\n\n# U\n[d]: https://docs.org\n");

    Assert.Contains("<table>", outline.Roots[0].Html);
    Assert.Contains("<a href=\"https://docs.org\">docs</a>", outline.Roots[0].Html);
    Assert.Equal(string.Empty, outline.Roots[1].Html);
  }

  [Fact]
  public void FrontMatterTitle_IsRead_AndNotContent()
  {
    var outline = OutlineParser.Parse("---\ntitle: \"My essay\"\ntags: [a]\n---\n# One\n");

    Assert.Equal("My essay", outline.Title);
    Assert.Equal(string.Empty, outline.PreambleHtml);
  }
}
