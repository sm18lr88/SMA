// Incoming side of RpcConnection: frame reader loop and dispatch of calls to local objects.
namespace PluginManager.Remoting;

using System.Reflection;
using System.Text;

public sealed partial class RpcConnection
{
  /// <summary>Optional sink for failures that cannot be reported to a caller (e.g. a broken reply).</summary>
  public static Action<Exception>? UnhandledError { get; set; }

  private void ReadLoop()
  {
    try
    {
      var header = new byte[4];
      while (IsConnected)
      {
        if (!ReadExactly(header))
          break;

        var body = new byte[BitConverter.ToInt32(header, 0)];
        if (!ReadExactly(body))
          break;

        var reader = new BinaryReader(new MemoryStream(body), Encoding.UTF8);
        switch ((FrameKind)reader.ReadByte())
        {
          case FrameKind.Call:
            ThreadPool.QueueUserWorkItem(_ => HandleCall(reader));
            break;

          case FrameKind.Return:
            if (_pending.TryGetValue(reader.ReadInt64(), out var returned))
              returned.Complete(reader);
            break;

          case FrameKind.Error:
            if (_pending.TryGetValue(reader.ReadInt64(), out var failed))
              failed.Fail(new RemoteException(reader.ReadString(), reader.ReadString(), reader.ReadString()));
            break;

          case FrameKind.Release:
            _references.ReleaseExport(reader.ReadInt64(), reader.ReadInt32());
            break;
        }
      }

      Close(null);
    }
    catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
    {
      Close(ex);
    }
  }

  private bool ReadExactly(byte[] buffer)
  {
    var read = 0;
    while (read < buffer.Length)
    {
      var n = _stream.Read(buffer, read, buffer.Length - read);
      if (n == 0)
        return false;
      read += n;
    }
    return true;
  }

  [ThreadStatic]
  private static List<Action>? _afterReply;

  /// <summary>
  ///   Runs <paramref name="action" /> once the reply to the remote call executing on this thread is sent, or at once
  ///   outside a remote call. Use it for work that would cut the reply off, such as ending the process.
  /// </summary>
  public static void RunAfterReply(Action action)
  {
    if (_afterReply is null)
      action();
    else
      _afterReply.Add(action);
  }

  private void HandleCall(BinaryReader reader)
  {
    var callId = reader.ReadInt64();
    _afterReply = new List<Action>();

    try
    {
      HandleCallCore(reader, callId);
    }
    finally
    {
      var pending = _afterReply;
      _afterReply = null;

      foreach (var action in pending)
        try { action(); }
        catch (Exception ex) { UnhandledError?.Invoke(ex); }
    }
  }

  private void HandleCallCore(BinaryReader reader, long callId)
  {
    try
    {
      var target    = _references.Local(reader.ReadInt64());
      var methodKey = reader.ReadString();
      var generics  = new Type[reader.ReadInt32()];
      for (var i = 0; i < generics.Length; i++)
        generics[i] = TypeNames.Resolve(reader.ReadString());

      var (method, parameters, returnType) = methodKey == MethodKeys.DelegateInvoke
        ? DelegateSignature((Delegate)target)
        : Signature(MethodKeys.Resolve(methodKey, generics));

      var argCount = reader.ReadInt32();
      if (argCount != parameters.Length)
        throw new RemotingException($"Argument count mismatch for {method.Name}: expected {parameters.Length}, got {argCount}.");

      var values = new ValueReader(reader, _references);
      var args   = new object?[argCount];
      for (var i = 0; i < argCount; i++)
      {
        var type = parameters[i].ParameterType;
        args[i] = values.Read(type.IsByRef ? type.GetElementType()! : type);
      }

      object? result;
      try
      {
        result = method.Invoke(target, args);
      }
      catch (TargetInvocationException ex) when (ex.InnerException is not null)
      {
        throw ex.InnerException;
      }

      TrySend(Frame(FrameKind.Return, w =>
      {
        w.Write(callId);
        var writer = new ValueWriter(w, _references);
        writer.Write(returnType == typeof(void) ? null : result);

        var byRef = parameters.Where(p => p.ParameterType.IsByRef).ToList();
        w.Write(byRef.Count);
        foreach (var p in byRef)
        {
          w.Write(p.Position);
          writer.Write(args[p.Position]);
        }
      }));
    }
    catch (Exception ex)
    {
      TrySendError(callId, ex);
    }
  }

  private void TrySendError(long callId, Exception ex)
  {
    var remote = ex as RemoteException;
    try
    {
      TrySend(Frame(FrameKind.Error, w =>
      {
        w.Write(callId);
        w.Write(remote?.RemoteType ?? ex.GetType().FullName ?? ex.GetType().Name);
        w.Write(ex.Message);
        w.Write(ex.StackTrace ?? string.Empty);
      }));
    }
    catch (Exception sendError)
    {
      UnhandledError?.Invoke(new AggregateException(ex, sendError));
    }
  }

  private static (MethodInfo Method, ParameterInfo[] Parameters, Type ReturnType) Signature(MethodInfo method) =>
    (method, method.GetParameters(), method.ReturnType);

  private static (MethodInfo Method, ParameterInfo[] Parameters, Type ReturnType) DelegateSignature(Delegate target)
  {
    var invoke = target.GetType().GetMethod("Invoke")!;
    return (invoke, invoke.GetParameters(), invoke.ReturnType);
  }
}
