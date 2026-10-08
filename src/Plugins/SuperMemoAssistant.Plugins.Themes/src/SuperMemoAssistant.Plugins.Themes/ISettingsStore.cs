// Where the plugin keeps its settings. The real store is SMA's configuration service; tests use memory.
namespace SuperMemoAssistant.Plugins.Themes
{
  internal interface ISettingsStore
  {
    ThemesCfg Load();

    void Save(ThemesCfg config);
  }
}
