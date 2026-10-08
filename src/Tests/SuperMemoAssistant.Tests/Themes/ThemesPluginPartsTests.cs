// Checks the plugin's smaller parts: the saved settings, registering the hook with an old or new SMA, and the settings view model and window.
namespace SuperMemoAssistant.Tests.Themes;

using Newtonsoft.Json;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.Interop.SuperMemo;
using SuperMemoAssistant.Interop.SMA.Notifications;
using SuperMemoAssistant.Plugins.Themes;
using SuperMemoAssistant.Plugins.Themes.UI;
using global::SuperMemoAssistant.Themes;
using Xunit;

public class ThemesPluginPartsTests
{
  private static readonly ThemeLibrary Library = ThemeLibrary.Load();

  private static ThemeInstaller Installer => new(Library, () => []);

  private static ThemesViewModel ViewModel(InMemoryStore store, Func<SuperMemoInstall?>? install = null) =>
    new(new ThemeCatalog(Path.Combine(Path.GetTempPath(), "sma-themes-vm-" + Guid.NewGuid().ToString("N")[..8])), store, install ?? (() => null));

  [Fact]
  public void Settings_AreOffByDefault_SoInstallingThePluginChangesNothing()
  {
    var cfg = new ThemesCfg();

    Assert.False(cfg.Enabled);
    Assert.False(cfg.Engaged);
    Assert.Null(cfg.ToSettings().ActiveThemeId);
    Assert.True(cfg.ThemeElements && cfg.LiveSwitching);
  }

  [Fact]
  public void Settings_SurviveAJsonRoundTrip_AndMapToTheEngineTypes()
  {
    var cfg = new ThemesCfg { Enabled = true, ActiveThemeId = "nord", InstalledThemeIds = ["nord", "tender"], LiveSwitching = false };

    cfg.Apply(new AppliedState { Engaged = true, ActiveThemeId = "nord", ManagedStyleResources = ["SMC_NORD"] });

    var back = JsonConvert.DeserializeObject<ThemesCfg>(JsonConvert.SerializeObject(cfg))!;

    Assert.Equal(["nord", "tender"], back.ToSettings().InstalledThemeIds);
    Assert.False(back.ToSettings().LiveSwitching);
    Assert.True(back.ToState().Engaged);
    Assert.Equal(["SMC_NORD"], back.ToState().ManagedStyleResources);
  }

  [Fact]
  public void Apply_ReportsWhetherTheStateChanged()
  {
    var cfg   = new ThemesCfg();
    var state = new AppliedState { Engaged = true, ManagedStyleResources = ["SMC_NORD"] };

    Assert.True(cfg.Apply(state));
    Assert.False(cfg.Apply(state));
  }

  [Fact]
  public void Registration_PassesTheHookToSma()
  {
    var sma  = new FakeSma();
    var hook = new ThemesLaunchHook(Installer, new InMemoryStore());

    Assert.True(LaunchHookRegistration.TryRegister(sma, hook, _ => Assert.Fail("no warning expected")));
    Assert.Equal("Themes", sma.RegisteredName);
    Assert.NotNull(sma.Before);
    Assert.NotNull(sma.After);
  }

  [Fact]
  public void Registration_OnAnSmaWithoutLaunchHooks_TellsTheUserToUpdate_InsteadOfFailing()
  {
    var warnings = new List<string>();
    var sma      = new FakeSma { Throws = new MissingMethodException("RegisterLaunchHook") };

    var accepted = LaunchHookRegistration.TryRegister(sma, new ThemesLaunchHook(Installer, new InMemoryStore()), warnings.Add);

    Assert.False(accepted);
    Assert.Equal([LaunchHookRegistration.UnsupportedMessage], warnings);
    Assert.Contains("Update SMA", LaunchHookRegistration.UnsupportedMessage);
  }

  [Fact]
  public void ViewModel_ListsEveryThemeAndFiltersBySearchAndVariant()
  {
    var vm = ViewModel(new InMemoryStore());

    Assert.Equal(Library.Entries.Count, vm.Rows.Count);

    vm.Search = "nord";
    Assert.NotEmpty(vm.Rows);
    Assert.All(vm.Rows, r => Assert.Contains("nord", r.Entry.Id + r.Name, StringComparison.OrdinalIgnoreCase));

    vm.VariantFilter = "Light";
    Assert.All(vm.Rows, r => Assert.Equal("light", r.Variant));
    Assert.Contains(vm.Rows, r => r.Entry.Id == "nord-light");

    vm.Search        = "";
    vm.VariantFilter = ThemesViewModel.AllVariants;
    Assert.Equal(Library.Entries.Count, vm.Rows.Count);
  }

