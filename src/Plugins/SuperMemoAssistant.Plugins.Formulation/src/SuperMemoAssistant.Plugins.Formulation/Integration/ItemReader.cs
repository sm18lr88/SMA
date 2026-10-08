// Reads the question and answer components of an item through the SMA registries.
namespace SuperMemoAssistant.Plugins.Formulation.Integration;

using System.Collections.Generic;
using System.Net;
using System.Threading;
using Anotar.Serilog;
using Engine;
using Interop.SuperMemo.Content.Components;
using Interop.SuperMemo.Elements.Models;
using Interop.SuperMemo.Elements.Types;
using Interop.SuperMemo.Registry.Members;
using PluginManager.Remoting;

/// <summary>
///   Reads an item from its component group (<see cref="IElement.ComponentGroup" />). Each HTML or text component points
///   to a text registry member, and its <see cref="Interop.SuperMemo.Content.Components.IComponent.DisplayAt" /> flags tell
///   whether it belongs to the question or to the answer.
/// </summary>
internal static class ItemReader
{
  /// <summary>Returns the item content, or null when <paramref name="element" /> is not an item.</summary>
  public static ItemHtml? Read(IElement? element, CancellationToken cancellationToken)
  {
    if (element is null || element.Deleted || element.Type != ElementType.Item)
      return null;

    var question       = new List<string>();
    var answer         = new List<string>();
    var hasHtmlContent = false;

    foreach (var component in element.ComponentGroup?.Components ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();

      var role = ComponentRoles.Classify(component.DisplayAt);

      if (role == ComponentRole.None)
        continue;

      var html = component switch
      {
        IComponentHtml htmlComponent => ReadText(htmlComponent.Text),
        IComponentText textComponent => ReadText(textComponent.Text) is { } plain ? WebUtility.HtmlEncode(plain) : null,
        _                            => null,
      };

      if (html is null)
        continue;

      hasHtmlContent |= component is IComponentHtml;
      (role == ComponentRole.Question ? question : answer).Add(html);
    }

    return new ItemHtml(question, answer, hasHtmlContent);
  }

  private static string? ReadText(IText? text)
  {
    try
    {
      return text is { Empty: false } ? text.Value : null;
    }
    catch (RemoteException ex)
    {
      // For example an RTF member, which the registry cannot read yet, or a missing HTML file.
      LogTo.Warning(ex, "Formulation: could not read a text registry member.");
      return null;
    }
  }
}
