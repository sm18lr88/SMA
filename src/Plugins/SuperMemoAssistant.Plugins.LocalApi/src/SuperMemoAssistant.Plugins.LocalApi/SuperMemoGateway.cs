namespace SuperMemoAssistant.Plugins.LocalApi;

using System;
using System.Linq;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
using SuperMemoAssistant.Interop.SuperMemo.Content.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Plugins.LocalApi.Server;
using SuperMemoAssistant.Services;

/// <summary>The SuperMemo operations of the API, through <see cref="Svc.SM" />. Every call is an RPC to the SMA core.</summary>
internal sealed class SuperMemoGateway : ISuperMemoGateway
{
  private static readonly string SmaVersion = typeof(ISuperMemoAssistant).GetAssemblyVersion();

  /// <inheritdoc />
  public ApiStatus GetStatus()
  {
    if (IsRunning() == false)
      return new ApiStatus(SmaVersion, null, false, null);

    var collection = Svc.SM.Collection?.Name;
    var current    = collection == null ? null : CurrentOrNull();

    return new ApiStatus(SmaVersion, collection, true, current);
  }

  /// <inheritdoc />
  public ElementInfo? GetCurrentElement()
  {
    EnsureCollection();
    return CurrentOrNull();
  }

  /// <inheritdoc />
  public ElementInfo? GetElement(int id)
  {
    EnsureCollection();
    return ToInfo(Svc.SM.Registry.Element[id]);
  }

  /// <inheritdoc />
  public int GetRootElementId()
  {
    EnsureCollection();
    return Svc.SM.Registry.Element.Root.Id;
  }

  /// <inheritdoc />
  public int CreateElement(NewElement element)
  {
    EnsureCollection();

    var builder = (element.Kind == ElementKind.Topic
        ? new ElementBuilder(ElementType.Topic, new TextContent(true, element.Html ?? string.Empty))
        : new ElementBuilder(ElementType.Item,
                             new TextContent(true, element.Question ?? string.Empty, AtFlags.All),
                             new TextContent(true, element.Answer ?? string.Empty, AtFlags.NonQuestion)))
      .WithParent(element.ParentId)
      .WithPriority(element.Priority)
      .DoNotDisplay();

    if (element.Title != null)
      builder = builder.WithTitle(element.Title);

    if (element.References is { } refs)
      builder = builder.WithReference(r => r.WithTitle(refs.Title)
                                            .WithAuthor(refs.Author)
                                            .WithLink(refs.Link)
                                            .WithSource(refs.Source)
                                            .WithDate(EscapeFormat(refs.Date))
                                            .WithComment(refs.Comment)
                                            .WithEmail(refs.Email));

    Svc.SM.Registry.Element.Add(out var results, ElemCreationFlags.None, builder);

    var result = results?.FirstOrDefault();
    if (result is not { Success: true, ElementId: > 0 })
      throw new ApiException(500, results?.GetErrorString() ?? "SuperMemo did not create the element.");

    return result.ElementId;
  }

  /// <inheritdoc />
  public bool Navigate(int id)
  {
    EnsureCollection();
    return Svc.SM.UI.ElementWdw.GoToElement(id);
  }

  private static bool IsRunning() => Svc.SM?.UI?.ElementWdw?.IsAvailable ?? false;

  private static void EnsureCollection()
  {
    if (IsRunning() == false)
      throw new ApiException(503, "SuperMemo is not running. Open your collection with SMA and try again.");

    if (Svc.SM.Collection == null)
      throw new ApiException(409, "No collection is open in SuperMemo.");
  }

  private static ElementInfo? CurrentOrNull()
  {
    var id = Svc.SM.UI.ElementWdw.CurrentElementId;
    return id > 0 ? ToInfo(Svc.SM.Registry.Element[id]) : null;
  }

  private static ElementInfo? ToInfo(IElement? element) =>
    element == null || element.Deleted
      ? null
      : new ElementInfo(element.Id, element.Title ?? string.Empty, element.Type.ToString().ToLowerInvariant(), element.Parent?.Id, element.ChildrenCount);

  /// <summary>References format dates with string.Format, so braces in a date text must be escaped.</summary>
  private static string? EscapeFormat(string? text) =>
    text?.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
}
