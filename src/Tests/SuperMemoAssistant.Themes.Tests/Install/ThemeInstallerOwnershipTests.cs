// Checks what the installer owns: it removes only its own styles, restores only what it engaged, and refuses to act while SuperMemo runs.
using SuperMemoAssistant.Themes.Exe;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Install;

public class ThemeInstallerOwnershipTests
{
  private static void RequireOriginal() => Assert.SkipWhen(GoldenData.Sm20OriginalPath is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

  [Fact]
  public void RemovingATheme_RemovesOnlyStylesThisToolInstalled_AndKeepsTheActiveOne()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var first  = h.Launch(InstallerHarness.Settings("nord", "tender"));
    var second = h.Launch(InstallerHarness.Settings("nord"), first.State);
    var status = h.Installer.ReadStatus(h.Install);

    Assert.Contains($"Removed the window style '{InstallerHarness.NameOf("tender")}'.", second.Actions);
    Assert.DoesNotContain(status.Styles, s => s.Name == InstallerHarness.NameOf("tender"));
    Assert.Contains(status.Styles, s => s.Name == "Nord");
    Assert.Equal(["SMC_NORD"], second.State.ManagedStyleResources);

    var third = h.Launch(InstallerHarness.Settings("tender"), second.State); // the active theme moved on, so Nord may go

    Assert.Contains("Removed the window style 'Nord'.", third.Actions);
  }

  [Fact]
  public void AStyleTheActiveSlotStillUses_IsKeptWithAWarning()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var first = h.Launch(InstallerHarness.Settings("nord", "tender"));

    h.Write(@"systems\coll\collection.ini", $"[Defaults]\r\nDark Theme={InstallerHarness.NameOf("tender")}\r\n");

    var second = h.Launch(InstallerHarness.Settings("nord"), first.State);

    Assert.Contains(second.Warnings, w => w.Contains($"'{InstallerHarness.NameOf("tender")}' is active"));
    Assert.Contains("SMC_TENDER", second.State.ManagedStyleResources);
    Assert.Contains(h.Installer.ReadStatus(h.Install).Styles, s => s.Name == InstallerHarness.NameOf("tender"));
  }

  [Fact]
  public void StylesAddedByAnotherTool_AreNeverRemoved()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    h.Launch(InstallerHarness.Settings("nord", "tender")); // installed, but the caller forgot the state

    var second = h.Launch(InstallerHarness.Settings("nord-light"), new AppliedState());
    var names  = h.Installer.ReadStatus(h.Install).Styles.Select(s => s.Name).ToList();

    Assert.Contains("Nord", names);
    Assert.Contains(InstallerHarness.NameOf("tender"), names);
    Assert.Contains("Nord Light", names);
    Assert.Equal(["SMC_NORD_LIGHT"], second.State.ManagedStyleResources);
  }

  [Fact]
  public void Disabling_RestoresTheOriginalExeByteForByte_AndClearsTheState()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var first    = h.Launch(InstallerHarness.Settings("nord", "tender"));
    var disabled = h.Launch(new ThemeSettings { Enabled = false }, first.State);

    Assert.False(disabled.State.Engaged);

    Assert.Contains("Restored the original sm20.exe.", disabled.Actions);
    Assert.Equal(File.ReadAllBytes(GoldenData.Sm20OriginalPath!), h.ExeBytes());
    Assert.Equal(new AppliedState(), disabled.State);
    Assert.False(h.Launch(new ThemeSettings { Enabled = false }, disabled.State).Changed);
  }

  [Fact]
  public void Disabled_AndNeverEngaged_LeavesAnExeThatAnotherToolChangedAlone()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    h.Launch(InstallerHarness.Settings("nord")); // stands for smcards: it changed the exe, but its state is not ours

    var theirs  = h.ExeBytes();
    var report  = h.Launch(new ThemeSettings { Enabled = false }, new AppliedState());

    Assert.False(report.Changed);
    Assert.Equal(theirs, h.ExeBytes());
    Assert.False(report.State.Engaged);
  }

  [Fact]
  public void Enabling_MarksTheToolAsEngaged_SoDisablingLaterRestoresTheExe()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var enabled = h.Launch(new ThemeSettings { Enabled = true });

    Assert.True(enabled.State.Engaged);
    Assert.True(h.Installer.ReadStatus(h.Install).ElementsThemed);

    h.Launch(new ThemeSettings { Enabled = false }, enabled.State);

    Assert.Equal(File.ReadAllBytes(GoldenData.Sm20OriginalPath!), h.ExeBytes());
  }

  [Fact]
  public void WhileSuperMemoRuns_NothingIsWritten()
  {
    RequireOriginal();

    using var h = new InstallerHarness { Running = ["sm20.exe"] };

    var before = h.ExeBytes();
    var report = h.Launch(InstallerHarness.Settings("nord"));

    Assert.Contains(report.Warnings, w => w.Contains("already running"));
    Assert.False(report.Changed);
    Assert.Equal(before, h.ExeBytes());
    Assert.Empty(h.BackupFiles());
  }

  [Fact]
  public void AnUnknownThemeId_IsSkippedWithAWarning_AndTheRestStillApplies()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var report = h.Launch(InstallerHarness.Settings("nord", "no-such-theme"));

    Assert.Contains(report.Warnings, w => w.Contains("'no-such-theme' is not in the library"));
    Assert.Contains(h.Installer.ReadStatus(h.Install).Styles, s => s.Name == "Nord");
  }

  [Fact]
  public void WhenTheBuildIsNotTheSupportedOne_TheLivePatchIsSkippedWithAWarning()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var pe = new PeImage(h.ExeBytes());

    pe.Write(LiveThemePatch.CallSites[0], new byte[5]); // a call site that no longer matches this build
    File.WriteAllBytes(h.Exe, pe.Bytes);

    var report = h.Launch(InstallerHarness.Settings("nord"));
    var status = h.Installer.ReadStatus(h.Install);

    Assert.Contains(report.Warnings, w => w.Contains("Live theme switching was skipped"));
    Assert.False(status.LivePatched);
    Assert.True(status.ElementsThemed);
  }

  [Fact]
  public void TurningLiveSwitchingAndElementsOff_RebuildsWithoutThem()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var on  = h.Launch(InstallerHarness.Settings("nord"));
    var off = h.Launch(InstallerHarness.Settings("nord") with { LiveSwitching = false, ThemeElements = false }, on.State);
    var status = h.Installer.ReadStatus(h.Install);

    Assert.True(off.Changed);
    Assert.False(status.LivePatched);
    Assert.False(status.ElementsThemed);
    Assert.Contains(status.Styles, s => s.Name == "Nord");
  }
}
