// Checks importing themes through the plugin: the catalog's files, and what the settings window's logic does with the result.
namespace SuperMemoAssistant.Tests.Themes;

using System.Text;
using SuperMemoAssistant.Plugins.Themes;
using SuperMemoAssistant.Plugins.Themes.UI;
using global::SuperMemoAssistant.Themes;
using Xunit;

public sealed class ThemesImportTests : IDisposable
{
  private const string Scheme = """
    system: "base16"
    name: "Imported Dusk"
    variant: "dark"
    palette:
      base00: "1b1b2f"
      base01: "232340"
      base02: "2d2d50"
      base03: "56567a"
      base04: "9a9ac0"
      base05: "d0d0f0"
      base06: "e0e0ff"
      base07: "f0f0ff"
      base08: "e06c75"
      base09: "d19a66"
      base0A: "e5c07b"
      base0B: "98c379"
      base0C: "56b6c2"
      base0D: "61afef"
      base0E: "c678dd"
      base0F: "be5046"
    """;

  private readonly string _root = Path.Combine(Path.GetTempPath(), "sma-themes-import-" + Guid.NewGuid().ToString("N")[..8]);

  public ThemesImportTests() => Directory.CreateDirectory(_root);

  public void Dispose() => Directory.Delete(_root, true);

  private string WriteScheme(string name = "dusk.yaml", string text = Scheme)
  {
    var path = Path.Combine(_root, name);

    File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(text));

    return path;
  }

  private ThemeCatalog NewCatalog() => new(Path.Combine(_root, "plugin-data"));

  [Fact]
  public void TheCatalog_StartsWithTheShippedLibrary_AndCreatesNoFilesUntilAnImport()
  {
    var catalog = NewCatalog();

    Assert.Equal(ThemeLibrary.Load().Entries.Count, catalog.Library.Entries.Count);
    Assert.False(Directory.Exists(Path.Combine(_root, "plugin-data")));
  }

  [Fact]
  public void Import_StoresTheThemeInThePluginsFolder_AndReloadsTheLibraryAndTheInstaller()
  {
    var catalog      = NewCatalog();
    var oldInstaller = catalog.Installer;

    var result = catalog.Import(WriteScheme());

    Assert.Equal("Imported Dusk", Assert.Single(result.Added).Name);
    Assert.NotNull(catalog.Library.TryGet("imported-dusk"));
    Assert.NotSame(oldInstaller, catalog.Installer);
    Assert.True(File.Exists(Path.Combine(_root, "plugin-data", ThemeCatalog.UserLibraryFile)));
    Assert.NotNull(new ThemeCatalog(Path.Combine(_root, "plugin-data")).Library.TryGet("imported-dusk")); // a new session sees it too
  }

  [Fact]
  public void TheViewModel_ShowsAnImportedThemeAtOnce_UncheckedAndKeepingEarlierChoices()
  {
    var vm = new ThemesViewModel(NewCatalog(), new InMemoryStore(), () => null);

    vm.Rows.Single(r => r.Entry.Id == "nord").Install  = true;
    vm.Rows.Single(r => r.Entry.Id == "nord").IsActive = true;

    vm.Import(WriteScheme());

    var imported = vm.Rows.Single(r => r.Entry.Id == "imported-dusk");

    Assert.False(imported.Install);
    Assert.True(vm.Rows.Single(r => r.Entry.Id == "nord").Install);
    Assert.Equal("nord", vm.ActiveThemeId);
    Assert.Contains("Imported 1 theme(s): Imported Dusk", vm.ImportMessage);
  }

  [Fact]
  public void AnImportedThemeCanBeChosen_AndIsSavedLikeAnyOther()
  {
    var store = new InMemoryStore();
    var vm    = new ThemesViewModel(NewCatalog(), store, () => null);

    vm.Import(WriteScheme());
    vm.Rows.Single(r => r.Entry.Id == "imported-dusk").Install  = true;
    vm.Rows.Single(r => r.Entry.Id == "imported-dusk").IsActive = true;
    vm.Enabled = true;
    vm.Save();

    var saved = store.Load();

    Assert.Equal("imported-dusk", saved.ActiveThemeId);
    Assert.Equal(["imported-dusk"], saved.InstalledThemeIds);
    Assert.Equal("imported-dusk", saved.ToSettings().ActiveThemeId);
  }

  [Fact]
  public void AFailedImport_ShowsTheReason_AndChangesNothing()
  {
    var vm     = new ThemesViewModel(NewCatalog(), new InMemoryStore(), () => null);
    var before = vm.Rows.Count;

    File.WriteAllText(Path.Combine(_root, "notes.txt"), "hello");
    vm.Import(Path.Combine(_root, "notes.txt"));

    Assert.StartsWith("Import failed:", vm.ImportMessage);
    Assert.Contains("don't know how to import", vm.ImportMessage);
    Assert.Equal(before, vm.Rows.Count);
  }

  [Fact]
  public void AFolderWithBadSchemes_ReportsWhatWasSkipped()
  {
    var folder = Path.Combine(_root, "schemes");

    Directory.CreateDirectory(folder);
    WriteScheme("schemes\\good.yaml");
    WriteScheme("schemes\\bad.yaml", "name: \"No Palette\"\n");

    var vm = new ThemesViewModel(NewCatalog(), new InMemoryStore(), () => null);

    vm.Import(folder);

    Assert.Contains("Imported 1 theme(s)", vm.ImportMessage);
    Assert.Contains("Skipped 1: bad.yaml", vm.ImportMessage);
  }

  [Fact]
  public void AFolderWithNoThemes_SaysSo()
  {
    var empty = Path.Combine(_root, "empty");

    Directory.CreateDirectory(empty);

    var vm = new ThemesViewModel(NewCatalog(), new InMemoryStore(), () => null);

    vm.Import(empty);

    Assert.Contains("No themes were found there.", vm.ImportMessage);
  }

  [Fact]
  public void TheLaunchHook_UsesTheReloadedInstaller_SoAnImportedThemeCanBeInstalled()
  {
    Assert.SkipWhen(OriginalExe.Path is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box     = new ThemeSandbox(withExe: true);
    var       catalog = NewCatalog();
    var       store   = new InMemoryStore();
    var       hook    = new ThemesLaunchHook(() => catalog.Installer, store);

    catalog.Import(WriteScheme());
    store.Update(c => { c.Enabled = true; c.ActiveThemeId = "imported-dusk"; c.InstalledThemeIds = ["imported-dusk"]; });

    var result = hook.BeforeLaunch(box.Info);

    Assert.Empty(result.Warnings);
    Assert.Contains(result.Actions, a => a.Contains("Activated 'Imported Dusk'"));
    Assert.Contains("SMC_IMPORTED_DUSK", store.Load().ManagedStyleResources);
  }
}
