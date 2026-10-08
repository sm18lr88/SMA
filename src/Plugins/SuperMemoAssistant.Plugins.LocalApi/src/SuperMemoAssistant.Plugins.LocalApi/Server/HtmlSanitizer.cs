namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

/// <summary>
///   Removes active content from HTML that a client sends, and keeps the rest of the markup. SuperMemo shows HTML in an
///   Internet Explorer control, so script elements, event handlers, script URLs, and CSS expressions can run code.
/// </summary>
public static class HtmlSanitizer
{
  private static readonly HashSet<string> DroppedElements = new(StringComparer.OrdinalIgnoreCase)
  {
    "script", "iframe", "frame", "frameset", "object", "embed", "applet", "form", "base", "meta", "link",
  };

  private static readonly HashSet<string> UrlAttributes = new(StringComparer.OrdinalIgnoreCase)
  {
    "href", "src", "action", "formaction", "background", "dynsrc", "lowsrc", "poster", "data", "codebase", "cite",
    "longdesc", "usemap", "xlink:href",
  };

  private static readonly string[] ScriptSchemes = ["javascript:", "vbscript:", "livescript:"];

  private static readonly string[] ActiveCss = ["expression", "javascript:", "vbscript:", "behavior", "-moz-binding", "@import"];

  private static readonly Regex CssComment = new(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

  /// <summary>Returns <paramref name="html" /> without active content.</summary>
  public static string Sanitize(string html)
  {
    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    foreach (var node in doc.DocumentNode.Descendants().Where(n => n.NodeType == HtmlNodeType.Element).ToList())
    {
      if (DroppedElements.Contains(node.Name))
      {
        node.Remove();
        continue;
      }

      foreach (var attribute in node.Attributes.ToList())
        if (IsActive(attribute))
          attribute.Remove();
    }

    return doc.DocumentNode.OuterHtml;
  }

  private static bool IsActive(HtmlAttribute attribute)
  {
    var name = attribute.Name;

    if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) || name.Equals("srcdoc", StringComparison.OrdinalIgnoreCase))
      return true;

    var value = Compact(HtmlEntity.DeEntitize(attribute.Value ?? string.Empty));

    if (UrlAttributes.Contains(name))
      return ScriptSchemes.Any(s => value.StartsWith(s, StringComparison.OrdinalIgnoreCase));

    if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
    {
      var css = CssComment.Replace(value, string.Empty);
      return ActiveCss.Any(s => css.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    return false;
  }

  /// <summary>
  ///   Browsers ignore white space and control characters inside a URL scheme, for example "java&#9;script:". CSS
  ///   ignores a backslash before a letter, for example "expr\ession".
  /// </summary>
  private static string Compact(string value)
  {
    var sb = new StringBuilder(value.Length);

    foreach (var c in value)
      if (c > ' ' && c != '\\')
        sb.Append(c);

    return sb.ToString();
  }
}
