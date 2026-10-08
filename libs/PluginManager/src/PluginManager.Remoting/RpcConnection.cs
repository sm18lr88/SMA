// One duplex pipe between two processes: issues synchronous calls, serves incoming calls, tracks object references.
namespace PluginManager.Remoting;

using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

public sealed partial class RpcConnection : IDisposable
{
  internal const long RootId = 0;

  private readonly PipeStream _stream;
  private readonly object     _writeLock = new();
  private readonly ConcurrentDictionary<long, PendingCall> _pending = new();
  private readonly ReferenceTable _references;
  private long _nextCallId;
  private int  _closed;

  internal RpcConnection(PipeStream stream, object? root, string name)
  {
    _stream     = stream;
    Name        = name;
    _references = new ReferenceTable(this, root);

    new Thread(ReadLoop) { IsBackground = true, Name = $"RPC reader {name}" }.Start();
  }

  public string Name { get; }

  public bool IsConnected => Volatile.Read(ref _closed) == 0;

  /// <summary>Raised once when the pipe closes, for any reason.</summary>
  public event EventHandler? Closed;

  /// <summary>Returns a proxy for the remote root object.</summary>
  public T GetRoot<T>() =>
    (T)_references.Decode(ValueTag.RemoteObject, RootId, [], typeof(T));

  public void Dispose() => Close(null);

  internal object? Invoke(long objectId, MethodInfo method, object?[] args) =>
    Call(objectId, MethodKeys.Encode(method), method.IsGenericMethod ? method.GetGenericArguments() : [], args,
         method.ReturnType, method.GetParameters());

  internal object? InvokeDelegate(long objectId, Type delegateType, object?[] args)
  {
    var invoke = delegateType.GetMethod("Invoke")!;
    return Call(objectId, MethodKeys.DelegateInvoke, [], args, invoke.ReturnType, invoke.GetParameters());
  }

  internal void ReleaseImport(long id)
  {
    var count = _references.TakeReleasedImport(id);
    if (count > 0 && IsConnected)
      ThreadPool.QueueUserWorkItem(_ => TrySend(Frame(FrameKind.Release, w => { w.Write(id); w.Write(count); })));
  }

  private object? Call(long objectId, string methodKey, Type[] genericArgs, object?[] args, Type returnType, ParameterInfo[] parameters)
  {
    if (!IsConnected)
      throw new RemotingException($"The connection '{Name}' is closed.");

    var callId  = Interlocked.Increment(ref _nextCallId);
    var pending = new PendingCall();
    _pending[callId] = pending;

    try
    {
      Send(Frame(FrameKind.Call, w =>
      {
        w.Write(callId);
        w.Write(objectId);
        w.Write(methodKey);
        w.Write(genericArgs.Length);
        foreach (var g in genericArgs) w.Write(TypeNames.Encode(g));
        w.Write(args.Length);
        var values = new ValueWriter(w, _references);
        foreach (var a in args) values.Write(a);
      }));

      var reader = pending.Wait();
      var result = new ValueReader(reader, _references);
      var value  = result.Read(returnType);

      var byRefCount = reader.ReadInt32();
      for (var i = 0; i < byRefCount; i++)
      {
        var index = reader.ReadInt32();
        args[index] = result.Read(parameters[index].ParameterType.GetElementType()!);
      }

      return value;
    }
    finally
    {
      _pending.TryRemove(callId, out _);
    }
  }

  private void Send(byte[] frame)
  {
    lock (_writeLock)
    {
      try
      {
        _stream.Write(frame);
        _stream.Flush();
      }
      catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
      {
        Close(ex);
        throw new RemotingException($"The connection '{Name}' is closed.", ex);
      }
    }
  }

  private void TrySend(byte[] frame)
  {
    try { Send(frame); }
    catch (RemotingException) { /* the connection closed; nothing left to release */ }
  }

  private static byte[] Frame(FrameKind kind, Action<BinaryWriter> body)
  {
    using var buffer = new MemoryStream();
    using (var w = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
    {
      w.Write(0);
      w.Write((byte)kind);
      body(w);
    }

    var frame = buffer.ToArray();
    BitConverter.TryWriteBytes(frame.AsSpan(0, 4), frame.Length - 4);
    return frame;
  }

  private void Close(Exception? reason)
  {
    if (Interlocked.Exchange(ref _closed, 1) != 0)
      return;

    foreach (var pending in _pending.Values)
      pending.Fail(new RemotingException($"The connection '{Name}' closed.", reason));

    _references.Clear();
    try { _stream.Dispose(); } catch (IOException) { /* already broken */ }
    Closed?.Invoke(this, EventArgs.Empty);
  }

  private enum FrameKind : byte { Call = 1, Return, Error, Release }

  private sealed class PendingCall
  {
    private readonly ManualResetEventSlim _done = new();
    private BinaryReader? _reader;
    private Exception?    _error;

    public void Complete(BinaryReader reader) { _reader = reader; _done.Set(); }

    public void Fail(Exception error) { _error = error; _done.Set(); }

    public BinaryReader Wait()
    {
      _done.Wait();
      return _error is null ? _reader! : throw _error;
    }
  }
}
