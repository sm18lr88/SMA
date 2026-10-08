using System;
using System.Collections.Generic;
using System.Linq;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Plugins.Writing.Import;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.Writing.SuperMemo
{
  /// <summary>Adds topics with <see cref="ElementBuilder" />. It only appends children; it never changes an element.</summary>
  internal sealed class SuperMemoElementCreator(string documentTitle, string sourceName) : IElementCreator
  {
    public int ChildLimit { get; } = Svc.SM.UI.ElementWdw.LimitChildrenCount;

    public int FreeSlots(int parentId)
    {
      var parent = Svc.SM.Registry.Element[parentId]
        ?? throw new InvalidOperationException($"SuperMemo element {parentId} could not be read.");

      return ChildLimit - parent.ChildrenCount;
    }

    public IReadOnlyList<int> CreateChildren(int parentId, IReadOnlyList<NewTopic> topics)
    {
      var builders = topics.Select(t => Build(parentId, t)).ToArray();
      var accepted = Svc.SM.Registry.Element.Add(out var results, ElemCreationFlags.None, builders);

      if (accepted == false || results == null || results.Count != builders.Length || results.Any(r => r.Success == false || r.ElementId <= 0))
        throw new InvalidOperationException(
          $"SuperMemo did not create all topics under element {parentId}. {results?.GetErrorString()}".TrimEnd());

      return results.Select(r => r.ElementId).ToList();
    }

    private ElementBuilder Build(int parentId, NewTopic topic)
    {
      var builder = string.IsNullOrWhiteSpace(topic.Html)
        ? new ElementBuilder(ElementType.Topic)
        : new ElementBuilder(ElementType.Topic, new TextContent(true, topic.Html));

      builder = builder.WithParent(parentId)
                       .WithTitle(topic.Title)
                       .WithPriority(topic.Priority)
                       .WithReference(r => r.WithTitle(documentTitle).WithSource(sourceName))
                       .DoNotDisplay();

      return topic.IsPart ? builder.WithStatus(ElementStatus.Dismissed) : builder;
    }
  }
}
