// Runs native-call requests on SuperMemo's main (VCL) thread, inside its message loop's PeekMessageW call.
namespace SuperMemoAssistant.Hooks.Agent;

using System.Collections.Concurrent;

internal sealed class MainThreadDispatcher(uint mainThreadId, NativeCaller caller, AgentLink link)
{
  private static readonly uint WakeMessage = Win32.RegisterWindowMessage("SuperMemoAssistant.Agent.Wake");

  private readonly ConcurrentQueue<ExecuteNative> _pending = new();
  private int _running;

  /// <summary>Queues a call and wakes the main thread's message loop.</summary>
  public void Enqueue(ExecuteNative request)
  {
    _pending.Enqueue(request);

    if (!Win32.PostThreadMessage(mainThreadId, WakeMessage, 0, 0))
      link.Log(AgentLogLevel.Warning, "Could not wake SuperMemo's main thread; the call will run on its next message.");
  }

  /// <summary>Called from the PeekMessageW hook. Runs queued calls only on the main thread and never re-entrantly.</summary>
  public void RunPending()
  {
    if (_pending.IsEmpty || Win32.GetCurrentThreadId() != mainThreadId || Interlocked.Exchange(ref _running, 1) == 1)
      return;

    try
    {
      while (_pending.TryDequeue(out var request))
        link.Post(Execute(request));
    }
    finally
    {
      Volatile.Write(ref _running, 0);
    }
  }

  private NativeResult Execute(ExecuteNative request)
  {
    try
    {
      return new NativeResult(request.CallId, true, caller.Execute(request.Method, request.Args), null);
    }
    catch (Exception ex)
    {
      return new NativeResult(request.CallId, false, 0, ex.Message);
    }
  }
}
