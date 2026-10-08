// Cross-connection behaviour of the Remoting replacement over real named pipes (both ends in this process).
namespace SuperMemoAssistant.Tests.Remoting;

using PluginManager.Remoting;
using Xunit;

public sealed class RpcTests : IDisposable
{
  private readonly Service   _service = new();
  private readonly RpcServer _server;
  private readonly IService  _proxy;

  public RpcTests()
  {
    var pipe = RpcEndpoint.NewPipeName();
    _server = RpcEndpoint.Serve(pipe, _service);
    _proxy  = RpcEndpoint.Connect<IService>(pipe, TimeSpan.FromSeconds(10));
  }

  public void Dispose() => _server.Dispose();

  [Fact]
  public void SynchronousMethodsAndProperties()
  {
    _proxy.Title = "hello";

    Assert.Equal("hello", _service.Title);
    Assert.Equal("hello", _proxy.Title);
    Assert.Equal(5, _proxy.Add(2, 3));
  }

  [Fact]
  public void ByReferenceObjectsKeepIdentityInBothDirections()
  {
    var first  = _proxy.GetChild("a");
    var second = _proxy.GetChild("a");

    Assert.True(RemoteObjects.IsRemote(first));
    Assert.Same(first, second);
    Assert.Equal("a", first.Name);
    Assert.True(_proxy.IsOriginalChild("a", first));
  }

  [Fact]
  public void ProxiesImplementEveryPublicInterfaceOfTheRemoteObject()
  {
    var child = _proxy.GetChild("b");

    var renamable = Assert.IsAssignableFrom<IRenamable>(child);
    renamable.Rename("c");
    Assert.Equal("c", child.Name);
  }

  [Fact]
  public void EventsDeliverCallbacksAndCanBeRemoved()
  {
    var received = new List<string>();
    Action<string> handler = received.Add;

    _proxy.Changed += handler;
    _proxy.Raise("one");
    _proxy.Changed -= handler;
    _proxy.Raise("two");

    Assert.Equal(["one"], received);
  }

  [Fact]
  public void LocalObjectsPassedToTheServerAreCallable()
  {
    var local = new Child("local");

    Assert.Equal("local!", _proxy.Shout(local));
  }

  [Fact]
  public void SerializableValuesCrossByValue()
  {
    var dto = new Dto { Name = "x", Tags = ["t1", "t2"], Counts = new() { ["k"] = 3 }, Kind = DtoKind.Second };

    var echoed = _proxy.Echo(dto);

    Assert.NotSame(dto, echoed);
    Assert.Equal("x", echoed.Name);
    Assert.Equal(["t1", "t2"], echoed.Tags);
    Assert.Equal(3, echoed.Counts["k"]);
    Assert.Equal(DtoKind.Second, echoed.Kind);
    Assert.Null(echoed.Transient);
  }

  [Fact]
  public void RemoteExceptionsCarryTheOriginalTypeAndMessage()
  {
    var ex = Assert.Throws<RemoteException>(() => _proxy.Fail("boom"));

    Assert.Equal(typeof(InvalidOperationException).FullName, ex.RemoteType);
    Assert.Contains("boom", ex.Message);
  }

  [Fact]
  public void NonSerializableValuesAreRejected()
  {
    Assert.Throws<RemotingException>(() => _proxy.Accept(new NotSerializable()));
  }

  [Fact]
  public void OutParametersAreReturned()
  {
    Assert.True(_proxy.TryLookup("k", out var value));
    Assert.Equal(42, value);
  }

  [Fact]
  public void RunAfterReplyDefersTheActionUntilTheReplyIsSent()
  {
    _proxy.DeferUntilReleased();

    Assert.False(_service.DeferredRan);

    _service.Release.Set();
    Assert.True(_service.DeferredDone.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    Assert.True(_service.DeferredRan);
  }

  [Fact]
  public void RunAfterReplyRunsAtOnceOutsideARemoteCall()
  {
    var ran = false;

    RpcConnection.RunAfterReply(() => ran = true);

    Assert.True(ran);
  }

  [Fact]
  public void CallsFailOnceTheConnectionCloses()
  {
    _server.Dispose();

    Assert.Throws<RemotingException>(() => _proxy.Add(1, 1));
  }

  public interface IService
  {
    string Title { get; set; }

    event Action<string> Changed;

    int Add(int a, int b);

    IChild GetChild(string name);

    bool IsOriginalChild(string name, IChild child);

    string Shout(IChild child);

    Dto Echo(Dto dto);

    void Fail(string message);

    void Accept(object value);

    bool TryLookup(string key, out int value);

    void Raise(string value);

    void DeferUntilReleased();
  }

  public interface IChild
  {
    string Name { get; }
  }

  public interface IRenamable
  {
    void Rename(string name);
  }

  public enum DtoKind { First, Second }

  [Serializable]
  public sealed class Dto
  {
    public string Name { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, int> Counts { get; set; } = [];
    public DtoKind Kind { get; set; }
    [field: NonSerialized] public object? Transient { get; set; } = new();
  }

  public sealed class NotSerializable;

  private sealed class Child(string name) : MarshalByRefObject, IChild, IRenamable
  {
    public string Name { get; private set; } = name;

    public void Rename(string name) => Name = name;
  }

  private sealed class Service : MarshalByRefObject, IService
  {
    private readonly Dictionary<string, Child> _children = [];

    public string Title { get; set; } = "";

    public event Action<string>? Changed;

    public int Add(int a, int b) => a + b;

    public IChild GetChild(string name) => _children.TryGetValue(name, out var c) ? c : _children[name] = new Child(name);

    public bool IsOriginalChild(string name, IChild child) => ReferenceEquals(_children[name], child);

    public string Shout(IChild child) => child.Name + "!";

    public Dto Echo(Dto dto) => dto;

    public void Fail(string message) => throw new InvalidOperationException(message);

    public void Accept(object value) { }

    public bool TryLookup(string key, out int value)
    {
      value = 42;
      return key == "k";
    }

    public void Raise(string value) => Changed?.Invoke(value);

    public ManualResetEventSlim Release      { get; } = new();
    public ManualResetEventSlim DeferredDone { get; } = new();
    public volatile bool        DeferredRan;

    // If the action ran before the reply, the call would block on Release and DeferredRan would be true on return.
    public void DeferUntilReleased() => RpcConnection.RunAfterReply(() =>
    {
      Release.Wait(TimeSpan.FromSeconds(10));
      DeferredRan = true;
      DeferredDone.Set();
    });
  }
}
