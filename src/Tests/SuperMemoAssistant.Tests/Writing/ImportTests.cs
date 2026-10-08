using Xunit;
using SuperMemoAssistant.Plugins.Writing.Import;

namespace SuperMemoAssistant.Tests.Writing;

public sealed class ImportTests
{
  [Fact]
  public void PrioritySequence_AscendsFromTheStart()
  {
    Assert.Equal([30, 30.1, 30.2, 30.3], PrioritySequence.Create(30, 4).Select(p => Math.Round(p, 6)));
  }

  [Fact]
  public void PrioritySequence_CompressesTheStep_SoThatItEndsAtOneHundredAtMost()
  {
    var priorities = PrioritySequence.Create(99, 21);

    Assert.Equal(99, priorities[0]);
    Assert.Equal(100, priorities[^1], 9);
    Assert.All(priorities.Zip(priorities.Skip(1)), pair => Assert.True(pair.Second > pair.First));
  }

  [Fact]
  public void PrioritySequence_HandlesEdgeCases_AndRejectsInvalidStarts()
  {
    Assert.Empty(PrioritySequence.Create(10, 0));
    Assert.Equal([100, 100], PrioritySequence.Create(100, 2));
    Assert.Throws<ArgumentOutOfRangeException>(() => PrioritySequence.Create(-1, 3));
    Assert.Throws<ArgumentOutOfRangeException>(() => PrioritySequence.Create(100.5, 3));
    Assert.Throws<ArgumentOutOfRangeException>(() => PrioritySequence.Create(double.NaN, 3));
  }

  [Fact]
  public void ImportPlan_AssignsPrioritiesInDocumentOrder_AndUsesTheOnlyTopHeadingAsTitle()
  {
    var plan = ImportPlan.Create(OutlineParser.Parse("Intro\n# Essay\n## A\n### A.1\n## B\n"), @"C:\notes\essay.md", 20);

    Assert.Equal("Essay", plan.DocumentTitle);
    Assert.Equal("essay.md", plan.SourceName);
    Assert.Equal(5, plan.TopicCount);
    Assert.Equal([ImportPlan.IntroductionTitle, "Essay"], plan.Topics.Select(t => t.Title));

    var essay = plan.Topics[1];
    Assert.Equal([20, 20.1, 20.2, 20.3, 20.4],
                 new[] { plan.Topics[0], essay, essay.Children[0], essay.Children[0].Children[0], essay.Children[1] }
                   .Select(t => Math.Round(t.Priority, 6)));
  }

  [Fact]
  public void ImportPlan_TitleFallsBackToTheFileName()
  {
    Assert.Equal("notes", ImportPlan.Create(OutlineParser.Parse("# A\n# B\n"), "notes.md", 50).DocumentTitle);
    Assert.Equal("Front", ImportPlan.Create(OutlineParser.Parse("---\ntitle: Front\n---\n# A\n"), "x.md", 50).DocumentTitle);
    Assert.Equal(0, ImportPlan.Create(OutlineParser.Parse("   \n"), "empty.md", 50).TopicCount);
  }

  [Fact]
  public void ChildLimitPlanner_NestsChildrenInParts_WhenThereAreTooMany()
  {
    var topics = Enumerable.Range(1, 7).Select(i => new ImportTopic($"T{i}", "", i)).ToList();

    Assert.Same(topics, ChildLimitPlanner.Fit(topics, 7, 10, "Parent"));

    var parts = ChildLimitPlanner.Fit(topics, 3, 3, "Parent");

    Assert.Equal(["[1] Parent", "[2] Parent", "[3] Parent"], parts.Select(p => p.Title));
    Assert.All(parts, p => Assert.True(p.IsPart));
    Assert.Equal([3, 3, 1], parts.Select(p => p.Children.Count));
    Assert.Equal([1, 4, 7], parts.Select(p => p.Priority));
  }

  [Fact]
  public void ChildLimitPlanner_NestsAgain_AndFailsWithoutAFreeSlot()
  {
    var topics = Enumerable.Range(1, 9).Select(i => new ImportTopic($"T{i}", "", i)).ToList();

    var nested = Assert.Single(ChildLimitPlanner.Fit(topics, 1, 3, "P"));
    Assert.Equal(3, nested.Children.Count);
    Assert.All(nested.Children, p => Assert.Equal(3, p.Children.Count));

    var error = Assert.Throws<InvalidOperationException>(() => ChildLimitPlanner.Fit(topics, 0, 3, "Full"));
    Assert.Contains("\"Full\" cannot take another child", error.Message);
  }

  [Fact]
  public void OutlineImporter_CreatesTopicsLevelByLevel_UnderTheParent()
  {
    var plan    = ImportPlan.Create(OutlineParser.Parse("# A\nbody\n## A.1\n# B\n"), "doc.md", 40);
    var creator = new FakeCreator(limit: 10);

    var result = new OutlineImporter(creator).Import(1, "Root", plan.Topics, null, TestContext.Current.CancellationToken);

    Assert.Equal(new ImportResult(3, 0, false), result);
    Assert.Equal(["1 > A", "1 > B", "2 > A.1"], creator.Created.Select(c => $"{c.Parent} > {c.Topic.Title}"));
    Assert.Equal("<p>body</p>", creator.Created[0].Topic.Html);
  }

  [Fact]
  public void OutlineImporter_RespectsTheChildLimit_WithParts()
  {
    var markdown = string.Concat(Enumerable.Range(1, 5).Select(i => $"# H{i}\n"));
    var plan     = ImportPlan.Create(OutlineParser.Parse(markdown), "doc.md", 40);
    var creator  = new FakeCreator(limit: 2);

    var result = new OutlineImporter(creator).Import(1, "Root", plan.Topics, null, TestContext.Current.CancellationToken);

    // 5 topics in parts of 2 need 3 parts; 3 parts exceed the root limit of 2 and nest in 2 more parts.
    Assert.Equal(new ImportResult(5, 5, false), result);
    Assert.All(creator.ChildCounts.Values, count => Assert.True(count <= 2));
    Assert.Equal(["H1", "H2", "H3", "H4", "H5"], creator.Created.Where(c => c.Topic.IsPart == false).Select(c => c.Topic.Title));
  }

  [Fact]
  public void OutlineImporter_StopsBetweenParents_WhenCancelled()
  {
    var plan    = ImportPlan.Create(OutlineParser.Parse("# A\n## A.1\n"), "doc.md", 40);
    var creator = new FakeCreator(limit: 10);

    using var cancellation = new CancellationTokenSource();
    var progress = new CancelOnReport(cancellation);

    var result = new OutlineImporter(creator).Import(1, "Root", plan.Topics, progress, cancellation.Token);

    Assert.Equal(new ImportResult(1, 0, true), result);
  }

  private sealed class CancelOnReport(CancellationTokenSource cancellation) : IProgress<int>
  {
    public void Report(int value) => cancellation.Cancel();
  }

  private sealed class FakeCreator(int limit) : IElementCreator
  {
    public List<(int Parent, NewTopic Topic)> Created     { get; } = [];
    public Dictionary<int, int>               ChildCounts { get; } = new() { [1] = 0 };

    public int ChildLimit => limit;

    public int FreeSlots(int parentId) => limit - ChildCounts[parentId];

    public IReadOnlyList<int> CreateChildren(int parentId, IReadOnlyList<NewTopic> topics)
    {
      ChildCounts[parentId] += topics.Count;

      return topics.Select(t =>
                   {
                     Created.Add((parentId, t));
                     var id = ChildCounts.Count + 1;
                     ChildCounts[id] = 0;
                     return id;
                   })
                   .ToList();
    }
  }
}
