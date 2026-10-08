// Checks the plugin's launch hook end to end against a sandbox install: settings in, exe and collection changed, state saved, report out.
namespace SuperMemoAssistant.Tests.Themes;

using SuperMemoAssistant.Plugins.Themes;
using SuperMemoAssistant.SMA;
using global::SuperMemoAssistant.Themes;
using Xunit;

public class ThemesLaunchHookTests
{
  private static readonly ThemeLibrary Library = ThemeLibrary.Load();

  private static ThemesLaunchHook HookFor(InMemoryStore store, IReadOnlyList<string>? running = null) =>
    new(new ThemeInstaller(Library, () => running ?? []), store);

  private static void RequireOriginal() => Assert.SkipWhen(OriginalExe.Path is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

  private static InMemoryStore Enabled(string active = "nord", params string[] installed)
  {
    var store = new InMemoryStore();

    store.Update(c =>
    {
      c.Enabled           = true;
      c.ActiveThemeId     = active;
      c.InstalledThemeIds = [.. installed];
    });

    return store;
  }

  [Fact]
  public void NeverEnabled_TheHookTouchesNothingAndSavesNothing_EvenWithoutAnExe()
  {
    using var box   = new ThemeSandbox(withExe: false);
    var       store = new InMemoryStore();
    var       hook  = HookFor(store);

    var before = hook.BeforeLaunch(box.Info);
    var after  = hook.AfterExit(box.Info);

    Assert.Empty(before.Actions);
    Assert.Empty(before.Warnings);
    Assert.Empty(after.Actions);
    Assert.Equal(0, store.Saves);
  }

  [Fact]
  public void Enabled_BeforeLaunchAppliesTheThemeAndSavesWhatItDid()
  {
    RequireOriginal();

    using var box   = new ThemeSandbox(withExe: true);
    var       store = Enabled("nord", "tender");

    var result = HookFor(store).BeforeLaunch(box.Info);
    var saved  = store.Load();

    Assert.Empty(result.Warnings);
    Assert.Contains(result.Actions, a => a.StartsWith("Rebuilt sm20.exe"));
    Assert.Contains(result.Actions, a => a.Contains("Activated 'Nord'"));
    Assert.True(saved.Engaged);
    Assert.Equal("nord", saved.AppliedActiveThemeId);
    Assert.Equal(["SMC_NORD", "SMC_TENDER"], saved.ManagedStyleResources.Order());
    Assert.Contains("Dark Theme=Nord", box.Read(@"systems\coll\collection.ini"));
    Assert.Equal(1, store.Saves);
  }

  [Fact]
  public void ASecondLaunch_ReportsNothingAndDoesNotSaveAgain()
  {
    RequireOriginal();

    using var box   = new ThemeSandbox(withExe: true);
    var       store = Enabled("nord");
    var       hook  = HookFor(store);

    hook.BeforeLaunch(box.Info);

    var again = hook.BeforeLaunch(box.Info);

    Assert.Empty(again.Actions);
    Assert.Empty(again.Warnings);
    Assert.Equal(1, store.Saves);
  }

  [Fact]
  public void AfterExit_RecolorsCardsToTheThemeThatWasPickedInSuperMemo()
  {
    RequireOriginal();

    using var box   = new ThemeSandbox(withExe: true);
    var       store = Enabled("nord", "tender");
    var       hook  = HookFor(store);
    var       tender = Library.TryGet("tender")!;

    hook.BeforeLaunch(box.Info);
    box.Write(@"systems\coll\collection.ini", $"[Defaults]\r\nDark Theme={tender.Name}\r\n");

    var result = hook.AfterExit(box.Info);

    Assert.Contains(result.Actions, a => a.StartsWith("Updated"));
    Assert.Contains(tender.Roles["bg"], box.Read(@"bin\DarkMode.css"), StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void WhileSuperMemoRuns_TheHookReturnsAWarning_InsteadOfThrowing()
  {
    RequireOriginal();

    using var box = new ThemeSandbox(withExe: true);

    var result = HookFor(Enabled(), ["sm20.exe"]).BeforeLaunch(box.Info);

    Assert.Contains(result.Warnings, w => w.Contains("already running"));
    Assert.Empty(result.Actions);
    Assert.Equal(File.ReadAllBytes(OriginalExe.Path!), File.ReadAllBytes(box.ExePath));
  }

  [Fact]
  public void TurningThemesOff_RestoresTheOriginalExeOnTheNextLaunch()
  {
    RequireOriginal();

    using var box   = new ThemeSandbox(withExe: true);
    var       store = Enabled();
    var       hook  = HookFor(store);

    hook.BeforeLaunch(box.Info);
    Assert.NotEqual(File.ReadAllBytes(OriginalExe.Path!), File.ReadAllBytes(box.ExePath));

    store.Update(c => c.Enabled = false);

    var result = hook.BeforeLaunch(box.Info);

    Assert.Contains("Restored the original sm20.exe.", result.Actions);
    Assert.Equal(File.ReadAllBytes(OriginalExe.Path!), File.ReadAllBytes(box.ExePath));
    Assert.False(store.Load().Engaged);
  }

  [Fact]
  public async Task ThroughTheCoreRunner_WarningsBecomeNotificationsAndActionsAreOnlyLogged()
  {
    RequireOriginal();

    using var box      = new ThemeSandbox(withExe: true);
    var       notified = new List<string>();
    var       runner   = new LaunchHookRunner(notified.Add);
    var       hook     = HookFor(Enabled(), ["sm20.exe"]);

    runner.Register("Themes", hook.BeforeLaunch, hook.AfterExit);

    var outcomes = await runner.RunBeforeLaunchAsync(box.Info, TimeSpan.FromMinutes(1));

    Assert.Single(outcomes);
    Assert.Single(notified);
    Assert.StartsWith("Themes: ", notified[0]);
    Assert.Contains("already running", notified[0]);
  }
}
