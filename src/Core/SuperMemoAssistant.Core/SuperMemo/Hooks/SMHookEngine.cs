// Starts sm20.exe suspended, injects the agent, and exposes main-thread native calls to the rest of SMA.
namespace SuperMemoAssistant.SuperMemo.Hooks
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Extensions;
  using Interop;
  using Interop.SuperMemo.Core;
  using Natives;
  using Process.NET;
  using Process.NET.Memory;
  using Process.NET.Types;
  using SMA;
  using SuperMemoAssistant.Hooks.Agent;
  using SuperMemoAssistant.Hooks.Symbols;
  using Sys.Exceptions;

  public sealed class SMHookEngine : IDisposable
  {
    private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(60);

    private NativeProcess.StartedProcess _process;
    private AgentConnection              _agent;

    public SMHookEngine()
    {
      Core.Hook = this;
    }

    /// <summary>
    ///   Starts SuperMemo with <paramref name="collection" /> and its main thread suspended, injects the agent and waits for
    ///   its hooks. SuperMemo stays suspended until <see cref="SignalWakeUp" />.
    /// </summary>
    public async Task<(IProcess Process, SMNatives Natives)> CreateAndHookAsync(
      SMCollection            collection,
      string                  binPath,
      IEnumerable<ISMAHookIO> ioCallbacks,
      SymbolTable             symbols)
    {
      LogTo.Debug("Starting SuperMemo and injecting the agent");

      var sinks = ioCallbacks.ToList();
      _agent = new AgentConnection(sinks);
      _process = NativeProcess.StartSuspended(binPath, collection.GetKnoFilePath().Quotify(), Path.GetDirectoryName(binPath));

      Task<AgentReady> handshake  = null;
      Task<uint>       agentStart = null;

      try
      {
        handshake = _agent.HandshakeAsync(
          _ => new ConfigureAgent(
            _process.MainThreadId,
            symbols.ToAgentFunctions(),
            symbols.Offset(NativePointer.ElWdw_ComponentsDataPtr),
            symbols.Offset(NativePointer.Queue_SizeOffset),
            sinks.SelectMany(s => s.GetTargetFilePaths()).ToList()),
          HandshakeTimeout);

        agentStart = AgentInjector.InjectAsync(_process, SMAFileSystem.HookAgentFile.FullPathWin, _agent.PipeName);

        // A failed agent start ends the remote thread long before the handshake would time out.
        if (await Task.WhenAny(handshake, agentStart).ConfigureAwait(false) == agentStart && await agentStart.ConfigureAwait(false) != 0)
          throw new HookException($"The agent failed to start inside SuperMemo (code {agentStart.Result}).");

        var ready    = await handshake.ConfigureAwait(false);
        var exitCode = await agentStart.ConfigureAwait(false);

        if (exitCode != 0)
          throw new HookException($"The agent failed to start inside SuperMemo (code {exitCode}).");

        LogTo.Debug("SuperMemo started and hooked, pId: {PId}", _process.ProcessId);

        return (new ProcessSharp(_process.ProcessId, MemoryType.Remote), new SMNatives(symbols, (IntPtr)ready.ModuleBase));
      }
      catch (Exception ex)
      {
        Abort();
        Observe(handshake);
        Observe(agentStart);

        if (ex is HookException)
          throw;

        throw new HookException("Hook setup failed: " + ex.Message, ex);
      }
    }

    /// <summary>Marks a task abandoned by a failed setup as observed, so its later fault is not reported as unhandled.</summary>
    private static void Observe(Task task) =>
      task?.ContinueWith(t => LogTo.Debug(t.Exception, "Abandoned hook setup task failed"), TaskContinuationOptions.OnlyOnFaulted);

    /// <summary>How many times SuperMemo opened a file of the selected collection since the agent started.</summary>
    public int CollectionFilesOpened => _agent?.CollectionFilesOpened ?? 0;

    /// <summary>Lets SuperMemo run once SMA and plugins have handled SM starting.</summary>
    public void SignalWakeUp() => NativeProcess.Resume(_process.ThreadHandle);

    /// <summary>
    ///   Executes <paramref name="method" /> on SuperMemo's main thread. Arguments may be integers, enums, booleans, pointers or
    ///   strings (passed as Delphi UnicodeStrings).
    /// </summary>
    public long ExecuteOnMainThread(NativeMethod method, IReadOnlyList<object> args)
    {
      var agent = _agent ?? throw new InvalidOperationException("SuperMemo is not running.");
      return agent.Execute(method, args.Select(ToNativeArg).ToArray());
    }

    public void CleanupHooks()
    {
      _agent?.Dispose();
      _agent = null;

      NativeProcess.Close(_process.ThreadHandle);
      NativeProcess.Close(_process.ProcessHandle);
      _process = default;
    }

    public void Dispose() => CleanupHooks();

    private void Abort()
    {
      try
      {
        if (_process.ProcessId != 0)
          Process.GetProcessById(_process.ProcessId).Kill();
      }
      catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
      {
        LogTo.Debug(ex, "SuperMemo already exited");
      }

      CleanupHooks();
    }

    private static NativeArg ToNativeArg(object value) => value switch
    {
      null                       => NativeArg.Of(0),
      string s                   => NativeArg.Of(s),
      DelphiUTF16String s        => NativeArg.Of(s.Text),
      IntPtr p                   => NativeArg.Of((long)p),
      bool b                     => NativeArg.Of(b ? 1 : 0),
      Enum e                     => NativeArg.Of(Convert.ToInt64(e, null)),
      IConvertible c             => NativeArg.Of(c.ToInt64(null)),
      _                          => throw new ArgumentException($"Unsupported native argument type {value.GetType()}."),
    };
  }
}
