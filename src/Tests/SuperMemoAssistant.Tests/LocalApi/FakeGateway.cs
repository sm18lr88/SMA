// A SuperMemo stand-in for the Local API tests: a few elements in memory, and a record of what the API asked for.
namespace SuperMemoAssistant.Tests.LocalApi;

using SuperMemoAssistant.Plugins.LocalApi.Server;

internal sealed class FakeGateway : ISuperMemoGateway
{
  private readonly Lock _lock = new();
  private          int  _nextId = 100;
  private          int  _running;

  public Dictionary<int, ElementInfo> Elements { get; } = new()
  {
    [1] = new ElementInfo(1, "Root", "conceptgroup", null, 1),
    [5] = new ElementInfo(5, "Current topic", "topic", 1, 0),
  };

  public int? CurrentId { get; set; } = 5;

  public List<NewElement> Created { get; } = [];

  public List<int> Navigated { get; } = [];

  /// <summary>How long CreateElement blocks, to simulate a busy SuperMemo.</summary>
  public TimeSpan CreateDelay { get; set; } = TimeSpan.Zero;

  /// <summary>The largest number of CreateElement calls that ran at the same time.</summary>
  public int MaxConcurrentCreates { get; private set; }

  public ApiStatus GetStatus() => new("3.1.0-test", "Test collection", true, GetCurrentElement());

  public ElementInfo? GetCurrentElement()
  {
    lock (_lock)
      return CurrentId is { } id ? Elements.GetValueOrDefault(id) : null;
  }

  public ElementInfo? GetElement(int id)
  {
    lock (_lock)
      return Elements.GetValueOrDefault(id);
  }

  public int GetRootElementId() => 1;

  public int CreateElement(NewElement element)
  {
    var running = Interlocked.Increment(ref _running);
    lock (_lock)
      MaxConcurrentCreates = Math.Max(MaxConcurrentCreates, running);

    Thread.Sleep(CreateDelay);
    Interlocked.Decrement(ref _running);

    lock (_lock)
    {
      var id = _nextId++;
      Created.Add(element);
      Elements[id] = new ElementInfo(id, element.Title ?? string.Empty, element.Kind.ToString().ToLowerInvariant(), element.ParentId, 0);
      return id;
    }
  }

  public bool Navigate(int id)
  {
    lock (_lock)
      Navigated.Add(id);

    return true;
  }
}
