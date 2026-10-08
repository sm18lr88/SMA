using System;
using System.Collections.Generic;
using System.Text;
using SuperMemoAssistant.Plugins.Writing.Html;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>
  ///   Renders a <see cref="BranchDocument" /> as linear text. Each section title becomes a heading whose level follows
  ///   the depth. Levels deeper than 6 become bold paragraphs.
  /// </summary>
  public abstract class DocumentRenderer
  {
    /// <summary>The deepest heading level that Markdown and HTML support.</summary>
    public const int MaxHeadingLevel = 6;

    protected StringBuilder Output { get; } = new();

    /// <summary>Creates the renderer for a format.</summary>
    public static DocumentRenderer For(OutputFormat format)
    {
      return format switch
      {
        OutputFormat.Markdown => new MarkdownDocumentRenderer(),
        OutputFormat.Html     => new HtmlDocumentRenderer(),
        _                     => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown output format."),
      };
    }

    /// <summary>
    ///   Renders the document. With a title page, the root title is a level-1 heading followed by the root references,
    ///   and the children start at level 2. Without it, the root title is left out and the children start at level 1.
    /// </summary>
    public string Render(BranchDocument document, bool includeTitlePage, ImageLinker images)
    {
      ArgumentNullException.ThrowIfNull(document);
      ArgumentNullException.ThrowIfNull(images);

      Output.Clear();
      BeginDocument(document.Title);

      foreach (var section in document.Sections)
      {
        var content = SuperMemoHtml.Clean(section.Html, images.Link);
        var level   = section.Depth + (includeTitlePage ? 1 : 0);

        if (section.Depth > 0 || includeTitlePage)
          WriteTitle(level, section.Title);

        if (section.Depth == 0 && includeTitlePage && content.References.Count > 0)
          WriteReferences(content.References);

        if (content.Body.Length > 0)
          WriteBody(content.Body);
      }

      EndDocument();

      return Output.ToString();
    }

    private void WriteTitle(int level, string title)
    {
      if (level <= MaxHeadingLevel)
        WriteHeading(level, title);
      else
        WriteBoldParagraph(title);
    }

    protected abstract void BeginDocument(string title);

    protected abstract void WriteHeading(int level, string title);

    protected abstract void WriteBoldParagraph(string title);

    protected abstract void WriteReferences(IReadOnlyList<KeyValuePair<string, string>> references);

    /// <summary>Writes the cleaned HTML of a section.</summary>
    protected abstract void WriteBody(string html);

    protected abstract void EndDocument();
  }
}
