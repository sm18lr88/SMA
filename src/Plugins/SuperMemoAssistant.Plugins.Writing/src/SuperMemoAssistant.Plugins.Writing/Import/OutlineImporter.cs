using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SuperMemoAssistant.Plugins.Writing.Import
{
  /// <summary>A topic for <see cref="IElementCreator" />.</summary>
  public sealed record NewTopic(string Title, string Html, double Priority, bool IsPart);

  /// <summary>Creates child topics. SuperMemo implements it; tests use an in-memory tree.</summary>
  public interface IElementCreator
  {
    /// <summary>The maximum number of children per element.</summary>
    int ChildLimit { get; }

    /// <summary>The number of children that <paramref name="parentId" /> can still take.</summary>
    int FreeSlots(int parentId);

    /// <summary>Appends the topics as the last children of the parent, in order. Returns their new ids.</summary>
    IReadOnlyList<int> CreateChildren(int parentId, IReadOnlyList<NewTopic> topics);
  }

  /// <summary>The numbers of topics created (outline topics and added part topics), and whether a cancellation stopped the import.</summary>
  public sealed record ImportResult(int TopicsCreated, int PartsCreated, bool Cancelled);

  /// <summary>Creates the topics of an <see cref="ImportPlan" /> level by level, one call per parent.</summary>
  public sealed class OutlineImporter(IElementCreator creator)
  {
    /// <summary>
    ///   Creates the topics under <paramref name="parentId" />. A cancellation stops the import between two parents;
    ///   the topics created until then stay.
    /// </summary>
    public ImportResult Import(int                    parentId,
                               string                 parentTitle,
                               IReadOnlyList<ImportTopic> topics,
                               IProgress<int>?        progress,
                               CancellationToken      cancellationToken)
    {
      var created = 0;
      var parts   = 0;
      var pending = new Queue<(int Id, string Title, IReadOnlyList<ImportTopic> Children, int Capacity)>();
      pending.Enqueue((parentId, parentTitle, topics, creator.FreeSlots(parentId)));

      while (pending.TryDequeue(out var level))
      {
        if (cancellationToken.IsCancellationRequested)
          return new ImportResult(created, parts, true);

        var fitted = ChildLimitPlanner.Fit(level.Children, level.Capacity, creator.ChildLimit, level.Title);
        var ids    = creator.CreateChildren(level.Id, fitted.Select(t => new NewTopic(t.Title, t.Html, t.Priority, t.IsPart)).ToList());

        if (ids.Count != fitted.Count)
          throw new InvalidOperationException($"SuperMemo created {ids.Count} of {fitted.Count} topics under \"{level.Title}\".");

        for (var i = 0; i < fitted.Count; i++)
        {
          var topic = fitted[i];

          if (topic.IsPart)
            parts++;
          else
            created++;

          if (topic.Children.Count > 0)
            pending.Enqueue((ids[i], topic.IsPart ? level.Title : topic.Title, topic.Children, creator.ChildLimit));
        }

        progress?.Report(created);
      }

      return new ImportResult(created, parts, false);
    }
  }
}
