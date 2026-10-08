// Drives the real SMA user interface on a hidden desktop, as a user would, and saves a screenshot of each step.
namespace SuperMemoAssistant.Tests.E2E;

using System.Text.Json;
using SuperMemoAssistant.PluginFeed;
using SuperMemoAssistant.Tests.Plugins;
using Xunit;

[Collection("SuperMemo E2E")]
public sealed class UiWalkthroughTests
{
  private const string Setup = "SMA Setup";
  private const string Picker = "Collection Selection";

  private const string FirstPlugin = "Books";

  private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "sma-ui-e2e.log");

  private static string SmRoot => SuperMemoLocation.Root ?? "";

  private static string PublishedFeed => Path.Combine(SmaAppHarness.RepoRoot, "artifacts", "feed");

  /// <summary>
  ///   A first run: the license, the SuperMemo executable through the Open dialog, the collection import, a plugin install
  ///   from the plugin feed (the first one in the list), the collection picker, and the session that opens SuperMemo with the chosen collection.
  /// </summary>
  [Fact(Timeout = 600_000)]
  public async Task FirstRun_SetupWizard_CollectionPicker_AndPluginInstall()
  {
    Assert.SkipUnless(Environment.GetEnvironmentVariable("SMA_E2E") == "1", "Set SMA_E2E=1 to run the UI walkthrough.");
    Assert.SkipUnless(SuperMemoLocation.Exe is not null, "sm20.exe is not found (set SMA_SM_ROOT).");
    Assert.SkipUnless(File.Exists(UiDriver.AgentExe) && File.Exists(Path.Combine(PublishedFeed, StaticFeedWriter.CatalogFile)),
                      "Build the solution and run build\\pack-plugins.ps1 first.");
    File.WriteAllText(LogPath, "");

    using var registry = new RegistryGuard();
    using var box      = SuperMemoSandbox.Create(SmRoot);
    using var desktop  = new HiddenDesktop();
    using var ui       = new UiDriver(desktop, Path.Combine(SmaAppHarness.RepoRoot, "artifacts", "ui-screenshots", "first-run"), Step, TestContext.Current.CancellationToken);
    await using var feed = ServeLocalFeed();
    using var app      = new SmaAppHarness(desktop, null);
    WriteUpdateSources(app, feed);

    using var stopAnswering = new CancellationTokenSource();
    var events   = new List<string>();
    var answerer = StartupPromptAnswerer.Start(desktop.Handle, processId: 0, stopAnswering.Token, events);

    app.Launch("");
    try
    {
      Walk(ui, box, app, desktop);
    }
    finally
    {

      foreach (var hwnd in StartupPromptAnswerer.Windows(desktop.Handle, app.ProcessId).Where(h => StartupPromptAnswerer.ClassName(h).StartsWith("HwndWrapper")))
        ui.Screenshot(hwnd, "final-window");
      Step($"SMA exited: {app.HasExited}; windows: " +
           string.Join(", ", StartupPromptAnswerer.Windows(desktop.Handle, 0).Select(StartupPromptAnswerer.Describe)));
      Step("log:" + Environment.NewLine + app.ReadLog());
      stopAnswering.Cancel();
      answerer.Join();
    }
  }

  private static void Walk(UiDriver ui, SuperMemoSandbox box, SmaAppHarness app, HiddenDesktop desktop)
  {
    ui.WaitForWindow(Setup);
    Screen(ui, Setup, "license");
    ui.Click(Setup, "I have read and agree", "CheckBox");
    ui.Click(Setup, "Next", "Button");

    Assert.True(ui.WaitFor(Setup, "SM .exe Path"), "The SuperMemo screen did not appear.");
    ui.Click(Setup, "Browse", "Button");
    ChooseFile(ui, box.ExePath);
    Assert.True(WaitUntil(() => ui.Read(Setup, "", "Edit")?["value"]?.GetValue<string>() == box.ExePath),
                "The path box does not show the chosen executable.");
    Screen(ui, Setup, "supermemo");
    ui.Click(Setup, "Next", "Button");

    Screen(ui, Setup, "importing");
    ui.Click(Setup, "Next", "Button");

    // The list shows only the rows that fit the window, so the test installs the first plugin of the feed (listed by name).
    Assert.True(ui.WaitFor(Setup, FirstPlugin, 60_000), "The plugin list from the feed did not appear.");
    Screen(ui, Setup, "plugins");

    ui.Click(Setup, "Install", "Button", within: FirstPlugin);
    Assert.True(WaitUntil(() => ui.Read(Setup, "Next", "Button")?["enabled"]?.GetValue<bool>() == true, 180_000),
                $"Installing {FirstPlugin} from the feed did not enable Next.");
    Screen(ui, Setup, "plugin-installed");
    ui.Click(Setup, "Next", "Button");

    ui.WaitForWindow(Picker);
    Screen(ui, Picker, "collections-empty");
    ui.Click(Picker, "Browse", "Button");
    ChooseFile(ui, box.KnoPath);

    // Choosing a collection file in the picker adds it to the list and opens it at once.
    Assert.True(WaitUntil(() => app.ReadLog().Contains("SuperMemo started and hooked"), 180_000),
                "The session with the chosen collection did not come up.");
    Assert.True(WaitUntil(() => app.ReadLog().Contains("1 started successfully, 0 failed to start"), 60_000),
                "The plugin installed from the feed is not running.");

    nint elementWindow = 0;
    Assert.True(WaitUntil(() => (elementWindow = StartupPromptAnswerer.Windows(desktop.Handle, 0)
                                                                        .FirstOrDefault(h => StartupPromptAnswerer.ClassName(h) == "TElWind")) != 0,
                          60_000),
                "The SuperMemo element window did not appear.");
    ui.Screenshot(elementWindow, "supermemo-session");

    // NuGet reports the loopback feed's plain HTTP as an error. Published feeds use HTTPS, so only that line is allowed.
    Assert.DoesNotContain(app.ReadLog().Split('\n'), line => line.Contains("[ERR]") && !line.Contains("'HTTP' source"));
  }

  private static bool WaitUntil(Func<bool> condition, int timeoutMs = 30_000)
  {
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (!condition())
    {
      if (DateTime.UtcNow > deadline)
        return false;

      Thread.Sleep(250);
    }

    return true;
  }

  private static void Screen(UiDriver ui, string window, string step)
  {
    ui.Screenshot(window, step);
    Step($"elements on {step}:{Environment.NewLine}{ui.Describe(window)}");
  }

  /// <summary>Completes the Open dialog that the app just opened.</summary>
  private static void ChooseFile(UiDriver ui, string path)
  {
    ui.WaitForWindow("Open");

    ui.SetValueById("Open", "1148", path);
    ui.Click("Open", "Open", "Button");
  }

  private static LoopbackFeedServer ServeLocalFeed()
  {
    var root = Path.Combine(Path.GetTempPath(), "sma-ui-feed-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    var server = LoopbackFeedServer.Create(root, TimeSpan.FromMinutes(10));

    // The published feed names its public URL in every resource, so it is rebuilt for the loopback server.
    var catalog  = FeedCatalog.Load(Path.Combine(PublishedFeed, StaticFeedWriter.CatalogFile));
    var packages = Directory.GetFiles(Path.Combine(PublishedFeed, "nuget", "flatcontainer"), "*.nupkg", SearchOption.AllDirectories);
    StaticFeedWriter.Write(packages, catalog, server.BaseUrl, root, DateTime.UtcNow);
    return server;
  }

  private static void WriteUpdateSources(SmaAppHarness app, LoopbackFeedServer feed)
  {
    var dir = Path.Combine(app.DataDir, "Configs", "Core");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "CoreCfg.json"), JsonSerializer.Serialize(new
    {
      Updates = new
      {
        EnableCoreUpdates      = false,
        PluginsUpdateUrl       = new Uri(feed.BaseUrl, StaticFeedWriter.CatalogFile).AbsoluteUri,
        PluginsUpdateNuGetUrls = new[] { new Uri(feed.BaseUrl, StaticFeedWriter.ServiceIndexFile).AbsoluteUri },
      },
    }));
  }

  private static void Step(string message) =>
    File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
}
