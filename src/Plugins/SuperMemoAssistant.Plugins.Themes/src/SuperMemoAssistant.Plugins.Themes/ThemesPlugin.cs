// The Themes plugin: wires the engine, the saved settings, the launch hook and the settings window together.
namespace SuperMemoAssistant.Plugins.Themes
{
  using System.Diagnostics;
  using System.IO;
  using SuperMemoAssistant.Interop.Plugins;
  using SuperMemoAssistant.Interop.SuperMemo.Core;
  using SuperMemoAssistant.Interop;
  using SuperMemoAssistant.Plugins.Themes.UI;
  using SuperMemoAssistant.Services;
  using SuperMemoAssistant.Services.ToastNotifications;
  using SuperMemoAssistant.Themes;

  /// <summary>
  ///   Themes SuperMemo's windows, cards and status bar. It needs SuperMemo to be closed to change sm20.exe, so it
  ///   works through launch hooks: it applies the settings before SuperMemo starts and recolors cards after it exits.
  /// </summary>
  public class ThemesPlugin : SMAPluginBase<ThemesPlugin>
  {
    internal ThemeCatalog Catalog { get; private set; } = null!;

    internal ISettingsStore Store { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Themes";

    /// <inheritdoc />
    public override bool HasSettings => true;

    /// <inheritdoc />
    protected override void OnPluginInitialized()
    {
      Catalog = new ThemeCatalog(Path.Combine(SMAFileSystem.ConfigDir.FullPathWin, AssemblyName));
      Store   = new ConfigurationStore();

      LaunchHookRegistration.TryRegister(
        Svc.SMA,
        new ThemesLaunchHook(() => Catalog.Installer, Store),
        message => $"Themes: {message}".ShowDesktopNotification());

      base.OnPluginInitialized();
    }

    /// <inheritdoc />
    public override void ShowSettings()
    {
      ThemesWindow.ShowSingle(new ThemesViewModel(Catalog, Store, CurrentInstall));
    }

    /// <summary>The SuperMemo that SMA runs now: its exe (read from the process) and the open collection. Null when unavailable.</summary>
    private static SuperMemoInstall? CurrentInstall()
    {
      if (Svc.SM is not { } sm || sm.ProcessId <= 0)
        return null;

      using var process = Process.GetProcessById(sm.ProcessId);

      return process.MainModule?.FileName is { } exe ? new SuperMemoInstall(exe, sm.Collection.GetRootDirPath()) : null;
    }
  }

  internal sealed class ConfigurationStore : ISettingsStore
  {
    public ThemesCfg Load() => Svc.Configuration.Load<ThemesCfg>() ?? new ThemesCfg();

    public void Save(ThemesCfg config) => Svc.Configuration.Save(config);
  }
}
