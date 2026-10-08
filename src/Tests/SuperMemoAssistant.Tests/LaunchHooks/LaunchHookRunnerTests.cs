// Checks that the launch hook runner keeps its promises: registration order, failure isolation, time limits, and what the user is told.
namespace SuperMemoAssistant.Tests.LaunchHooks;

using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.SMA;
using Xunit;

public class LaunchHookRunnerTests
{
  private static readonly string ExePath = Path.Combine(Path.GetTempPath(), "sm-root", "sm20.exe");

  private static readonly string CollectionPath = Path.Combine(Path.GetTempPath(), "sm-root", "systems", "coll");

  private static readonly SMLaunchInfo Info = new(ExePath, CollectionPath);

  private static readonly TimeSpan Generous = TimeSpan.FromSeconds(10);

  private readonly List<string>      _notified = [];
  private readonly LaunchHookRunner  _runner;

  public LaunchHookRunnerTests()
  {
    _runner = new LaunchHookRunner(_notified.Add);
  }

  private static Func<SMLaunchInfo, LaunchHookResult> Records(List<string> log, string entry, LaunchHookResult? result = null) =>
    info =>
    {
      lock (log)
        log.Add($"{entry}:{info.CollectionFolder}");

      return result ?? LaunchHookResult.Nothing;
    };

  [Fact]
  public async Task BeforeLaunch_RunsHooksInRegistrationOrder_WithTheLaunchInfo()
  {
    var log = new List<string>();

    _runner.Register("first", Records(log, "first"), null!);
    _runner.Register("second", Records(log, "second"), null!);

    var outcomes = await _runner.RunBeforeLaunchAsync(Info, Generous);

    Assert.Equal([$"first:{CollectionPath}", $"second:{CollectionPath}"], log);
    Assert.Equal(["first", "second"], outcomes.Select(o => o.Name));
    Assert.All(outcomes, o => Assert.Equal(LaunchHookRunner.BeforeLaunchPhase, o.Phase));
  }

  [Fact]
  public void AfterExit_RunsOnlyTheAfterExitDelegates()
  {
    var log = new List<string>();

    _runner.Register("before-only", Records(log, "before"), null!);
    _runner.Register("both", Records(log, "both-before"), Records(log, "both-after"));

    var outcomes = _runner.RunAfterExit(Info, Generous);

    Assert.Equal([$"both-after:{CollectionPath}"], log);
    Assert.Equal(["both"], outcomes.Select(o => o.Name));
  }

  [Fact]
  public async Task AHookThatThrows_IsReported_AndTheNextHookStillRuns()
  {
    var log = new List<string>();

    _runner.Register("broken", _ => throw new InvalidOperationException("disk is full"), null!);
    _runner.Register("fine", Records(log, "fine"), null!);

    var outcomes = await _runner.RunBeforeLaunchAsync(Info, Generous);

    Assert.Contains("disk is full", outcomes[0].Error);
    Assert.False(outcomes[0].TimedOut);
    Assert.Single(_notified);
    Assert.Contains("broken", _notified[0]);
    Assert.Contains("disk is full", _notified[0]);
    Assert.Single(log);
    Assert.Null(outcomes[1].Error);
  }

  [Fact]
  public async Task AHookThatRunsPastItsLimit_IsAbandoned_AndReported()
  {
    using var release = new ManualResetEventSlim();

    var log = new List<string>();

    _runner.Register("slow", _ => { release.Wait(TimeSpan.FromSeconds(30)); return LaunchHookResult.Nothing; }, null!);
    _runner.Register("after-the-slow-one", Records(log, "ran"), null!);

    // The slow hook cannot finish before release.Set(); the fast one runs on its own thread, so one second is ample.
    var outcomes = await _runner.RunBeforeLaunchAsync(Info, TimeSpan.FromSeconds(1));

    release.Set();

    Assert.True(outcomes[0].TimedOut);
    Assert.Contains("did not finish", _notified.Single());
    Assert.Single(log);
  }

  [Fact]
  public async Task Warnings_AreShownToTheUser_ButActionsAreOnlyLogged()
  {
    _runner.Register("themes", _ => new LaunchHookResult(["Rebuilt sm20.exe"], ["The live patch was skipped"]), null!);

    await _runner.RunBeforeLaunchAsync(Info, Generous);

    Assert.Equal(["themes: The live patch was skipped"], _notified);
  }

  [Fact]
  public async Task ANullResult_IsTreatedAsNothingToReport()
  {
    _runner.Register("quiet", _ => null!, null!);

    var outcomes = await _runner.RunBeforeLaunchAsync(Info, Generous);

    Assert.Empty(_notified);
    Assert.NotNull(outcomes[0].Result);
  }

  [Fact]
  public async Task BeforeLaunch_ReturnsAtOnce_WhileAHookIsStillRunning()
  {
    using var started = new ManualResetEventSlim();
    using var release = new ManualResetEventSlim();

    _runner.Register("slow", _ => { started.Set(); release.Wait(TimeSpan.FromSeconds(30)); return LaunchHookResult.Nothing; }, null!);

    var pending = _runner.RunBeforeLaunchAsync(Info, Generous);

    Assert.True(started.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    Assert.False(pending.IsCompleted, "the caller must not be blocked, and the result is not ready until the hook finishes");

    release.Set();

    Assert.Single(await pending);
  }

  [Theory]
  [InlineData("")]
  [InlineData("  ")]
  public void Register_RequiresAName(string name)
  {
    Assert.Throws<ArgumentException>(() => _runner.Register(name, _ => LaunchHookResult.Nothing, null!));
  }

  [Fact]
  public void Register_RequiresAtLeastOneDelegate()
  {
    Assert.Throws<ArgumentException>(() => _runner.Register("nothing", null!, null!));
  }

  [Fact]
  public async Task NoHooks_IsNotAnError()
  {
    Assert.Empty(await _runner.RunBeforeLaunchAsync(Info, Generous));
    Assert.Empty(_runner.RunAfterExit(Info, Generous));
  }
}
