using Xunit;
using SuperMemoAssistant.Plugins.Writing.Export;

namespace SuperMemoAssistant.Tests.Writing;

public sealed class BranchCompilerTests
{
  private static BranchDocument Compile(InMemoryTree tree, int rootId, CompileOptions? options = null)
  {
    return new BranchCompiler(tree).Compile(rootId, options ?? new CompileOptions(), null, TestContext.Current.CancellationToken);
  }

  [Fact]
  public void Compile_WalksTheSubtreeInTreeOrder_WithDepths()
  {
    var tree    = new InMemoryTree();
    var root    = tree.Add(null, "Article");
    var first   = tree.Add(root, "First");
    tree.Add(first, "First.1");
    tree.Add(first, "First.2");
    var second  = tree.Add(root, "Second");
    tree.Add(second, "Second.1");

    var document = Compile(tree, root);

    Assert.Equal("Article", document.Title);
    Assert.Equal(["Article", "First", "First.1", "First.2", "Second", "Second.1"], document.Sections.Select(s => s.Title));
    Assert.Equal([0, 1, 2, 2, 1, 2], document.Sections.Select(s => s.Depth));
  }

  [Fact]
  public void Compile_SkipsToDoBranches_AndDoesNotReadTheirContent()
  {
    var tree = new InMemoryTree();
    var root = tree.Add(null, "Article");
    tree.Add(root, "Body", "<P>text</P>");
    var todo = tree.Add(root, " to-do ");
    var idea = tree.Add(todo, "Idea");
    var other = tree.Add(root, "TODO");

    var document = Compile(tree, root);

    Assert.Equal(["Article", "Body"], document.Sections.Select(s => s.Title));
    Assert.DoesNotContain(todo, tree.ContentReadIds);
    Assert.DoesNotContain(idea, tree.ContentReadIds);
    Assert.DoesNotContain(other, tree.ContentReadIds);
  }

  [Fact]
  public void Compile_UsesTheConfiguredSkipList()
  {
    var tree = new InMemoryTree();
    var root = tree.Add(null, "Article");
    tree.Add(root, "TODO");
    tree.Add(root, "Notes");

    var options  = new CompileOptions { SkippedTitles = CompileOptions.ParseTitleList("Notes, Drafts\nOld") };
    var document = Compile(tree, root, options);

    Assert.Equal(["Article", "TODO"], document.Sections.Select(s => s.Title));
  }

  [Fact]
  public void Compile_ExcludesItemsByDefault_AndIncludesThemOnRequest()
  {
    var tree  = new InMemoryTree();
    var root  = tree.Add(null, "Article");
    var topic = tree.Add(root, "Topic");
    tree.Add(topic, "What is IW?", "<P>Q</P><P>A</P>", NodeKind.Item);

    var without = Compile(tree, root);
    var with    = Compile(tree, root, new CompileOptions { IncludeItems = true });

    Assert.Equal(["Article", "Topic"], without.Sections.Select(s => s.Title));
    Assert.Equal(["Article", "Topic", "What is IW?"], with.Sections.Select(s => s.Title));
    Assert.Equal(NodeKind.Item, with.Sections[2].Kind);
  }

  [Fact]
  public void Compile_KeepsTheRoot_EvenWhenItsTitleIsSkipped()
  {
    var tree = new InMemoryTree();
    var root = tree.Add(null, "TODO");
    tree.Add(root, "Task");

    Assert.Equal(["TODO", "Task"], Compile(tree, root).Sections.Select(s => s.Title));
  }

  [Fact]
  public void Compile_ReadsInBatches_AndReportsProgress()
  {
    var tree = new InMemoryTree();
    var root = tree.Add(null, "Root");

    for (var i = 0; i < 7; i++)
      tree.Add(root, $"Child {i}");

    var reports  = new List<CompileProgress>();
    var progress = new SynchronousProgress<CompileProgress>(reports.Add);
    var document = new BranchCompiler(tree).Compile(root, new CompileOptions { BatchSize = 3 }, progress, TestContext.Current.CancellationToken);

    Assert.Equal(8, document.Sections.Count);
    Assert.Equal([1, 3, 3, 1], tree.NodeBatchSizes);
    Assert.Equal([3, 3, 2], tree.ContentBatchSizes);
    Assert.Equal(new CompileProgress(CompileStage.ReadingContent, 8, 8), reports[^1]);
    Assert.Contains(reports, r => r is { Stage: CompileStage.ReadingTree, Total: null });
  }

  [Fact]
  public void Compile_StopsWhenCancelled()
  {
    var tree = new InMemoryTree();
    var root = tree.Add(null, "Root");
    tree.Add(root, "Child");

    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    Assert.Throws<OperationCanceledException>(
      () => new BranchCompiler(tree).Compile(root, new CompileOptions(), null, cancellation.Token));
    Assert.Empty(tree.NodeBatchSizes);
  }

  private sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
  {
    public void Report(T value) => report(value);
  }
}
