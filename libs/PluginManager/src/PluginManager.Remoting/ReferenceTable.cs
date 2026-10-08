// Per-connection object identity: exported local objects (with distributed reference counts) and imported proxies.
namespace PluginManager.Remoting;

internal sealed class ReferenceTable : IReferenceCodec
{
  private readonly RpcConnection                 _connection;
  private readonly object                        _lock    = new();
  private readonly Dictionary<long, Export>      _exports = new();
  private readonly Dictionary<object, long>      _exportIds = new(ReferenceEqualityComparer.Instance);
  private readonly Dictionary<long, Import>      _imports = new();
  private long _nextExportId = RpcConnection.RootId;

  public ReferenceTable(RpcConnection connection, object? root)
  {
    _connection = connection;
    if (root is not null)
      Add(RpcConnection.RootId, root, int.MaxValue);
  }

  public bool IsByReference(object value) => value is MarshalByRefObject or RemoteProxy or Delegate;

  public (ValueTag Tag, long Id) Encode(object value)
  {
    if (value is RemoteProxy proxy && ReferenceEquals(proxy.Connection, _connection))
      return (ValueTag.ReturnedObject, proxy.Id);

    if (value is Delegate { Target: RemoteDelegateTarget target } && ReferenceEquals(target.Connection, _connection))
      return (ValueTag.ReturnedObject, target.Id);

    lock (_lock)
    {
      if (_exportIds.TryGetValue(value, out var id))
      {
        var entry = _exports[id];
        if (entry.Count < int.MaxValue) entry.Count++;
        return (ValueTag.RemoteObject, id);
      }

      id = ++_nextExportId;
      Add(id, value, 1);
      return (ValueTag.RemoteObject, id);
    }
  }

  public object Decode(ValueTag tag, long id, string[] interfaceNames, Type declaredType)
  {
    if (tag == ValueTag.ReturnedObject)
      return Local(id);

    lock (_lock)
    {
      if (_imports.TryGetValue(id, out var existing) && existing.Value.TryGetTarget(out var alive))
      {
        existing.Count++;
        return alive;
      }

      var created = Create(id, interfaceNames, declaredType);
      _imports[id] = new Import(new WeakReference<object>(created)) { Count = 1 };
      return created;
    }
  }

  public object Local(long id)
  {
    lock (_lock)
      return _exports.TryGetValue(id, out var entry)
        ? entry.Value
        : throw new RemotingException($"Object {id} is no longer exported by '{_connection.Name}'.");
  }

  public void ReleaseExport(long id, int count)
  {
    lock (_lock)
    {
      if (!_exports.TryGetValue(id, out var entry) || entry.Count == int.MaxValue)
        return;

      entry.Count -= count;
      if (entry.Count > 0)
        return;

      _exports.Remove(id);
      _exportIds.Remove(entry.Value);
    }
  }

  /// <summary>Called when a proxy is finalized: returns how many references to release, or 0 if a newer proxy exists.</summary>
  public int TakeReleasedImport(long id)
  {
    lock (_lock)
    {
      if (!_imports.TryGetValue(id, out var entry) || entry.Value.TryGetTarget(out _))
        return 0;

      _imports.Remove(id);
      return entry.Count;
    }
  }

  public void Clear()
  {
    lock (_lock)
    {
      _exports.Clear();
      _exportIds.Clear();
      _imports.Clear();
    }
  }

  private object Create(long id, string[] interfaceNames, Type declaredType)
  {
    var types = interfaceNames.Select(TryResolve).OfType<Type>().ToList();

    var delegateType = IsConcreteDelegate(declaredType) ? declaredType : types.FirstOrDefault(IsConcreteDelegate);
    if (delegateType is not null)
      return ProxyFactory.CreateDelegate(_connection, id, delegateType);

    return ProxyFactory.CreateProxy(_connection, id, types, declaredType);
  }

  private void Add(long id, object value, int count)
  {
    _exports[id]      = new Export(value) { Count = count };
    _exportIds[value] = id;
  }

  private static Type? TryResolve(string name)
  {
    try { return TypeNames.Resolve(name); }
    catch (RemotingException) { return null; }
  }

  private static bool IsConcreteDelegate(Type t) =>
    typeof(Delegate).IsAssignableFrom(t) && t != typeof(Delegate) && t != typeof(MulticastDelegate);

  private sealed class Export(object value)
  {
    public object Value { get; } = value;
    public int Count { get; set; }
  }

  private sealed class Import(WeakReference<object> value)
  {
    public WeakReference<object> Value { get; } = value;
    public int Count { get; set; }
  }
}
