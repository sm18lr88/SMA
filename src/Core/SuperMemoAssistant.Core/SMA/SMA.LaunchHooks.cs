// Connects launch hooks to the SMA lifecycle: they run before sm20.exe is inspected and again after it has exited.
namespace SuperMemoAssistant.SMA
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using global::SuperMemoAssistant.Hooks.Symbols;
  using Interop.SMA;
  using Sys.Windows;

  public partial class SMA
  {
    /// <summary>Time a beforeLaunch hook may take: rebuilding sm20.exe takes seconds, and SuperMemo waits for it.</summary>
    public static readonly TimeSpan BeforeLaunchTimeout = TimeSpan.FromMinutes(3);

    /// <summary>Time an afterExit hook may take: SMA stays open until it is done.</summary>
    public static readonly TimeSpan AfterExitTimeout = TimeSpan.FromMinutes(1);

    private readonly LaunchHookRunner _launchHooks;

    private SMLaunchInfo _launchInfo;

    /// <summary>For tests: a SuperMemo Assistant whose launch hook warnings go to <paramref name="notify" /> instead of a desktop notification.</summary>
    internal SMA(Action<string> notify)
    {
      _launchHooks = new LaunchHookRunner(notify);
    }

    private static void ShowAsDesktopNotification(string message) => message.ShowDesktopNotification();

    /// <inheritdoc />
    public void RegisterLaunchHook(
      string                               name,
      Func<SMLaunchInfo, LaunchHookResult> beforeLaunch,
      Func<SMLaunchInfo, LaunchHookResult> afterExit)
    {
      _launchHooks.Register(name, beforeLaunch, afterExit);
    }

    /// <summary>
    ///   Runs the beforeLaunch hooks, and only then resolves the symbols, so the symbol table always describes the sm20.exe
    ///   that is started next.
    /// </summary>
    internal async Task<SymbolTable> PrepareSuperMemoAsync(SMLaunchInfo info, Func<SymbolTable> resolveSymbols)
    {
      _launchInfo = info;

      await _launchHooks.RunBeforeLaunchAsync(info, BeforeLaunchTimeout).ConfigureAwait(false);

      return resolveSymbols();
    }

    /// <summary>Runs the afterExit hooks once for the start that just ended.</summary>
    internal void RunAfterExitHooks()
    {
      var info = Interlocked.Exchange(ref _launchInfo, null);

      if (info != null)
        _launchHooks.RunAfterExit(info, AfterExitTimeout);
    }
  }
}
