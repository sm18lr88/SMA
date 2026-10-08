using System.Collections.Generic;
using System.Net;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>Renders a branch as one standalone HTML5 document.</summary>
  public sealed class HtmlDocumentRenderer : DocumentRenderer
  {
    protected override void BeginDocument(string title)
    {
      Output.Append("<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<title>")
            .Append(WebUtility.HtmlEncode(title.Trim()))
            .Append("</title>\n</head>\n<body>\n");
    }

    protected override void WriteHeading(int level, string title)
    {
      Output.Append("<h").Append(level).Append('>')
            .Append(WebUtility.HtmlEncode(title.Trim()))
            .Append("</h").Append(level).Append(">\n");
    }

    protected override void WriteBoldParagraph(string title)
    {
      Output.Append("<p><strong>").Append(WebUtility.HtmlEncode(title.Trim())).Append("</strong></p>\n");
    }

    protected override void WriteReferences(IReadOnlyList<KeyValuePair<string, string>> references)
    {
      Output.Append("<ul class=\"references\">\n");

      foreach (var (key, value) in references)
        Output.Append("<li>").Append(WebUtility.HtmlEncode(key)).Append(": ")
              .Append(WebUtility.HtmlEncode(value)).Append("</li>\n");

      Output.Append("</ul>\n");
    }

    protected override void WriteBody(string html)
    {
      Output.Append(html).Append('\n');
    }

    protected override void EndDocument()
    {
      Output.Append("</body>\n</html>\n");
    }
  }
}
