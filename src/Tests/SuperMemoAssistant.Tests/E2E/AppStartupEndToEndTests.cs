// Runs the real SuperMemoAssistant.exe: first alone, then as a full session that opens SuperMemo 20 with every plugin.
namespace SuperMemoAssistant.Tests.E2E;

using System.Text.RegularExpressions;
using Xunit;

[Collection("SuperMemo E2E")]
public sealed partial class AppStartupEndToEndTests
{
  /// <summary>
  ///   Plugins built by the solution. DevSandbox is left out:
  ///   it is a developer tool that opens a console window by design.
  /// </summary>
  private static readonly string[] AllPlugins =
  [
    "SuperMemoAssistant.Plugins.Books",
    "SuperMemoAssistant.Plugins.Dictionary",
    "SuperMemoAssistant.Plugins.Email",
    "SuperMemoAssistant.Plugins.Formulation",
    "SuperMemoAssistant.Plugins.ImageOcclusion",
    "SuperMemoAssistant.Plugins.Import",
    "SuperMemoAssistant.Plugins.LaTeX",
    "SuperMemoAssistant.Plugins.LocalApi",
    "SuperMemoAssistant.Plugins.OmniMemo",
    "SuperMemoAssistant.Plugins.PDF",
    "SuperMemoAssistant.Plugins.Template",
    "SuperMemoAssistant.Plugins.Writing",
  ];

