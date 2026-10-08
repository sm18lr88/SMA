namespace SuperMemoAssistant.Plugins.Books.Planning
{
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>The SuperMemo references of a planned element.</summary>
  public sealed record ReferenceInfo(string? Title, string? Author, string? Source, string? Date);

  /// <summary>Options that apply to every planned tree.</summary>
  /// <param name="Priority">The priority of the top topic, 0..100.</param>
  /// <param name="PriorityStep">How much lower each further child is, in percentage points.</param>
  /// <param name="ChildrenLimit">The largest number of children that one SuperMemo element can have.</param>
  public sealed record PlanOptions(double Priority, double PriorityStep, int ChildrenLimit);

  /// <summary>One planned topic. The planner builds the tree; the importer creates it in SuperMemo.</summary>
  public sealed class ImportNode
  {
    /// <summary>Creates a node.</summary>
    public ImportNode(string title, string html, double priority, ReferenceInfo references)
    {
      Title      = title;
      Html       = html;
      Priority   = priority;
      References = references;
    }

    /// <summary>The element title.</summary>
    public string Title { get; }

    /// <summary>The safe HTML content.</summary>
    public string Html { get; }

    /// <summary>The SuperMemo priority, 0..100 (a smaller value is a higher priority).</summary>
    public double Priority { get; }

    /// <summary>The element references.</summary>
    public ReferenceInfo References { get; }

    /// <summary>The children, in creation order.</summary>
    public List<ImportNode> Children { get; } = [];

    /// <summary>Kindle clipping hashes to record when this element is created.</summary>
    public List<string> HighlightHashes { get; } = [];

    /// <summary>The number of elements in this subtree, this node included.</summary>
    public int Count => 1 + Children.Sum(c => c.Count);

    /// <summary>All nodes of this subtree in creation order (a parent before its children).</summary>
    public IEnumerable<ImportNode> DescendantsAndSelf()
    {
      yield return this;

      foreach (var node in Children.SelectMany(c => c.DescendantsAndSelf()))
        yield return node;
    }
  }
}
