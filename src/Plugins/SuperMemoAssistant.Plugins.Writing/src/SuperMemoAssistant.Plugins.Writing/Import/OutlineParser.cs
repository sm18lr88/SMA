using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace SuperMemoAssistant.Plugins.Writing.Import
{
  /// <summary>
  ///   Parses Markdown (CommonMark, pipe tables, task lists, strikethrough, YAML front matter) into an outline. Only
  ///   top-level ATX and setext headings define the tree, so "#" inside code blocks, lists, or quotes is content.
  /// </summary>
  public static class OutlineParser
  {
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
                                                        .UseYamlFrontMatter()
                                                        .UsePipeTables()
                                                        .UseTaskLists()
                                                        .UseAutoLinks()
                                                        .UseEmphasisExtras()
                                                        .Build();

    /// <summary>Parses a Markdown text into an outline.</summary>
    public static Outline Parse(string markdown)
    {
      ArgumentNullException.ThrowIfNull(markdown);

      var document = Markdown.Parse(markdown, Pipeline);
      var roots    = new List<OutlineNode>();
      var open     = new Stack<OutlineNode>();
      var blocks   = new List<Block>();
      string? title    = null;
      var preamble = string.Empty;
      OutlineNode? current = null;

      foreach (var block in document)
        switch (block)
        {
          case YamlFrontMatterBlock frontMatter:
            title = FrontMatterTitle(frontMatter);
            break;

          case HeadingBlock heading:
            Flush();
            current = new OutlineNode(PlainText(heading.Inline), heading.Level);

            while (open.Count > 0 && open.Peek().Level >= current.Level)
              open.Pop();

            (open.Count > 0 ? open.Peek().Children : roots).Add(current);
            open.Push(current);
            break;

          default:
            blocks.Add(block);
            break;
        }

      Flush();

      return new Outline(title, preamble, roots);

      void Flush()
      {
        var html = RenderHtml(blocks);
        blocks.Clear();

        if (current == null)
          preamble = html;
        else
          current.Html = html;
      }
    }

    private static string RenderHtml(List<Block> blocks)
    {
      if (blocks.Count == 0)
        return string.Empty;

      using var writer   = new StringWriter();
      var       renderer = new HtmlRenderer(writer);
      Pipeline.Setup(renderer);

      foreach (var block in blocks)
        renderer.Render(block);

      writer.Flush();

      return writer.ToString().Trim();
    }

    /// <summary>The visible text of a heading, without Markdown syntax.</summary>
    private static string PlainText(ContainerInline? inline)
    {
      var text = new StringBuilder();
      AppendText(inline, text);

      var result = string.Join(' ', text.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

      return result.Length > 0 ? result : "(untitled)";
    }

    private static void AppendText(Inline? inline, StringBuilder text)
    {
      switch (inline)
      {
        case LiteralInline literal:
          text.Append(literal.Content.ToString());
          break;

        case CodeInline code:
          text.Append(code.Content);
          break;

        case HtmlEntityInline entity:
          text.Append(entity.Transcoded.ToString());
          break;

        case AutolinkInline autolink:
          text.Append(autolink.Url);
          break;

        case LineBreakInline:
          text.Append(' ');
          break;

        case ContainerInline container:
          foreach (var child in container)
            AppendText(child, text);
          break;
      }
    }

    private static string? FrontMatterTitle(YamlFrontMatterBlock frontMatter)
    {
      var line = frontMatter.Lines.ToString()
                            .Split('\n', StringSplitOptions.TrimEntries)
                            .FirstOrDefault(l => l.StartsWith("title:", StringComparison.OrdinalIgnoreCase));

      var value = line?["title:".Length..].Trim().Trim('"', '\'').Trim();

      return string.IsNullOrEmpty(value) ? null : value;
    }
  }
}
