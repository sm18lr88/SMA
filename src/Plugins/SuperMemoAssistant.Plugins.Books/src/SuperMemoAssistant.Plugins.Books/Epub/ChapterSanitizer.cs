namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Text;
  using HtmlAgilityPack;

  /// <summary>The safe HTML of one chapter document and the facts that the planner needs.</summary>
  public sealed record SanitizedChapter(string Html, int TextLength, int ImageCount, string? Heading, string? DocumentTitle);

  /// <summary>
  ///   Converts chapter XHTML to safe HTML for SuperMemo. An allow list keeps headings, paragraphs, emphasis, lists,
  ///   tables, block quotes, and links. Scripts, styles, event handlers, forms, and external resources are removed.
  /// </summary>
  public static class ChapterSanitizer
  {
    private static readonly HashSet<string> Dropped = new(StringComparer.OrdinalIgnoreCase)
    {
      "script", "style", "noscript", "form", "iframe", "frame", "frameset", "object", "embed", "applet", "link", "meta",
      "head", "title", "template", "audio", "video", "canvas", "input", "button", "select", "textarea", "base", "source",
    };

    private static readonly HashSet<string> Kept = new(StringComparer.OrdinalIgnoreCase)
    {
      "h1", "h2", "h3", "h4", "h5", "h6", "p", "br", "hr", "em", "i", "strong", "b", "u", "s", "sub", "sup", "small",
      "blockquote", "q", "cite", "ul", "ol", "li", "dl", "dt", "dd", "table", "thead", "tbody", "tfoot", "tr", "th",
      "td", "caption", "a", "span", "div", "pre", "code", "abbr",
    };

    private static readonly HashSet<string> AsDiv = new(StringComparer.OrdinalIgnoreCase)
    {
      "section", "article", "aside", "header", "footer", "nav", "main", "figure", "figcaption",
    };

    private static readonly HashSet<string> Void = new(StringComparer.OrdinalIgnoreCase) { "br", "hr" };

    private static readonly HashSet<string> Inline = new(StringComparer.OrdinalIgnoreCase)
    {
      "em", "i", "strong", "b", "u", "s", "sub", "sup", "small", "q", "cite", "a", "span", "code", "abbr",
    };

    private static readonly string[] SafeSchemes = ["http://", "https://", "mailto:"];

    /// <summary>Sanitizes one chapter document.</summary>
    /// <param name="html">The raw XHTML of the document.</param>
    /// <param name="documentPath">The package path of the document, used to resolve images and links.</param>
    /// <param name="findResource">Finds an image by package path.</param>
    public static SanitizedChapter Sanitize(string html, string documentPath, Func<string, EpubResource?> findResource)
    {
      var doc = new HtmlDocument();
      doc.LoadHtml(html);

      var root    = doc.DocumentNode.Descendants("body").FirstOrDefault() ?? doc.DocumentNode;
      var context = new Context(EpubPath.Directory(documentPath), findResource);

      foreach (var child in root.ChildNodes)
        context.Write(child);

      var docTitle = doc.DocumentNode.Descendants("title").Select(t => Clean(t.InnerText)).FirstOrDefault(t => t.Length > 0);
      var text     = Clean(context.Text.ToString());

      return new SanitizedChapter(context.Html.ToString().Trim(), text.Length, context.ImageCount, context.Heading, docTitle);
    }

    private static string Clean(string text) => OpfMetadataReader.Normalize(HtmlEntity.DeEntitize(text));

    private sealed class Context(string directory, Func<string, EpubResource?> findResource)
    {
      public StringBuilder Html       { get; } = new();
      public StringBuilder Text       { get; } = new();
      public int           ImageCount { get; private set; }
      public string?       Heading    { get; private set; }

      public void Write(HtmlNode node)
      {
        switch (node.NodeType)
        {
          case HtmlNodeType.Text:
            var text = HtmlEntity.DeEntitize(node.InnerText);
            Html.Append(WebUtility.HtmlEncode(text));
            Text.Append(text);
            return;

          case HtmlNodeType.Element:
            WriteElement(node);
            return;
        }
      }

      private void WriteElement(HtmlNode node)
      {
        var name = node.Name.ToLowerInvariant();

        if (Dropped.Contains(name))
          return;

        if (name == "img" || name == "svg")
        {
          WriteImage(node);
          return;
        }

        if (Heading == null && name.Length == 2 && name[0] == 'h' && name[1] is >= '1' and <= '6')
        {
          var heading = Clean(node.InnerText);
          Heading = heading.Length > 0 ? heading : null;
        }

        var tag = Kept.Contains(name) ? name : AsDiv.Contains(name) ? "div" : null;

        if (tag != null)
          Html.Append('<').Append(tag).Append(Attributes(node, name)).Append('>');

        if (tag != null && Void.Contains(tag))
          return;

        foreach (var child in node.ChildNodes)
          Write(child);

        if (tag != null)
          Html.Append("</").Append(tag).Append('>');

        if (tag != null && Inline.Contains(tag) == false)
          Text.Append(' ');
      }

      private void WriteImage(HtmlNode node)
      {
        var img = node.Name.Equals("svg", StringComparison.OrdinalIgnoreCase)
          ? node.Descendants("image").FirstOrDefault()
          : node;

        if (img == null)
          return;

        var src  = img.GetAttributeValue("src", null) ?? img.GetAttributeValue("xlink:href", null) ?? img.GetAttributeValue("href", null);
        var alt  = HtmlEntity.DeEntitize(img.GetAttributeValue("alt", null) ?? node.Descendants("title").FirstOrDefault()?.InnerText ?? string.Empty);
        var path = src == null ? null : EpubPath.Resolve(directory, src);

        ImageCount++;
        Html.Append(ImageInliner.Render(path == null ? null : findResource(path), alt));
      }

      private static string Attributes(HtmlNode node, string name)
      {
        var sb = new StringBuilder();

        void Add(string attr, string? value)
        {
          if (string.IsNullOrWhiteSpace(value) == false)
            sb.Append(' ').Append(attr).Append("=\"").Append(WebUtility.HtmlEncode(HtmlEntity.DeEntitize(value))).Append('"');
        }

        foreach (var attr in new[] { "id", "title", "lang", "dir" })
          Add(attr, node.GetAttributeValue(attr, null));

        switch (name)
        {
          case "a":
            Add("href", SafeHref(node.GetAttributeValue("href", null)));
            break;

          case "td":
          case "th":
            Add("colspan", Numeric(node.GetAttributeValue("colspan", null)));
            Add("rowspan", Numeric(node.GetAttributeValue("rowspan", null)));
            break;

          case "ol":
            Add("start", Numeric(node.GetAttributeValue("start", null)));
            break;
        }

        return sb.ToString();
      }

      /// <summary>Keeps web and mail links; a link into the book keeps only its fragment, which works inside a chapter.</summary>
      private static string? SafeHref(string? href)
      {
        if (string.IsNullOrWhiteSpace(href))
          return null;

        href = HtmlEntity.DeEntitize(href).Trim();

        if (SafeSchemes.Any(s => href.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
          return href;

        if (EpubPath.HasScheme(href))
          return null;

        var (_, fragment) = EpubPath.Split(href);
        return fragment == null ? null : "#" + fragment;
      }

      private static string? Numeric(string? value) => int.TryParse(value, out var n) && n > 0 ? n.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
    }
  }
}
