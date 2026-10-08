// Runtime proof: inject the NativeAOT agent into a sandboxed sm20.exe on a hidden desktop and drive native calls through it.
namespace SuperMemoAssistant.Tests.E2E;

using Process.NET;
using Process.NET.Memory;
using SuperMemoAssistant.Hooks.Agent;
using SuperMemoAssistant.Hooks.Symbols;
using SuperMemoAssistant.SuperMemo;
using SuperMemoAssistant.SuperMemo.Hooks;
using Xunit;

[Collection("SuperMemo E2E")]
public sealed class AgentEndToEndTests
{
  private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(100);
  private static readonly string   LogPath  = Path.Combine(Path.GetTempPath(), "sma-e2e.log");

  /// <summary>Opt-in (SMA_E2E=1): starts a real SuperMemo 20 copy. Needs a SuperMemo 20 folder and a built app. The SuperMemo folder is found as SuperMemoLocation says.</summary>
  [Fact(Timeout = 150_000)]
  public async Task Agent_RunsNativeCallsOnSuperMemosMainThread()
  {
    var smRoot = SuperMemoLocation.Root ?? "";
    var agent  = Path.Combine(SmaAppHarness.AppDir, "SuperMemoAssistant.Hooks.Agent.dll");
    Assert.SkipUnless(Environment.GetEnvironmentVariable("SMA_E2E") == "1", "Set SMA_E2E=1 to run the SuperMemo end-to-end test.");
    Assert.SkipUnless(File.Exists(Path.Combine(smRoot, "sm20.exe")) && File.Exists(agent), "sm20.exe or the published agent is missing.");

    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
    deadline.CancelAfter(Deadline);
    var events = new List<string>();
    File.WriteAllText(LogPath, "");

    using var registry = new RegistryGuard();
    using var box      = SuperMemoSandbox.Create(smRoot);
    using var desktop  = new HiddenDesktop();

    var symbols = SymbolResolver.ResolveVerified(box.ExePath);
    var sink    = new RecordingSink();
    using var connection = new AgentConnection([sink]);

    var process = NativeProcess.StartSuspended(box.ExePath, $"\"{box.KnoPath}\"", box.Root, desktop.StartupDesktop);
    try
    {
      desktop.Track(process.ProcessHandle);

      var handshake = connection.HandshakeAsync(
        _ => new ConfigureAgent(process.MainThreadId, symbols.ToAgentFunctions(),
                                symbols.Offset(NativePointer.ElWdw_ComponentsDataPtr), symbols.Offset(NativePointer.Queue_SizeOffset),
                                [Path.Combine(box.CollectionDir, "info", "elementinfo.dat").ToLowerInvariant()]),
        Deadline);
      Step($"pid {process.ProcessId} suspended; injecting");
      var started = AgentInjector.InjectAsync(process, agent, connection.PipeName);
      Step("agent DLL loaded; waiting for the handshake");

      if (await Task.WhenAny(handshake, Task.Delay(TimeSpan.FromSeconds(20), deadline.Token)) != handshake)
      {
        var alive = !System.Diagnostics.Process.GetProcessById(process.ProcessId).HasExited;
        Step($"no handshake after 20s: start task {(started.IsCompletedSuccessfully ? "returned " + await started : started.Status.ToString())}, process alive: {alive}");
        Assert.Fail("The agent did not complete the handshake; see " + LogPath);
      }

      var ready = await handshake;
      Step($"handshake done, module base 0x{ready.ModuleBase:X}");
      Assert.Equal(0u, await started);
      Assert.Equal(process.ProcessId, ready.ProcessId);

      using var answerer = new CancellationTokenSource();
      var promptThread = StartupPromptAnswerer.Start(desktop.Handle, process.ProcessId, answerer.Token, events);
      NativeProcess.Resume(process.ThreadHandle);

      using var sm      = new ProcessSharp(process.ProcessId, MemoryType.Remote);
      var elWindVar     = symbols.GlobalAddress(NativePointer.ElWdw_InstancePtr, (IntPtr)ready.ModuleBase);
      var loadedElement = symbols.Offset(NativePointer.ElWdw_ElementIdPtr);

      Step("resumed; waiting for TElWind");
      var elWind = await PollAsync(() => sm.Memory.Read<IntPtr>(elWindVar), p => p != IntPtr.Zero, deadline.Token);
      Step($"ElWind = 0x{(long)elWind:X}");
      try
      {
        await PollAsync(() => sm.Memory.Read<int>(elWind + loadedElement), id => id > 0, TimeoutAfter(25));
      }
      catch (OperationCanceledException)
      {
        var windows = StartupPromptAnswerer.Windows(desktop.Handle, process.ProcessId).Select(StartupPromptAnswerer.ClassName);
        Step("no element loaded after 25s; visible windows: " + string.Join(", ", windows));
        throw;
      }
      Step($"loaded element {sm.Memory.Read<int>(elWind + loadedElement)}; file events so far {sink.Events}");

      await PollAsync(() => StartupPromptAnswerer.Windows(desktop.Handle, process.ProcessId).Any(h => StartupPromptAnswerer.ClassName(h) == "TContents"),
                      ready => ready, deadline.Token);
      await Task.Delay(1500, deadline.Token);

      var original = sm.Memory.Read<int>(elWind + loadedElement);
      foreach (var target in new[] { 1, original })
      {
        Step($"GoToElement({target})");
        await Task.Run(() => connection.Execute(NativeMethod.ElWdw_GoToElement, [NativeArg.Of(elWind), NativeArg.Of(target)]), deadline.Token);
        await Task.Delay(1500, deadline.Token);
        Step($"after call, loaded element = {sm.Memory.Read<int>(elWind + loadedElement)}; windows: "
             + string.Join(", ", StartupPromptAnswerer.Windows(desktop.Handle, process.ProcessId).Select(StartupPromptAnswerer.ClassName)));
        Assert.Equal(target, await PollAsync(() => sm.Memory.Read<int>(elWind + loadedElement), id => id == target, TimeoutAfter(15)));
      }

      answerer.Cancel();
      promptThread.Join();
    }
    finally
    {
      lock (events)
        foreach (var e in events)
          Step("harness: " + e);

      desktop.Dispose(); // kill-on-close job: the sandboxed SuperMemo tree ends here
      NativeProcess.Close(process.ThreadHandle);
      NativeProcess.Close(process.ProcessHandle);
    }

    foreach (var e in events)
      TestContext.Current.TestOutputHelper?.WriteLine(e);
  }

  private static CancellationToken TimeoutAfter(int seconds) => new CancellationTokenSource(TimeSpan.FromSeconds(seconds)).Token;

  private static void Step(string message) =>
    File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");

  private static async Task<T> PollAsync<T>(Func<T> read, Func<T, bool> done, CancellationToken token)
  {
    while (true)
    {
      var value = read();
      if (done(value))
        return value;

      await Task.Delay(250, token);
    }
  }


  private sealed class RecordingSink : ISMAHookIO
  {
    public int Events;

    public IEnumerable<string> GetTargetFilePaths() => [];
    public void OnFileCreate(string filePath, IntPtr fileHandle) => Interlocked.Increment(ref Events);
    public void OnFileSeek(IntPtr fileHandle, uint position) => Interlocked.Increment(ref Events);
    public void OnFileWrite(IntPtr fileHandle, byte[] buffer, uint count) => Interlocked.Increment(ref Events);
    public void OnFileClose(IntPtr fileHandle) => Interlocked.Increment(ref Events);
  }
}
