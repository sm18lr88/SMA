using System.Collections.Generic;
using System.Text;
using SuperMemoAssistant.Plugins.Writing.Html;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>Renders a branch as CommonMark. Blocks are separated by one blank line.</summary>
  public sealed class MarkdownDocumentRenderer : DocumentRenderer
  {
    private readonly HtmlToMarkdown _converter = new();

    /// <summary>Escapes the characters that would make plain text Markdown syntax.</summary>
    public static string Escape(string text)
    {
      var escaped = new StringBuilder(text.Length);

      foreach (var c in text.ReplaceLineEndings(" "))
      {
        if (c is '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '#' or '|')
          escaped.Append('\\');

        escaped.Append(c);
      }

      return escaped.ToString();
    }

    protected override void BeginDocument(string title) { }

    protected override void WriteHeading(int level, string title)
    {
      WriteBlock(new string('#', level) + " " + Escape(title.Trim()));
    }

    protected override void WriteBoldParagraph(string title)
    {
      WriteBlock("**" + Escape(title.Trim()) + "**");
    }

    protected override void WriteReferences(IReadOnlyList<KeyValuePair<string, string>> references)
    {
      var list = new StringBuilder();

      foreach (var (key, value) in references)
        list.Append("- ").Append(Escape(key)).Append(": ").Append(Escape(value)).Append('\n');

      WriteBlock(list.ToString().TrimEnd());
    }

    protected override void WriteBody(string html)
    {
      var markdown = _converter.Convert(html);

      if (markdown.Length > 0)
        WriteBlock(markdown);
    }

    protected override void EndDocument() { }

    private void WriteBlock(string block)
    {
      if (Output.Length > 0)
        Output.Append('\n');

      Output.Append(block).Append('\n');
    }
  }
}
