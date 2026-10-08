namespace SuperMemoAssistant.Tests.PDF;

using SuperMemoAssistant.Plugins.PDF.Extracts;
using Xunit;

public sealed class SectionPlannerTests
{
  // Book (30 pages): Preface p0; Part I p2 (1.1 p2, 1.2 p6 (1.2.1 p7)); Part II p12 (2.1 p12, 2.2 p20); Index p28.
  private readonly OutlineNode _preface = new("Preface", 0);
  private readonly OutlineNode _s11     = new("1.1", 2);
  private readonly OutlineNode _s121    = new("1.2.1", 7);
  private readonly OutlineNode _s12;
  private readonly OutlineNode _part1;
  private readonly OutlineNode _s21 = new("2.1", 12);
  private readonly OutlineNode _s22 = new("2.2", 20);
  private readonly OutlineNode _part2;
  private readonly OutlineNode _index = new("Index", 28);
  private readonly OutlineTree _tree;

  public SectionPlannerTests()
  {
    _s12   = new OutlineNode("1.2", 6, [_s121]);
    _part1 = new OutlineNode("Part I", 2, [_s11, _s12]);
    _part2 = new OutlineNode("Part II", 12, [_s21, _s22]);
    _tree  = new OutlineTree([_preface, _part1, _part2, _index], 30);
  }

  [Fact]
  public void ASectionEndsBeforeTheNextSectionAtTheSameOrAHigherLevel()
  {
    Assert.Equal(new PageRange(0, 1), _tree.GetRange(_preface));
    Assert.Equal(new PageRange(2, 11), _tree.GetRange(_part1));
    Assert.Equal(new PageRange(2, 5), _tree.GetRange(_s11));
    Assert.Equal(new PageRange(6, 11), _tree.GetRange(_s12));
    Assert.Equal(new PageRange(7, 11), _tree.GetRange(_s121));
    Assert.Equal(new PageRange(20, 27), _tree.GetRange(_s22));
  }

  [Fact]
  public void TheLastSectionEndsAtTheEndOfTheDocument()
  {
    Assert.Equal(new PageRange(28, 29), _tree.GetRange(_index));
  }

  [Fact]
  public void ASectionFollowedOnTheSamePageIsOnePage()
  {
    var a    = new OutlineNode("A", 3);
    var b    = new OutlineNode("B", 3);
    var tree = new OutlineTree([a, b], 5);

    Assert.Equal(new PageRange(3, 3), tree.GetRange(a));
    Assert.Equal(new PageRange(3, 4), tree.GetRange(b));
  }

  [Fact]
  public void EntriesWithoutAValidPageNeitherGetARangeNorEndASection()
  {
    var a       = new OutlineNode("A", 1);
    var noDest  = new OutlineNode("External link", null);
    var invalid = new OutlineNode("Broken", 99);
    var before  = new OutlineNode("Out of order", 0);
    var c       = new OutlineNode("C", 6);
    var tree    = new OutlineTree([a, noDest, invalid, before, c], 10);

    Assert.Equal(new PageRange(1, 5), tree.GetRange(a));
    Assert.Null(tree.GetRange(noDest));
    Assert.Null(tree.GetRange(invalid));
  }

  [Fact]
  public void ADeeperEntryOfALaterSectionEndsASectionWhenItsParentHasNoPage()
  {
    var a      = new OutlineNode("A", 0);
    var bChild = new OutlineNode("B.1", 4);
    var b      = new OutlineNode("B", null, [bChild]);
    var tree   = new OutlineTree([a, b], 8);

    Assert.Equal(new PageRange(0, 3), tree.GetRange(a));
  }

  [Fact]
  public void FindDeepestContainingReturnsTheDeepestSection()
  {
    Assert.Same(_s121, _tree.FindDeepestContaining(8));
    Assert.Same(_s12, _tree.FindDeepestContaining(6));
    Assert.Same(_preface, _tree.FindDeepestContaining(1));
    Assert.Null(new OutlineTree([new OutlineNode("Late", 5)], 10).FindDeepestContaining(2));
  }

  [Fact]
  public void EachSubsectionListsTheDirectChildrenInOrder()
  {
    Assert.Equal([_s11, _s12], SectionPlanner.EachSubsection(_tree, _part1));
    Assert.Empty(SectionPlanner.EachSubsection(_tree, _index));
  }

  [Fact]
  public void ThisLevelListsTheEntryAndItsSiblings()
  {
    Assert.Equal([_preface, _part1, _part2, _index], SectionPlanner.ThisLevel(_tree, _part2));
    Assert.Equal([_s21, _s22], SectionPlanner.ThisLevel(_tree, _s22));
    Assert.Equal([_s121], SectionPlanner.ThisLevel(_tree, _s121));
  }

  [Fact]
  public void PlanCoversWholePagesAndSkipsExistingExtracts()
  {
    var existing = new[] { new TextRange(2, 0, 11, 1100), new TextRange(12, 0, 19, 50) };

    var plan = SectionPlanner.Plan(_tree, SectionPlanner.ThisLevel(_tree, _preface), page => page * 100, existing);

    Assert.Equal(
      [
        new PlannedSection(_preface, new TextRange(0, 0, 1, 100), false),
        new PlannedSection(_part1, new TextRange(2, 0, 11, 1100), true),
        new PlannedSection(_part2, new TextRange(12, 0, 27, 2700), false),
        new PlannedSection(_index, new TextRange(28, 0, 29, 2900), false),
      ],
      plan);
  }

  [Fact]
  public void PlanLeavesOutSectionsWithoutAPage()
  {
    var a    = new OutlineNode("A", 0);
    var link = new OutlineNode("Link", null);
    var tree = new OutlineTree([a, link], 3);

    var plan = SectionPlanner.Plan(tree, tree.Roots, _ => 10, []);

    Assert.Equal([new PlannedSection(a, new TextRange(0, 0, 2, 10), false)], plan);
    Assert.Equal([a], SectionPlanner.ThisLevel(tree, link));
  }
}