  private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "sma-app-e2e.log");

  private static string SmRoot => SuperMemoLocation.Root ?? "";

  [Fact(Timeout = 120_000)]
  public async Task App_ReachesItsFirstWindow_AndStartsAPlugin()
  {
    SkipUnlessEnabled();
    File.WriteAllText(LogPath, "");

    using var desktop = new HiddenDesktop();
    using var app     = new SmaAppHarness(desktop, Path.Combine(SmRoot, "sm20.exe"));
    app.SeedPlugin("SuperMemoAssistant.Plugins.Template");
    app.Launch("");

    var log = await WaitForLogAsync(app, l => PluginsStarted(l) is not null, TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);
    var windows = StartupPromptAnswerer.Windows(desktop.Handle, app.ProcessId).Select(StartupPromptAnswerer.ClassName).ToList();
    Step("windows: " + string.Join(", ", windows) + Environment.NewLine + log);

    Assert.Contains(windows, w => w.StartsWith("HwndWrapper", StringComparison.Ordinal));
    Assert.Equal((1, 0), PluginsStarted(log));
    AssertNoProblems(log);
  }

  [Fact(Timeout = 120_000)]
  public async Task App_StartsTheOptionalThemesPlugin_AndItRegistersItsLaunchHookWithTheCore()
  {
    SkipUnlessEnabled();
    File.WriteAllText(LogPath, "");

    using var desktop = new HiddenDesktop();
    using var app     = new SmaAppHarness(desktop, Path.Combine(SmRoot, "sm20.exe"));
    app.SeedPlugin("SuperMemoAssistant.Plugins.Themes");
    app.Launch("");

    var log = await WaitForLogAsync(app, l => PluginsStarted(l) is not null, TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);
    Step(log);

    Assert.Equal((1, 0), PluginsStarted(log));
    Assert.Contains("Launch hook Themes registered", log);
    AssertNoProblems(log);
  }

  [Fact(Timeout = 300_000)]
  public async Task FullSession_OpensSuperMemo20_WithEveryPlugin()
  {
    SkipUnlessEnabled();
    File.WriteAllText(LogPath, "");

    using var registry = new RegistryGuard();
    using var box      = SuperMemoSandbox.Create(SmRoot);
    using var desktop  = new HiddenDesktop();
    using var app      = new SmaAppHarness(desktop, box.ExePath);
    foreach (var plugin in AllPlugins)
      app.SeedPlugin(plugin);

    using var stopAnswering = new CancellationTokenSource();
    var events   = new List<string>();
    var answerer = StartupPromptAnswerer.Start(desktop.Handle, processId: 0, stopAnswering.Token, events);

    app.Launch($"-c \"{box.KnoPath}\"");

    var log = await WaitForLogAsync(app, l => l.Contains("window pointer became available") && PluginsStarted(l) is not null,
                                    TimeSpan.FromSeconds(180), TestContext.Current.CancellationToken);
    await Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken); // let plugins react to SuperMemo having started
    log = app.ReadLog();

    var windows = StartupPromptAnswerer.Windows(desktop.Handle, 0).Select(StartupPromptAnswerer.Describe).ToList();
    stopAnswering.Cancel();
    answerer.Join();

    lock (events)
      Step(string.Join(Environment.NewLine, events));
    Step("windows: " + string.Join(", ", windows) + Environment.NewLine + log);

    Assert.Contains("SuperMemo started and hooked", log);
    Assert.Contains("TElWind (sm20)", windows);
    Assert.DoesNotContain(windows, w => w.StartsWith("ConsoleWindowClass", StringComparison.Ordinal));
    Assert.Equal((AllPlugins.Length, 0), PluginsStarted(log));
    AssertNoProblems(log);
  }

  [Fact(Timeout = 300_000)]
  public async Task FullSession_WithThemesEnabled_RebuildsTheExeBeforeSuperMemoStarts_AndSuperMemoRunsFromIt()
  {
    SkipUnlessEnabled();
    Assert.SkipWhen(Themes.OriginalExe.Path is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");
    File.WriteAllText(LogPath, "");

    using var registry = new RegistryGuard();
    using var box      = SuperMemoSandbox.Create(SmRoot);
    using var desktop  = new HiddenDesktop();
    using var app      = new SmaAppHarness(desktop, box.ExePath);

    Directory.CreateDirectory(Path.Combine(box.Root, "smcards-backups", "exe"));
    File.Copy(Themes.OriginalExe.Path!, Path.Combine(box.Root, "smcards-backups", "exe", "sm20.exe.original"));
    File.Copy(Themes.OriginalExe.Path!, box.ExePath, overwrite: true); // the copy of the real exe may already be themed
    app.SeedPlugin("SuperMemoAssistant.Plugins.Themes");
    app.SeedPluginConfig("SuperMemoAssistant.Plugins.Themes", new global::SuperMemoAssistant.Plugins.Themes.ThemesCfg { Enabled = true, ActiveThemeId = "nord", InstalledThemeIds = ["nord"] });

    using var stopAnswering = new CancellationTokenSource();
    var events   = new List<string>();
    var answerer = StartupPromptAnswerer.Start(desktop.Handle, processId: 0, stopAnswering.Token, events);

    app.Launch($"-c \"{box.KnoPath}\"");

    var log = await WaitForLogAsync(app, l => l.Contains("SuperMemo started and hooked"), TimeSpan.FromSeconds(180), TestContext.Current.CancellationToken);
    await Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    log = app.ReadLog();

    var windows = StartupPromptAnswerer.Windows(desktop.Handle, 0).Select(StartupPromptAnswerer.Describe).ToList();
    stopAnswering.Cancel();
    answerer.Join();

    Step("windows: " + string.Join(", ", windows) + Environment.NewLine + log);

    int At(string text) => log.IndexOf(text, StringComparison.Ordinal);

    var registered  = At("Launch hook Themes registered");
    var initialized = At("have finished initializing");
    var rebuilt     = At("Rebuilt sm20.exe from the original");
    var started     = At("SuperMemo started and hooked");

    Assert.True(registered >= 0, "the hook must be registered");
    Assert.True(initialized > registered, "SMA must wait for plugin initialization after the hook is registered");
    Assert.True(rebuilt > initialized, "the exe must be rebuilt after that");
    Assert.True(started > rebuilt, "SuperMemo must start only after the rebuild");
    Assert.Contains("TElWind (sm20)", windows);

    var state = global::SuperMemoAssistant.Themes.Exe.ExeBuilder.ReadState(box.ExePath);

    Assert.True(state.Patched && state.Elements);
    Assert.Contains("SMC_NORD", state.Styles.Keys);
    AssertNoProblems(log);

    // End the SuperMemo process this test started (by its exact id) and watch the after-exit hook run.
    var smPid = int.Parse(SuperMemoPidRegex().Match(log).Groups[1].Value);

    using (var superMemo = System.Diagnostics.Process.GetProcessById(smPid))
    {
      Assert.Equal(box.ExePath, superMemo.MainModule!.FileName, ignoreCase: true);
      superMemo.Kill();
    }

    log = await WaitForLogAsync(app, l => l.Contains("Launch hook Themes (after exit) finished"), TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);
    Step(log);

    Assert.Contains("Launch hook Themes (after exit) finished", log);
    Assert.DoesNotContain("did not finish within", log);
  }

  [GeneratedRegex(@"SuperMemo started and hooked, pId: (\d+)")]
  private static partial Regex SuperMemoPidRegex();

  private static void SkipUnlessEnabled()
  {
    Assert.SkipUnless(Environment.GetEnvironmentVariable("SMA_E2E") == "1", "Set SMA_E2E=1 to run the SMA end-to-end tests.");
    Assert.SkipUnless(File.Exists(SmaAppHarness.AppExe) && SuperMemoLocation.Exe is not null,
                      "The built app (artifacts\\app-dev) or sm20.exe is missing.");
  }

  private static async Task<string> WaitForLogAsync(SmaAppHarness app, Func<string, bool> done, TimeSpan timeout, CancellationToken token)
  {
    var deadline = DateTime.UtcNow + timeout;
    var log      = "";
    while (DateTime.UtcNow < deadline)
    {
      log = app.ReadLog();
      if (done(log))
        return log;

      if (app.HasExited)
      {
        Step("SMA exited early; log:" + Environment.NewLine + app.ReadLog());
        Assert.Fail("SMA exited early; see " + LogPath);
      }
      await Task.Delay(1000, token);
    }

    Step("timed out; log so far:" + Environment.NewLine + log);
    return log;
  }

  private static (int Started, int Failed)? PluginsStarted(string log)
  {
    var match = StartedRegex().Match(log);
    return match.Success ? (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)) : null;
  }

  private static void AssertNoProblems(string log)
  {
    Assert.DoesNotContain("[ERR]", log);
    Assert.DoesNotContain("[FTL]", log);
    Assert.DoesNotContain("crashed", log);
  }

  private static void Step(string message) =>
    File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");

  [GeneratedRegex(@"(\d+) started successfully, (\d+) failed to start")]
  private static partial Regex StartedRegex();
}
