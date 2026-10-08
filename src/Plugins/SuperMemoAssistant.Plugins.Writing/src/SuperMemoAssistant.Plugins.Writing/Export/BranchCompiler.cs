using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>One element of the compiled branch. The root has depth 0.</summary>
  public sealed record Section(int Id, int Depth, string Title, NodeKind Kind, string Html);

  /// <summary>A branch in tree order (pre-order), ready to render.</summary>
  public sealed record BranchDocument(string Title, IReadOnlyList<Section> Sections);

  /// <summary>The two stages of a compilation, for progress reports.</summary>
  public enum CompileStage
  {
    ReadingTree,
    ReadingContent,
  }

  /// <summary>Progress of a compilation. <see cref="Total" /> is unknown while the tree is read.</summary>
  public sealed record CompileProgress(CompileStage Stage, int Done, int? Total);

  /// <summary>Walks a branch of a <see cref="ITreeSource" /> in batches and collects its sections.</summary>
  public sealed class BranchCompiler(ITreeSource source)
  {
    /// <summary>
    ///   Reads the branch under <paramref name="rootId" />. Skipped branches and excluded items are left out with their
    ///   whole subtree. The root is always included.
    /// </summary>
    public BranchDocument Compile(int                         rootId,
                                  CompileOptions              options,
                                  IProgress<CompileProgress>? progress,
                                  CancellationToken           cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(options);
      ArgumentOutOfRangeException.ThrowIfLessThan(options.BatchSize, 1, nameof(options));

      var (nodes, depths) = ReadStructure(rootId, options, progress, cancellationToken);
      var order           = PreOrder(rootId, nodes);
      var contents        = ReadContents(order, options.BatchSize, progress, cancellationToken);

      var sections = order.Select((id, i) => new Section(id, depths[id], nodes[id].Title, nodes[id].Kind, contents[i]))
                          .ToList();

      return new BranchDocument(nodes[rootId].Title, sections);
    }

    private (Dictionary<int, TreeNodeInfo> Nodes, Dictionary<int, int> Depths) ReadStructure(
      int                         rootId,
      CompileOptions              options,
      IProgress<CompileProgress>? progress,
      CancellationToken           cancellationToken)
    {
      var nodes    = new Dictionary<int, TreeNodeInfo>();
      var depths   = new Dictionary<int, int> { [rootId] = 0 };
      var frontier = new List<int> { rootId };
      var read     = 0;

      while (frontier.Count > 0)
      {
        var next = new List<int>();

        foreach (var batch in frontier.Chunk(options.BatchSize))
        {
          cancellationToken.ThrowIfCancellationRequested();

          foreach (var node in ReadBatch(batch, source.ReadNodes(batch)))
          {
            if (node.Id != rootId && IsExcluded(node, options))
              continue;

            nodes[node.Id] = node;

            // An element listed twice (a malformed tree) is read and placed once.
            foreach (var childId in node.ChildIds.Where(c => depths.TryAdd(c, depths[node.Id] + 1)))
              next.Add(childId);
          }

          read += batch.Length;
          progress?.Report(new CompileProgress(CompileStage.ReadingTree, read, null));
        }

        frontier = next;
      }

      return (nodes, depths);
    }

    private List<string> ReadContents(IReadOnlyList<int>          order,
                                      int                         batchSize,
                                      IProgress<CompileProgress>? progress,
                                      CancellationToken           cancellationToken)
    {
      var contents = new List<string>(order.Count);

      foreach (var batch in order.Chunk(batchSize))
      {
        cancellationToken.ThrowIfCancellationRequested();

        contents.AddRange(ReadBatch(batch, source.ReadContents(batch)));
        progress?.Report(new CompileProgress(CompileStage.ReadingContent, contents.Count, order.Count));
      }

      return contents;
    }

    private static List<int> PreOrder(int rootId, Dictionary<int, TreeNodeInfo> nodes)
    {
      var order   = new List<int>(nodes.Count);
      var visited = new HashSet<int>();
      var stack   = new Stack<int>();
      stack.Push(rootId);

      while (stack.TryPop(out var id))
      {
        if (visited.Add(id) == false)
          continue;

        order.Add(id);

        foreach (var childId in nodes[id].ChildIds.Where(nodes.ContainsKey).Reverse())
          stack.Push(childId);
      }

      return order;
    }

    private static bool IsExcluded(TreeNodeInfo node, CompileOptions options)
    {
      return (node.Kind == NodeKind.Item && options.IncludeItems == false) || options.IsSkippedTitle(node.Title);
    }

    private static IReadOnlyList<T> ReadBatch<T>(int[] batch, IReadOnlyList<T> result)
    {
      if (result.Count != batch.Length)
        throw new InvalidOperationException(
          $"The knowledge tree returned {result.Count} results for {batch.Length} elements (first element: {batch[0]}).");

      return result;
    }
  }
}