  [Fact]
  public void ViewModel_AllowsOnlyOneActiveTheme()
  {
    var vm    = ViewModel(new InMemoryStore());
    var nord  = vm.Rows.Single(r => r.Entry.Id == "nord");
    var light = vm.Rows.Single(r => r.Entry.Id == "nord-light");

    nord.IsActive  = true;
    light.IsActive = true;

    Assert.False(nord.IsActive);
    Assert.Equal("nord-light", vm.ActiveThemeId);
  }

  [Fact]
  public void ViewModel_Save_WritesTheChoices_AndKeepsTheEngineBookkeeping()
  {
    var store = new InMemoryStore();

    store.Update(c => c.Apply(new AppliedState { Engaged = true, ActiveThemeId = "old", ManagedStyleResources = ["SMC_OLD"] }));

    var vm = ViewModel(store);

    vm.Enabled = true;
    vm.LiveSwitching = false;
    vm.Rows.Single(r => r.Entry.Id == "nord").Install = true;
    vm.Rows.Single(r => r.Entry.Id == "nord").IsActive = true;
    vm.Save();

    var saved = store.Load();

    Assert.True(saved.Enabled);
    Assert.False(saved.LiveSwitching);
    Assert.Equal("nord", saved.ActiveThemeId);
    Assert.Equal(["nord"], saved.InstalledThemeIds);
    Assert.True(saved.Engaged);
    Assert.Equal(["SMC_OLD"], saved.ManagedStyleResources);
  }

  [Fact]
  public void ViewModel_CuratedSet_ChecksTheCuratedThemes()
  {
    var vm = ViewModel(new InMemoryStore());

    Assert.Equal(0, vm.InstallCount);

    vm.UseCuratedSet();

    Assert.Equal(Library.CuratedIds.Count(id => Library.TryGet(id) is not null), vm.InstallCount);
  }

  [Fact]
  public void ViewModel_WithoutARunningSuperMemo_SaysTheStateCouldNotBeRead()
  {
    Assert.Contains("could not be read", ViewModel(new InMemoryStore()).Status);
  }

  [Fact]
  public void ViewModel_PreChecksThemesThatAreAlreadyInTheExe_SoAdoptingThemIsOneClick()
  {
    Assert.SkipWhen(OriginalExe.Path is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box   = new ThemeSandbox(withExe: true);
    var       store = new InMemoryStore();

    store.Update(c => { c.Enabled = true; c.ActiveThemeId = "nord"; });
    new ThemesLaunchHook(Installer, store).BeforeLaunch(box.Info);
    store.Update(c => c.InstalledThemeIds = []); // as if another tool had installed it

    var vm = ViewModel(store, () => new SuperMemoInstall(box.ExePath, box.Info.CollectionFolder));

    Assert.True(vm.Rows.Single(r => r.Entry.Id == "nord").Install);
    Assert.Contains("installed theme(s)", vm.Status);
  }

  [Fact]
  public void Window_BuildsWithoutShowing_SoItsRowTemplateParses()
  {
    Exception? failure = null;
    object?    content = null;

    var thread = new Thread(() =>
    {
      try
      {
        var window = ThemesWindow.Create(ViewModel(new InMemoryStore()));

        content = window.Content;
      }
      catch (Exception ex)
      {
        failure = ex;
      }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    Assert.Null(failure);
    Assert.NotNull(content);
  }

  private sealed class FakeSma : ISuperMemoAssistant
  {
    public Exception? Throws { get; init; }

    public string? RegisteredName { get; private set; }

    public Func<SMLaunchInfo, LaunchHookResult>? Before { get; private set; }

    public Func<SMLaunchInfo, LaunchHookResult>? After { get; private set; }

    public void RegisterLaunchHook(string name, Func<SMLaunchInfo, LaunchHookResult> beforeLaunch, Func<SMLaunchInfo, LaunchHookResult> afterExit)
    {
      if (Throws is not null)
        throw Throws;

      (RegisteredName, Before, After) = (name, beforeLaunch, afterExit);
    }

    public void RegisterCommand(PaletteCommand command, Action execute) => throw new NotSupportedException();

    public void UnregisterCommand(string owner, string id) => throw new NotSupportedException();

    public ISuperMemo SM => throw new NotSupportedException();

    public IEnumerable<string> Layouts => [];

    public INotificationManager NotificationMgr => throw new NotSupportedException();

    public event Action<SuperMemoAssistant.Interop.SuperMemo.Core.SMCollection>? OnCollectionSelectedEvent { add { } remove { } }

    public event Action? OnSMStartingEvent { add { } remove { } }

    public event Action? OnSMStartedEvent { add { } remove { } }

    public event Action? OnSMStoppedEvent { add { } remove { } }
  }
}
