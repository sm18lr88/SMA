using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace SuperMemoAssistant.Plugins.Writing.Html
{
  /// <summary>The content of an element without its reference block, and the references themselves.</summary>
  public sealed record CleanHtml(string Body, IReadOnlyList<KeyValuePair<string, string>> References);

  /// <summary>
  ///   Turns the Internet Explorer HTML that SuperMemo stores (upper-case tags, unclosed P and LI, FONT, styled SPAN,
  ///   IE attributes) into small, well-formed semantic HTML, and separates the SuperMemo reference block. Parsing
  ///   follows the HTML5 algorithm, so implicitly closed elements are closed as a browser would close them.
  /// </summary>
  public static partial class SuperMemoHtml
  {
    private static readonly HashSet<string> RemovedElements = ["script", "style", "title", "meta", "link", "xml", "o:p"];

    private static readonly Dictionary<string, string[]> KeptAttributes = new()
    {
      ["a"]   = ["href", "title"],
      ["img"] = ["src", "alt", "title"],
      ["td"]  = ["colspan", "rowspan"],
      ["th"]  = ["colspan", "rowspan"],
      ["ol"]  = ["start"],
    };

    /// <summary>Cleans one element's HTML.</summary>
    /// <param name="html">The HTML as SuperMemo stores it.</param>
    /// <param name="rewriteImageSource">Optional: maps each image source to the source to write.</param>
    public static CleanHtml Clean(string html, Func<string, string>? rewriteImageSource = null)
    {
      var (body, referenceHtml) = SplitReferences(html);
      var document = new HtmlParser().ParseDocument(body);

      if (document.Body is not { } root)
        return new CleanHtml(string.Empty, ParseReferences(referenceHtml));

      foreach (var node in root.Descendants().Where(IsRemoved).ToList())
        node.RemoveFromParent();

      foreach (var element in root.Descendants<IElement>().Where(e => e.LocalName is "span" or "font").Reverse().ToList())
        Unwrap(element);

      foreach (var element in root.Descendants<IElement>())
      {
        KeepOnlyKnownAttributes(element);

        if (rewriteImageSource != null && element.LocalName == "img" && element.GetAttribute("src") is { } src)
          element.SetAttribute("src", rewriteImageSource(src));
      }

      return new CleanHtml(root.InnerHtml.Trim(), ParseReferences(referenceHtml));
    }

    private static (string Body, string References) SplitReferences(string html)
    {
      var marker = ReferenceMarker().Match(html);

      if (marker.Success == false)
        return (html, string.Empty);

      var body = TrailingBreaks().Replace(html[..marker.Index], string.Empty);

      return (body, html[(marker.Index + marker.Length)..]);
    }

    private static List<KeyValuePair<string, string>> ParseReferences(string referenceHtml)
    {
      if (string.IsNullOrWhiteSpace(referenceHtml))
        return [];

      var text = new HtmlParser().ParseDocument(LineBreak().Replace(referenceHtml, "\n")).Body?.TextContent ?? string.Empty;

      return text.Split('\n', StringSplitOptions.TrimEntries)
                 .Where(line => line.StartsWith('#'))
                 .Select(line => line[1..].Split(':', 2, StringSplitOptions.TrimEntries))
                 .Where(parts => parts.Length == 2 && parts[1].Length > 0)
                 .Select(parts => KeyValuePair.Create(parts[0], parts[1]))
                 .ToList();
    }

    private static bool IsRemoved(INode node)
    {
      return node is IComment || (node is IElement element && RemovedElements.Contains(element.LocalName));
    }

    /// <summary>Replaces a FONT or SPAN by its children, keeping the emphasis that its inline style expresses.</summary>
    private static void Unwrap(IElement element)
    {
      if (element.Parent is not { } parent)
        return;

      IElement? outer = null;
      IElement? inner = null;

      foreach (var tag in EmphasisTags(element.GetAttribute("style") ?? string.Empty))
      {
        var wrapper = element.Owner?.CreateElement(tag)
          ?? throw new InvalidOperationException("An HTML element has no owner document.");

        if (inner == null)
          outer = wrapper;
        else
          inner.AppendChild(wrapper);

        inner = wrapper;
      }

      foreach (var child in element.ChildNodes.ToList())
      {
        if (inner == null)
          parent.InsertBefore(child, element);
        else
          inner.AppendChild(child);
      }

      if (outer != null)
        parent.InsertBefore(outer, element);

      element.RemoveFromParent();
    }

    private static IEnumerable<string> EmphasisTags(string style)
    {
      if (BoldStyle().IsMatch(style))
        yield return "strong";

      if (ItalicStyle().IsMatch(style))
        yield return "em";

      if (StrikeStyle().IsMatch(style))
        yield return "del";
    }

    private static void KeepOnlyKnownAttributes(IElement element)
    {
      var kept = KeptAttributes.GetValueOrDefault(element.LocalName, []);

      foreach (var name in element.Attributes.Select(a => a.Name).Where(n => kept.Contains(n) == false).ToList())
        element.RemoveAttribute(name);
    }

    [GeneratedRegex(@"<hr\b[^>]*\bsupermemo\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ReferenceMarker();

    [GeneratedRegex(@"(\s*<br\s*/?>\s*)+$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingBreaks();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreak();

    [GeneratedRegex(@"font-weight\s*:\s*(bold|bolder|[6-9]00)", RegexOptions.IgnoreCase)]
    private static partial Regex BoldStyle();

    [GeneratedRegex(@"font-style\s*:\s*(italic|oblique)", RegexOptions.IgnoreCase)]
    private static partial Regex ItalicStyle();

    [GeneratedRegex(@"text-decoration[^;]*line-through", RegexOptions.IgnoreCase)]
    private static partial Regex StrikeStyle();
  }
}
