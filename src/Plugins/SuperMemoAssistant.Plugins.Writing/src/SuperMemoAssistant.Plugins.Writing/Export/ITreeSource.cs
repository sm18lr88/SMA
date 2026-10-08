using System.Collections.Generic;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>The kinds of elements that compilation treats differently.</summary>
  public enum NodeKind
  {
    /// <summary>A topic, task, or concept group: always a section.</summary>
    Topic,

    /// <summary>An item (question and answer): a section only when items are included.</summary>
    Item,
  }

  /// <summary>The structure of one element: identity, title, kind, and children in tree order.</summary>
  public sealed record TreeNodeInfo(int Id, string Title, NodeKind Kind, IReadOnlyList<int> ChildIds);

  /// <summary>Read-only access to a knowledge tree. SuperMemo implements it; tests use an in-memory tree.</summary>
  public interface ITreeSource
  {
    /// <summary>Reads the structure of a batch of elements. Returns one entry per id, in the same order.</summary>
    IReadOnlyList<TreeNodeInfo> ReadNodes(IReadOnlyList<int> ids);

    /// <summary>Reads the HTML content of a batch of elements. Returns one entry per id, in the same order.</summary>
    IReadOnlyList<string> ReadContents(IReadOnlyList<int> ids);
  }
}
