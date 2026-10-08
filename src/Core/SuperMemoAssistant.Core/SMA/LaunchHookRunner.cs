// Registry and runner of launch hooks: runs plugin work around a SuperMemo start, one hook at a time, without ever blocking the start or exit.
namespace SuperMemoAssistant.SMA
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Interop.SMA;

  /// <summary>What happened when one hook ran.</summary>
  public sealed class LaunchHookOutcome
  {
    public LaunchHookOutcome(string name, string phase, LaunchHookResult result, string error, bool timedOut)
    {
      Name     = name;
      Phase    = phase;
      Result   = result;
      Error    = error;
      TimedOut = timedOut;
    }

    public string Name { get; }

    public string Phase { get; }

    public LaunchHookResult Result { get; }

    public string Error { get; }

    public bool TimedOut { get; }
  }

  public sealed class LaunchHookRunner
  {
    public const string BeforeLaunchPhase = "before launch";
    public const string AfterExitPhase    = "after exit";

    private readonly object                                    _lock  = new object();
    private readonly List<RegisteredHook>                      _hooks = new List<RegisteredHook>();
    private readonly Action<string>                            _notify;

    public LaunchHookRunner(Action<string> notify)
    {
      _notify = notify ?? (_ => { });
    }

    public void Register(
      string                               name,
      Func<SMLaunchInfo, LaunchHookResult> beforeLaunch,
      Func<SMLaunchInfo, LaunchHookResult> afterExit)
    {
      if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("A launch hook needs a name.", nameof(name));

      if (beforeLaunch == null && afterExit == null)
        throw new ArgumentException("A launch hook needs at least one of beforeLaunch and afterExit.");

      lock (_lock)
        _hooks.Add(new RegisteredHook(name, beforeLaunch, afterExit));

      LogTo.Information("Launch hook {Name} registered", name);
    }

    /// <summary>Runs every beforeLaunch hook off the calling thread. Never throws.</summary>
    public Task<IReadOnlyList<LaunchHookOutcome>> RunBeforeLaunchAsync(SMLaunchInfo info, TimeSpan timeout)
    {
      return Task.Run(() => RunAll(BeforeLaunchPhase, h => h.BeforeLaunch, info, timeout));
    }

    /// <summary>Runs every afterExit hook. Never throws.</summary>
    public IReadOnlyList<LaunchHookOutcome> RunAfterExit(SMLaunchInfo info, TimeSpan timeout)
    {
      return RunAll(AfterExitPhase, h => h.AfterExit, info, timeout);
    }

    private IReadOnlyList<LaunchHookOutcome> RunAll(
      string                                                  phase,
      Func<RegisteredHook, Func<SMLaunchInfo, LaunchHookResult>> select,
      SMLaunchInfo                                            info,
      TimeSpan                                                timeout)
    {
      RegisteredHook[] hooks;

      lock (_lock)
        hooks = _hooks.ToArray();

      return hooks.Select(h => (h.Name, Hook: select(h)))
                  .Where(h => h.Hook != null)
                  .Select(h => RunOne(h.Name, phase, h.Hook, info, timeout))
                  .ToList();
    }

    private LaunchHookOutcome RunOne(string name, string phase, Func<SMLaunchInfo, LaunchHookResult> hook, SMLaunchInfo info, TimeSpan timeout)
    {
      // A dedicated thread per hook: a hook that never returns keeps its thread, so it must not starve the thread pool
      // that the next hook needs to start within its own limit.
      var task = Task.Factory.StartNew(() => hook(info), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

      try
      {
        if (!task.Wait(timeout))
        {
          var message = $"{name} ({phase}) did not finish within {timeout.TotalSeconds:0} seconds. SuperMemo continues without waiting.";

          LogTo.Warning("Launch hook {Name} timed out in phase {Phase}", name, phase);
          _notify(message);

          return new LaunchHookOutcome(name, phase, null, message, true);
        }
      }
      catch (AggregateException ex)
      {
        var inner   = ex.InnerException ?? ex;
        var message = $"{name} ({phase}) failed: {inner.Message}";

        LogTo.Error(inner, "Launch hook {Name} failed in phase {Phase}", name, phase);
        _notify(message);

        return new LaunchHookOutcome(name, phase, null, message, false);
      }

      var result = task.Result ?? LaunchHookResult.Nothing;

      foreach (var action in result.Actions)
        LogTo.Information("Launch hook {Name} ({Phase}): {Action}", name, phase, action);

      foreach (var warning in result.Warnings)
      {
        LogTo.Warning("Launch hook {Name} ({Phase}): {Warning}", name, phase, warning);
        _notify($"{name}: {warning}");
      }

      LogTo.Information("Launch hook {Name} ({Phase}) finished", name, phase);

      return new LaunchHookOutcome(name, phase, result, null, false);
    }

    private sealed class RegisteredHook
    {
      public RegisteredHook(string name, Func<SMLaunchInfo, LaunchHookResult> beforeLaunch, Func<SMLaunchInfo, LaunchHookResult> afterExit)
      {
        Name         = name;
        BeforeLaunch = beforeLaunch;
        AfterExit    = afterExit;
      }

      public string Name { get; }

      public Func<SMLaunchInfo, LaunchHookResult> BeforeLaunch { get; }

      public Func<SMLaunchInfo, LaunchHookResult> AfterExit { get; }
    }
  }
}
