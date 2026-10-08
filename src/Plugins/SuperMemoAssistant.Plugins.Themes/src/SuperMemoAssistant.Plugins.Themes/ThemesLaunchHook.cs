// Adapts the theme engine to the launch hook contract: settings in, engine run, state saved, report out.
namespace SuperMemoAssistant.Plugins.Themes
{
  using System;
  using System.Linq;
  using SuperMemoAssistant.Interop.SMA;
  using SuperMemoAssistant.Themes;


  internal sealed class ThemesLaunchHook
  {
    private readonly Func<ThemeInstaller> _installer;
    private readonly ISettingsStore         _store;

    public ThemesLaunchHook(ThemeInstaller installer, ISettingsStore store) : this(() => installer, store) { }

    /// <param name="installer">Called on every run, so a library that was reloaded after an import is used.</param>
    public ThemesLaunchHook(Func<ThemeInstaller> installer, ISettingsStore store)
    {
      _installer = installer;
      _store     = store;
    }

    /// <summary>Runs before SuperMemo starts: makes sm20.exe and the collection match the settings, and saves what was done.</summary>
    public LaunchHookResult BeforeLaunch(SMLaunchInfo info)
    {
      var config = _store.Load();
      var report = _installer().PrepareLaunch(ToInstall(info), config.ToSettings(), config.ToState());

      if (config.Apply(report.State))
        _store.Save(config);

      return ToResult(report);
    }

    /// <summary>Runs after SuperMemo exited: recolors the cards to the theme SuperMemo saved.</summary>
    public LaunchHookResult AfterExit(SMLaunchInfo info)
    {
      var config = _store.Load();

      return ToResult(_installer().SyncAfterExit(ToInstall(info), config.ToSettings(), config.ToState()));
    }

    private static SuperMemoInstall ToInstall(SMLaunchInfo info) => new(info.SuperMemoExePath, info.CollectionFolder);

    private static LaunchHookResult ToResult(ThemeReport report) => new(report.Actions.ToArray(), report.Warnings.ToArray());
  }
}
