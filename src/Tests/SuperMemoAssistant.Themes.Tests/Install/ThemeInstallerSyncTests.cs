// Checks the after-exit sync: cards follow the theme SuperMemo saved on exit, and the sync never runs when it should not.
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Install;

public class ThemeInstallerSyncTests
{
  private static void RequireOriginal() => Assert.SkipWhen(GoldenData.Sm20OriginalPath is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

  [Fact]
  public void AfterExit_CardsFollowTheThemeThatWasPickedInsideSuperMemo()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var settings = InstallerHarness.Settings("nord", "tender");
    var launched = h.Launch(settings);
    var tender   = InstallerHarness.Library.TryGet("tender")!;

    h.Write(@"systems\coll\collection.ini", $"[Defaults]\r\nDark Theme={InstallerHarness.NameOf("tender")}\r\n"); // what SuperMemo writes when it exits

    var report = h.Installer.SyncAfterExit(h.Install, settings, launched.State);

    Assert.True(report.Changed);
    Assert.Empty(report.Warnings);
    Assert.Contains(tender.Roles["bg"], h.Read(@"bin\DarkMode.css"), StringComparison.OrdinalIgnoreCase);
    Assert.Contains(tender.Roles["bg"], h.Read(@"bin\supermemo.css"), StringComparison.OrdinalIgnoreCase);
    Assert.Equal(launched.State, report.State);
  }

  [Fact]
  public void AfterExit_ASecondSyncChangesNothing()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var settings = InstallerHarness.Settings("nord");
    var launched = h.Launch(settings);

    Assert.False(h.Installer.SyncAfterExit(h.Install, settings, launched.State).Changed);
  }

  [Fact]
  public void AfterExit_WhenDisabled_DoesNothing()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var before = h.Read(@"bin\DarkMode.css");
    var report = h.Installer.SyncAfterExit(h.Install, new ThemeSettings { Enabled = false }, new AppliedState());

    Assert.False(report.Changed);
    Assert.Equal(before, h.Read(@"bin\DarkMode.css"));
    Assert.Empty(h.BackupFiles());
  }

  [Fact]
  public void AfterExit_WhileAnotherSuperMemoRuns_IsSkippedWithAWarning()
  {
    RequireOriginal();

    using var h = new InstallerHarness { Running = ["sm20.exe"] };

    var report = h.Installer.SyncAfterExit(h.Install, InstallerHarness.Settings("nord"), new AppliedState());

    Assert.Contains(report.Warnings, w => w.Contains("Another SuperMemo is running"));
    Assert.False(report.Changed);
    Assert.Empty(h.BackupFiles());
  }

  [Fact]
  public void AfterExit_AStyleThatIsNotInTheExe_IsReportedAndLeftAlone()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var settings = InstallerHarness.Settings("nord");
    var launched = h.Launch(settings);

    h.Write(@"systems\coll\collection.ini", "[Defaults]\r\nDark Theme=Gone Style\r\n");

    var darkCss = h.Read(@"bin\DarkMode.css");
    var report  = h.Installer.SyncAfterExit(h.Install, settings, launched.State);

    Assert.Contains(report.Warnings, w => w.Contains("'Gone Style' is not in sm20.exe"));
    Assert.Equal(darkCss, h.Read(@"bin\DarkMode.css"));
  }

  [Fact]
  public void AfterExit_BuiltInStylesGetCardColorsReadFromTheStyleItself()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var settings = InstallerHarness.Settings("nord");
    var launched = h.Launch(settings);
    var builtin  = h.Installer.ReadStatus(h.Install).Styles.First(s => s.BuiltIn && s.Name == "Windows10 Dark");

    h.Write(@"systems\coll\collection.ini", $"[Defaults]\r\nDark Theme={builtin.Name}\r\n");

    var report = h.Installer.SyncAfterExit(h.Install, settings, launched.State);

    Assert.True(report.Changed);
    Assert.DoesNotContain(InstallerHarness.Library.TryGet("nord")!.Roles["bg"], h.Read(@"bin\DarkMode.css"), StringComparison.OrdinalIgnoreCase);
  }
}
