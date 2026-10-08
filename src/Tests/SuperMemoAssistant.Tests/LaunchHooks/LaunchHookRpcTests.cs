// Proves the launch hook contract works between a plugin process and SMA: delegates and results cross a real named pipe.
namespace SuperMemoAssistant.Tests.LaunchHooks;

using PluginManager.Remoting;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.SMA;
using Xunit;

public sealed class LaunchHookRpcTests : IDisposable
{
  private static readonly string ExePath = Path.Combine(Path.GetTempPath(), "sm-root", "sm20.exe");

  private static readonly string CollectionPath = Path.Combine(Path.GetTempPath(), "sm-root", "systems", "coll");

  private static readonly SMLaunchInfo Info = new(ExePath, CollectionPath);

  private readonly Host      _host = new();
  private readonly RpcServer _server;
  private readonly IHost     _proxy;

  public LaunchHookRpcTests()
  {
    var pipe = RpcEndpoint.NewPipeName();

    _server = RpcEndpoint.Serve(pipe, _host);
    _proxy  = RpcEndpoint.Connect<IHost>(pipe, TimeSpan.FromSeconds(10));
  }

  public void Dispose() => _server.Dispose();

  public interface IHost
  {
    void RegisterLaunchHook(string name, Func<SMLaunchInfo, LaunchHookResult> beforeLaunch, Func<SMLaunchInfo, LaunchHookResult> afterExit);
  }

  private sealed class Host : MarshalByRefObject, IHost
  {
    public List<string>     Notified { get; } = [];
    public LaunchHookRunner Runner   { get; }

    public Host() => Runner = new LaunchHookRunner(Notified.Add);

    public void RegisterLaunchHook(string name, Func<SMLaunchInfo, LaunchHookResult> beforeLaunch, Func<SMLaunchInfo, LaunchHookResult> afterExit) =>
      Runner.Register(name, beforeLaunch, afterExit);
  }

  [Fact]
  public async Task APluginsDelegates_AreCalledByTheHost_WithTheInfoAndReturnTheirResultByValue()
  {
    SMLaunchInfo? received = null;

    _proxy.RegisterLaunchHook(
      "themes",
      info => { received = info; return new LaunchHookResult(["rebuilt exe", "wrote css"], ["patch skipped"]); },
      info => new LaunchHookResult([$"synced {info.CollectionFolder}"], []));

    var before = (await _host.Runner.RunBeforeLaunchAsync(Info, TimeSpan.FromSeconds(10))).Single();
    var after  = _host.Runner.RunAfterExit(Info, TimeSpan.FromSeconds(10)).Single();

    Assert.Equal(CollectionPath, received!.CollectionFolder);
    Assert.Equal(["rebuilt exe", "wrote css"], before.Result.Actions);
    Assert.Equal(["patch skipped"], before.Result.Warnings);
    Assert.Equal([$"synced {CollectionPath}"], after.Result.Actions);
    Assert.Equal(["themes: patch skipped"], _host.Notified);
  }

  [Fact]
  public async Task AnExceptionInThePlugin_ReachesTheHostAsAFailedOutcome_NotAsACrash()
  {
    _proxy.RegisterLaunchHook("themes", _ => throw new InvalidOperationException("collection.ini is locked"), null!);

    var outcome = (await _host.Runner.RunBeforeLaunchAsync(Info, TimeSpan.FromSeconds(10))).Single();

    Assert.Contains("collection.ini is locked", outcome.Error);
    Assert.Contains("themes", _host.Notified.Single());
  }

  [Fact]
  public async Task AHookWithOnlyAnAfterExitDelegate_CanBeRegisteredWithANullBeforeLaunch()
  {
    _proxy.RegisterLaunchHook("exit-only", null!, _ => LaunchHookResult.Nothing);

    Assert.Empty(await _host.Runner.RunBeforeLaunchAsync(Info, TimeSpan.FromSeconds(10)));
    Assert.Single(_host.Runner.RunAfterExit(Info, TimeSpan.FromSeconds(10)));
  }

  [Fact]
  public async Task ASlowPlugin_IsAbandonedAtTheLimit_AndTheHostMovesOn()
  {
    using var release = new ManualResetEventSlim();

    _proxy.RegisterLaunchHook("slow", _ => { release.Wait(TimeSpan.FromSeconds(30)); return LaunchHookResult.Nothing; }, null!);

    var outcome = (await _host.Runner.RunBeforeLaunchAsync(Info, TimeSpan.FromMilliseconds(300))).Single();

    release.Set();

    Assert.True(outcome.TimedOut);
  }
}
