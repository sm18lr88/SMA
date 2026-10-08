// Owns the theme library and the engine objects built on it, and rebuilds them after the user imports themes.
namespace SuperMemoAssistant.Plugins.Themes
{
  using System.IO;
  using SuperMemoAssistant.Themes;

  internal interface IThemeCatalog
  {
    ThemeLibrary Library { get; }

    ThemeInstaller Installer { get; }

    /// <summary>Adds the themes in a file or folder to the user's library and reloads the library.</summary>
    ImportResult Import(string path);
  }

  internal sealed class ThemeCatalog : IThemeCatalog
  {
    public const string UserLibraryFile = "user-themes.json";

    public const string ObsidianCacheFile = "obsidian-base.css";

    private readonly string _folder;

    /// <param name="folder">The plugin's own data folder. The user library and the Obsidian defaults cache live here.</param>
    public ThemeCatalog(string folder)
    {
      _folder = folder;
      Reload();
    }

    public ThemeLibrary Library { get; private set; } = null!;

    public ThemeInstaller Installer { get; private set; } = null!;

    public string UserLibraryPath => Path.Combine(_folder, UserLibraryFile);

    public ImportResult Import(string path)
    {
      var result = new ThemeImporter(Library, UserLibraryPath, Path.Combine(_folder, ObsidianCacheFile)).Import(path);

      Reload();

      return result;
    }

    private void Reload()
    {
      Library   = ThemeLibrary.Load(UserLibraryPath);
      Installer = new ThemeInstaller(Library);
    }
  }
}
