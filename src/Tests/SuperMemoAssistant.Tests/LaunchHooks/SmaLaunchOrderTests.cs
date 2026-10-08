// Checks the SMA lifecycle wiring: hooks run before the symbols are resolved, and after exit before the plugins are stopped.
namespace SuperMemoAssistant.Tests.LaunchHooks;

using SuperMemoAssistant.Hooks.Symbols;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.SMA;
using Xunit;

public class SmaLaunchOrderTests
{
  private static readonly string ExePath = Path.Combine(Path.GetTempPath(), "sm-root", "sm20.exe");

  private static readonly string CollectionPath = Path.Combine(Path.GetTempPath(), "sm-root", "systems", "coll");

  private static readonly SMLaunchInfo Info = new(ExePath, CollectionPath);

  [Fact]
  public async Task BeforeLaunchHooks_RunBeforeTheSymbolsAreResolved()
  {
    var order = new List<string>();
    var sma   = new SMA(_ => { });

    sma.RegisterLaunchHook("themes", _ => { order.Add("hook"); return LaunchHookResult.Nothing; }, null!);

    await sma.PrepareSuperMemoAsync(Info, () => { order.Add("resolve"); return null!; });

    Assert.Equal(["hook", "resolve"], order);
  }

  [Fact]
  public async Task AFailingBeforeLaunchHook_IsReported_AndDoesNotStopTheSymbolsFromResolving()
  {
    var notified = new List<string>();
    var sma      = new SMA(notified.Add);
    var resolved = false;

    sma.RegisterLaunchHook("broken", _ => throw new InvalidOperationException("boom"), null!);

    await sma.PrepareSuperMemoAsync(Info, () => { resolved = true; return null!; });

    Assert.True(resolved);
    Assert.Contains("boom", Assert.Single(notified));
  }

  [Fact]
  public async Task AfterExitHooks_RunBeforeTheStoppedSubscribers_AndOnlyOncePerStart()
  {
    var order = new List<string>();
    var sma   = new SMA(_ => { });

    sma.RegisterLaunchHook("themes", null!, _ => { order.Add("after-exit hook"); return LaunchHookResult.Nothing; });
    sma.OnSMStoppedInternalEvent += (_, _) => order.Add("plugins stopped");

    await sma.PrepareSuperMemoAsync(Info, () => null!);

    sma.OnSMStopped();
    sma.OnSMStopped();

    Assert.Equal(["after-exit hook", "plugins stopped", "plugins stopped"], order);
  }

  [Fact]
  public void AfterExitHooks_DoNotRun_WhenNoStartWasPrepared()
  {
    var ran = false;
    var sma = new SMA(_ => { });

    sma.RegisterLaunchHook("themes", null!, _ => { ran = true; return LaunchHookResult.Nothing; });
    sma.OnSMStopped();

    Assert.False(ran);
  }

  [Fact]
  public async Task TheHookReceivesTheExeAndTheCollectionOfThisStart()
  {
    SMLaunchInfo? seen = null;
    var           sma  = new SMA(_ => { });

    ISuperMemoAssistant asInterface = sma;

    asInterface.RegisterLaunchHook("themes", info => { seen = info; return LaunchHookResult.Nothing; }, null!);

    await sma.PrepareSuperMemoAsync(Info, () => null!);

    Assert.Equal(ExePath, seen!.SuperMemoExePath);
    Assert.Equal(CollectionPath, seen.CollectionFolder);
  }
}
