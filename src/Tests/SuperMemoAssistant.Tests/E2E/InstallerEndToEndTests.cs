// Installs the Velopack Setup.exe into a throwaway profile on a hidden desktop and runs the installed SMA with its bundled plugins.
namespace SuperMemoAssistant.Tests.E2E;

using SuperMemoAssistant.SuperMemo.Hooks;
using Xunit;

[Collection("SuperMemo E2E")]
public sealed class InstallerEndToEndTests
{
  private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "sma-installer-e2e.log");

  /// <summary>Opt-in (SMA_E2E=1). Needs artifacts\releases from build\pack.ps1.</summary>
  [Fact(Timeout = 300_000)]
  public async Task Setup_InstallsSma_AndTheInstalledAppStartsItsBundledPlugins()
  {
    var setup = Path.Combine(SmaAppHarness.RepoRoot, "artifacts", "releases", "SuperMemoAssistant-win-Setup.exe");
    Assert.SkipUnless(Environment.GetEnvironmentVariable("SMA_E2E") == "1", "Set SMA_E2E=1 to run the installer end-to-end test.");
    Assert.SkipUnless(SuperMemoLocation.Exe is not null, "sm20.exe is not found (set SMA_SM_ROOT).");
    Assert.SkipUnless(File.Exists(setup), "Run build\\pack.ps1 first (artifacts\\releases\\SuperMemoAssistant-win-Setup.exe).");

    File.WriteAllText(LogPath, "");
    using var desktop = new HiddenDesktop();
    using var app     = new SmaAppHarness(desktop, SuperMemoLocation.Exe ?? "");
    using var registry = new RegistryGuard();

    var installDir = Path.Combine(app.Profile, "AppData", "Local", "SuperMemoAssistant");
    var setupLog   = Path.Combine(app.Profile, "setup.log");

    // Velopack resolves shortcut folders through the shell, not the redirected environment: remove what this run adds.
    using var shortcuts = new ShortcutGuard();

    var exitCode = await RunToExitAsync(desktop, app, setup, $"--silent --installto \"{installDir}\" --log \"{setupLog}\"", TimeSpan.FromSeconds(180));
    Step($"setup exit code {exitCode}{Environment.NewLine}{(File.Exists(setupLog) ? File.ReadAllText(setupLog) : "(no setup log)")}");
    desktop.TerminateProcesses(); // Setup starts the app after installing; this test starts its own instance

    var installedExe = Path.Combine(installDir, "current", "SuperMemoAssistant.exe");
    Assert.Equal(0u, exitCode);
    Assert.True(File.Exists(installedExe), "Setup did not install " + installedExe);

    var bundled = Directory.GetDirectories(Path.Combine(installDir, "current", "Plugins")).Select(Path.GetFileName).ToList();
    Step("bundled plugins: " + string.Join(", ", bundled));
    Assert.Contains("SuperMemoAssistant.Plugins.Import", bundled);
    Assert.Contains("SuperMemoAssistant.Plugins.PDF", bundled);

    app.Launch(installedExe, "");
    var deadline = DateTime.UtcNow.AddSeconds(90);
    string log;
    do
    {
      await Task.Delay(1000, TestContext.Current.CancellationToken);
      log = app.ReadLog();
    } while (!log.Contains("started successfully") && !app.HasExited && DateTime.UtcNow < deadline);

    Step(log);
    Assert.Contains($"{bundled.Count} started successfully, 0 failed to start", log);
    Assert.DoesNotContain("[ERR]", log);
    Assert.DoesNotContain("[FTL]", log);
  }

  private static async Task<uint> RunToExitAsync(HiddenDesktop desktop, SmaAppHarness app, string exe, string arguments, TimeSpan timeout)
  {
    var process = NativeProcess.StartSuspended(exe, arguments, Path.GetDirectoryName(exe), desktop.StartupDesktop, app.Environment);
    try
    {
      desktop.Track(process.ProcessHandle);
      NativeProcess.Resume(process.ThreadHandle);

      using var handle = System.Diagnostics.Process.GetProcessById(process.ProcessId);
      using var cts    = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
      cts.CancelAfter(timeout);
      await handle.WaitForExitAsync(cts.Token);
      return (uint)handle.ExitCode;
    }
    finally
    {
      NativeProcess.Close(process.ThreadHandle);
      NativeProcess.Close(process.ProcessHandle);
    }
  }

  private static void Step(string message) =>
    File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
}
