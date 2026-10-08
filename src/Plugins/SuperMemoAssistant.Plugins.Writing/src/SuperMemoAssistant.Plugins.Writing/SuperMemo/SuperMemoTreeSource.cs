using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using SuperMemoAssistant.Interop.SuperMemo.Content.Components;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Plugins.Writing.Export;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.Writing.SuperMemo
{
  /// <summary>Reads the SuperMemo knowledge tree through the element and component registries. It never writes.</summary>
  internal sealed class SuperMemoTreeSource : ITreeSource
  {
    public IReadOnlyList<TreeNodeInfo> ReadNodes(IReadOnlyList<int> ids)
    {
      return ids.Select(id =>
                {
                  var element = Get(id);

                  return new TreeNodeInfo(id,
                                          element.Title ?? string.Empty,
                                          element.Type == ElementType.Item ? NodeKind.Item : NodeKind.Topic,
                                          element.Children.Select(c => c.Id).ToList());
                })
                .ToList();
    }

    public IReadOnlyList<string> ReadContents(IReadOnlyList<int> ids)
    {
      return ids.Select(id => ContentHtml(Get(id))).ToList();
    }

    /// <summary>Joins the HTML, plain text, and image components of an element, in component order.</summary>
    private static string ContentHtml(IElement element)
    {
      var html = new StringBuilder();

      foreach (var component in element.ComponentGroup?.Components ?? [])
        switch (component)
        {
          case IComponentHtml web:
            html.Append(web.Text?.Value).Append('\n');
            break;

          case IComponentText text:
            html.Append("<p>").Append(WebUtility.HtmlEncode(text.Text?.Value)).Append("</p>\n");
            break;

          case IComponentImage image when image.Image?.GetFilePath() is { Length: > 0 } path:
            html.Append("<p><img src=\"").Append(WebUtility.HtmlEncode(new Uri(path).AbsoluteUri))
                .Append("\" alt=\"").Append(WebUtility.HtmlEncode(image.Image.Name)).Append("\"></p>\n");
            break;
        }

      return html.ToString();
    }

    private static IElement Get(int id)
    {
      return Svc.SM.Registry.Element[id]
        ?? throw new InvalidOperationException($"SuperMemo element {id} could not be read. Was it deleted during the compilation?");
    }
  }
}
