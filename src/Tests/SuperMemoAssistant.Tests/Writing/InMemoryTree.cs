using SuperMemoAssistant.Plugins.Writing.Export;

namespace SuperMemoAssistant.Tests.Writing;

/// <summary>An in-memory knowledge tree that records how it is read.</summary>
internal sealed class InMemoryTree : ITreeSource
{
  private readonly Dictionary<int, (string Title, NodeKind Kind, string Html, List<int> Children)> _nodes = [];

  public List<int> NodeBatchSizes    { get; } = [];
  public List<int> ContentReadIds    { get; } = [];
  public List<int> ContentBatchSizes { get; } = [];

  public int Add(int? parentId, string title, string html = "", NodeKind kind = NodeKind.Topic)
  {
    var id = _nodes.Count + 1;
    _nodes[id] = (title, kind, html, []);

    if (parentId is { } parent)
      _nodes[parent].Children.Add(id);

    return id;
  }

  public IReadOnlyList<TreeNodeInfo> ReadNodes(IReadOnlyList<int> ids)
  {
    NodeBatchSizes.Add(ids.Count);

    return ids.Select(id => new TreeNodeInfo(id, _nodes[id].Title, _nodes[id].Kind, _nodes[id].Children.ToList())).ToList();
  }

  public IReadOnlyList<string> ReadContents(IReadOnlyList<int> ids)
  {
    ContentBatchSizes.Add(ids.Count);
    ContentReadIds.AddRange(ids);

    return ids.Select(id => _nodes[id].Html).ToList();
  }
}
