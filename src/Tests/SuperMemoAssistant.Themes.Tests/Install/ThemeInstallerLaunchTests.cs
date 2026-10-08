// Checks the first and repeated launches: the exe is rebuilt, the collection settings are written once, and a repeat changes nothing.
using SuperMemoAssistant.Themes.Exe;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Install;

public class ThemeInstallerLaunchTests
{
  private static void RequireOriginal() => Assert.SkipWhen(GoldenData.Sm20OriginalPath is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

  [Fact]
  public void FirstLaunch_InstallsStyles_ActivatesTheTheme_AndWritesCardFiles()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var report = h.Launch(InstallerHarness.Settings("nord", "tender"));
    var status = h.Installer.ReadStatus(h.Install);
    var nord   = InstallerHarness.Library.TryGet("nord")!;

    Assert.Empty(report.Warnings);
    Assert.True(report.Changed);
    Assert.True(status.ElementsThemed && status.LivePatched);
    Assert.Contains(status.Styles, s => s.Name == "Nord" && !s.BuiltIn);
    Assert.Contains(status.Styles, s => s.Name == InstallerHarness.NameOf("tender") && !s.BuiltIn);
    Assert.Equal("Nord", status.ActiveDark);
    Assert.True(status.DarkMode);
    Assert.Equal("nord", report.State.ActiveThemeId);
    Assert.Equal(["SMC_NORD", "SMC_TENDER"], report.State.ManagedStyleResources.Order());
    Assert.Contains(nord.Roles["bg"], h.Read(@"bin\DarkMode.css"), StringComparison.OrdinalIgnoreCase);
    Assert.Contains(nord.Roles["bg"], h.Read(@"bin\supermemo.css"), StringComparison.OrdinalIgnoreCase);
    Assert.True(File.Exists(Path.Combine(h.Install.BinFolder, "themes", "Nord.css")));
    Assert.True(Directory.GetFiles(Path.Combine(h.Install.BinFolder, "themes")).Length > 40, "built-in styles get stylesheets too");
    Assert.Equal(h.ProgramHash(), ExeBuilder.ProgramHash(File.ReadAllBytes(GoldenData.Sm20OriginalPath!)));
  }

  [Fact]
  public void FirstLaunch_BacksUpTheOriginalExeAndEveryChangedFile()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    h.Launch(InstallerHarness.Settings("nord"));

    var backups = h.BackupFiles();

    Assert.Contains(@"exe\sm20.exe.original", backups);
    Assert.Contains(backups, b => b.EndsWith(@"collection.ini"));
    Assert.Contains(backups, b => b.EndsWith(@"supermemo.ini"));
    Assert.Contains(backups, b => b.EndsWith("manifest.json"));
  }

  [Fact]
  public void SecondLaunch_WithTheSameSettings_ChangesNothing()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var first   = h.Launch(InstallerHarness.Settings("nord", "tender"));
    var exe     = h.ExeBytes();
    var ini     = h.Read(@"systems\coll\collection.ini");
    var backups = h.BackupFiles();
    var second  = h.Launch(InstallerHarness.Settings("nord", "tender"), first.State);

    Assert.False(second.Changed, string.Join("; ", second.Actions));
    Assert.Empty(second.Warnings);
    Assert.Equal(exe, h.ExeBytes());
    Assert.Equal(ini, h.Read(@"systems\coll\collection.ini"));
    Assert.Equal(first.State.ActiveThemeId, second.State.ActiveThemeId);
    Assert.Equal(first.State.ManagedStyleResources, second.State.ManagedStyleResources);
    Assert.Equal(backups.Where(b => b.StartsWith("exe")), h.BackupFiles().Where(b => b.StartsWith("exe")));
  }

  [Fact]
  public void LaterLaunches_KeepAThemePickedInsideSuperMemo()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var first = h.Launch(InstallerHarness.Settings("nord", "tender"));

    h.Write(@"systems\coll\collection.ini", $"[Defaults]\r\nTheme=Windows\r\nDark Theme={InstallerHarness.NameOf("tender")}\r\n");
    h.Launch(InstallerHarness.Settings("nord", "tender"), first.State);

    Assert.Contains($"Dark Theme={InstallerHarness.NameOf("tender")}", h.Read(@"systems\coll\collection.ini"));
  }

  [Fact]
  public void ChangingTheActiveTheme_WritesTheNewSlotOnce()
  {
    RequireOriginal();

    using var h = new InstallerHarness();

    var first  = h.Launch(InstallerHarness.Settings("nord"));
    var second = h.Launch(InstallerHarness.Settings("nord-light", "nord"), first.State);

    Assert.Equal("nord-light", second.State.ActiveThemeId);
    Assert.Contains("Theme=Nord Light", h.Read(@"systems\coll\collection.ini"));
    Assert.Contains("Dark mode=0", h.Read(@"bin\supermemo.ini"));
  }
}
