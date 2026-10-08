using Xunit;
using SuperMemoAssistant.Plugins.Writing.Html;

namespace SuperMemoAssistant.Tests.Writing;

public sealed class HtmlToMarkdownTests
{
  private static string Convert(string superMemoHtml)
  {
    return new HtmlToMarkdown().Convert(SuperMemoHtml.Clean(superMemoHtml).Body);
  }

  [Fact]
  public void FontAndStyledSpans_BecomeEmphasis()
  {
    var markdown = Convert(
      "<P><FONT face=Arial size=3>Incremental <STRONG>writing</STRONG> is <SPAN style=\"FONT-WEIGHT: bold\">powerful</SPAN>"
      + " and <SPAN style=\"FONT-STYLE: italic\">fun</SPAN>, not <SPAN style=\"COLOR: red\">red</SPAN>.</FONT></P>");

    Assert.Equal("Incremental **writing** is **powerful** and *fun*, not red.", markdown);
  }

  [Fact]
  public void ParagraphsAndLineBreaks()
  {
    Assert.Equal("First paragraph\n\nLine one  \nLine two", Convert("<P>First paragraph</P><P>Line one<BR>Line two</P>"));
  }

  [Fact]
  public void HeadingsLinksAndImages()
  {
    var markdown = Convert(
      "<H2 align=center>Section</H2><P><A href=\"https://supermemo.guru/wiki/Incremental_writing\" target=_blank>Incremental writing</A>"
      + " <IMG title=Graph alt=Graph src=\"https://example.org/graph.png\" width=200></P>");

    Assert.Equal(
      "## Section\n\n[Incremental writing](https://supermemo.guru/wiki/Incremental_writing) ![Graph](https://example.org/graph.png \"Graph\")",
      markdown);
  }

  [Fact]
  public void UnclosedListItems_BecomeLists()
  {
    Assert.Equal("- First\n- Second\n\n1. One\n2. Two", Convert("<UL><LI>First<LI>Second</UL><OL><LI>One<LI>Two</OL>"));
  }

  [Fact]
  public void Clean_ClosesImplicitlyClosedElements_AndDropsIeAttributes()
  {
    var clean = SuperMemoHtml.Clean("<P align=left>One<P>Two<UL><LI>First<LI>Second</UL><TD width=3>x");

    Assert.Equal("<p>One</p><p>Two</p><ul><li>First</li><li>Second</li></ul>x", clean.Body);
  }

  [Fact]
  public void Tables_BecomePipeTables()
  {
    var markdown = Convert(
      "<TABLE border=1 cellSpacing=0><TBODY><TR><TD><P>Name</P></TD><TD>Value</TD></TR><TR><TD>a</TD><TD>1</TD></TR></TBODY></TABLE>");

    Assert.Equal("| Name | Value |\n| --- | --- |\n| a | 1 |", markdown);
  }

  [Fact]
  public void BlockQuotesAndCode()
  {
    var markdown = Convert("<BLOCKQUOTE><P>Quoted</P></BLOCKQUOTE><PRE>var x = 1;\nreturn x;</PRE><P>Use <CODE>x</CODE>.</P>");

    Assert.Equal("> Quoted\n\n```\nvar x = 1;\nreturn x;\n```\n\nUse `x`.", markdown);
  }

  [Fact]
  public void ReferencesCommentsAndScripts_AreRemoved()
  {
    var clean = SuperMemoHtml.Clean(
      "<HTML><HEAD><TITLE>x</TITLE><STYLE>p{}</STYLE></HEAD><BODY><!-- c --><P>Text</P><SCRIPT>alert(1)</SCRIPT>"
      + "<br><br><hr SuperMemo><SuperMemoReference><H5 dir=ltr align=left><FONT style=\"COLOR: transparent\" size=1>"
      + "#SuperMemo Reference:</FONT><BR><FONT class=reference>#Title: Advantages of incremental writing<br>"
      + "#Source: <a>https://supermemo.guru</a><br>#Date: </FONT></SuperMemoReference></BODY></HTML>");

    Assert.Equal("<p>Text</p>", clean.Body);
    Assert.Equal(
      [KeyValuePair.Create("Title", "Advantages of incremental writing"), KeyValuePair.Create("Source", "https://supermemo.guru")],
      clean.References);
  }
}
