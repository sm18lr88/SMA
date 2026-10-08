// SMA's end of the agent pipe: handshake, synchronous native calls, and ordered delivery of file I/O events.
namespace SuperMemoAssistant.SuperMemo.Hooks
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.IO;
  using System.IO.Pipes;
  using System.Linq;
  using System.Security.Cryptography;
  using System.Threading;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using SuperMemoAssistant.Hooks.Agent;

  internal sealed class AgentConnection : IDisposable
  {
    /// <summary>Native calls may open SuperMemo dialogs that wait on the user, so the bound is generous.</summary>
    public static readonly TimeSpan CallTimeout = TimeSpan.FromMinutes(2);

    private readonly NamedPipeServerStream _pipe;
    private readonly object                _writeLock = new();
    private readonly ConcurrentDictionary<uint, TaskCompletionSource<NativeResult>> _pending = new();
    private readonly IReadOnlyList<ISMAHookIO> _ioSinks;
    private uint _nextCallId;
    private int  _collectionFilesOpened;

    public AgentConnection(IReadOnlyList<ISMAHookIO> ioSinks)
    {
      PipeName = "sma-agent-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
      _ioSinks = ioSinks;
      _pipe    = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                                           PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    }

    public string PipeName { get; }

    public event EventHandler Closed;

    /// <summary>How many times SuperMemo opened one of the watched collection files.</summary>
    public int CollectionFilesOpened => Volatile.Read(ref _collectionFilesOpened);

    /// <summary>Waits for the agent, receives <see cref="AgentReady" />, sends the configuration and waits for its acknowledgement.</summary>
    public async Task<AgentReady> HandshakeAsync(Func<AgentReady, ConfigureAgent> configure, TimeSpan timeout)
    {
      using var cts = new CancellationTokenSource(timeout);
      await _pipe.WaitForConnectionAsync(cts.Token).ConfigureAwait(false);

      var ready = await Task.Run(() => AgentProtocol.Read(_pipe), cts.Token).ConfigureAwait(false) as AgentReady
                  ?? throw new InvalidDataException("The agent did not start with a Ready message.");

      Send(configure(ready));

      while (true)
      {
        var message = await Task.Run(() => AgentProtocol.Read(_pipe), cts.Token).ConfigureAwait(false);
        switch (message)
        {
          case AgentConfigured { Success: true }:
            new Thread(ReadLoop) { IsBackground = true, Name = "SMA agent reader" }.Start();
            return ready;

          case AgentConfigured failed:
            throw new InvalidOperationException("The agent could not install its hooks: " + failed.Error);

          case AgentLog log:
            Log(log);
            break;

          default:
            throw new InvalidDataException($"Unexpected agent message during handshake: {message?.GetType().Name ?? "end of stream"}.");
        }
      }
    }

    /// <summary>Runs <paramref name="method" /> on SuperMemo's main thread and returns its normalized result.</summary>
    public long Execute(SuperMemoAssistant.SuperMemo.NativeMethod method, IReadOnlyList<NativeArg> args)
    {
      var callId = Interlocked.Increment(ref _nextCallId);
      var result = new TaskCompletionSource<NativeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
      _pending[callId] = result;

      try
      {
        Send(new ExecuteNative(callId, method, args));

        if (!result.Task.Wait(CallTimeout))
          throw new TimeoutException($"SuperMemo did not run {method} within {CallTimeout}.");

        var reply = result.Task.Result;
        return reply.Success ? reply.Value : throw new InvalidOperationException($"{method} failed in SuperMemo: {reply.Error}");
      }
      finally
      {
        _pending.TryRemove(callId, out _);
      }
    }

    public void Dispose()
    {
      try
      {
        if (_pipe.IsConnected)
          Send(new ShutdownAgent());
      }
      catch (IOException)
      {
        // SuperMemo already exited.
      }

      _pipe.Dispose();
    }

    private void Send(AgentMessage message)
    {
      var frame = AgentProtocol.Encode(message);
      lock (_writeLock)
      {
        _pipe.Write(frame, 0, frame.Length);
        _pipe.Flush();
      }
    }

    private void ReadLoop()
    {
      try
      {
        while (AgentProtocol.Read(_pipe) is { } message)
          Dispatch(message);
      }
      catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidDataException)
      {
        LogTo.Debug(ex, "Agent pipe closed");
      }

      foreach (var pending in _pending.Values)
        pending.TrySetException(new IOException("SuperMemo exited or the agent disconnected."));

      Closed?.Invoke(this, EventArgs.Empty);
    }

    private void Dispatch(AgentMessage message)
    {
      switch (message)
      {
        case NativeResult r when _pending.TryGetValue(r.CallId, out var pending):
          pending.TrySetResult(r);
          break;

        case FileCreated m:
          Interlocked.Increment(ref _collectionFilesOpened);
          ForEachSink(s => s.OnFileCreate(m.Path, (IntPtr)m.Handle));
          break;

        case FileSeeked m:
          ForEachSink(s => s.OnFileSeek((IntPtr)m.Handle, (uint)m.Position));
          break;

        case FileWritten m:
          ForEachSink(s => s.OnFileWrite((IntPtr)m.Handle, m.Data, (uint)m.Data.Length));
          break;

        case FileClosed m:
          ForEachSink(s => s.OnFileClose((IntPtr)m.Handle));
          break;

        case AgentLog log:
          Log(log);
          break;
      }
    }

    private void ForEachSink(Action<ISMAHookIO> action)
    {
      foreach (var sink in _ioSinks)
        try
        {
          action(sink);
        }
        catch (Exception ex)
        {
          LogTo.Error(ex, "Collection file hook {Sink} failed", sink.GetType().Name);
        }
    }

    private static void Log(AgentLog log)
    {
      switch (log.Level)
      {
        case AgentLogLevel.Error:   LogTo.Error("[agent] {Message}", log.Message); break;
        case AgentLogLevel.Warning: LogTo.Warning("[agent] {Message}", log.Message); break;
        default:                    LogTo.Debug("[agent] {Message}", log.Message); break;
      }
    }
  }
}
