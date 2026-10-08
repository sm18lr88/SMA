// Finds SMA's data folder by the rules SMA itself uses, and reads the sm20.exe that SMA is set up with.
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SuperMemoAssistant.Themes.Editing;

internal static class SmaData
{
  public const string FolderVariable = "SMA_APPDATA_DIR";

  private const string DataFolderName = "SuperMemoAssistant";

  private const string PreInitFile = "supermemoassistant.json";

  private const string ThemesPluginFolder = "SuperMemoAssistant.Plugins.Themes";

  public static string Folder() =>
    Folder(Environment.GetEnvironmentVariable(FolderVariable), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

  /// <summary>
  ///   The variable wins. Then the AppDataDirPath of supermemoassistant.json in the user profile, when that folder exists.
  ///   Otherwise the user profile. SMA's data folder is named SuperMemoAssistant inside the chosen folder.
  /// </summary>
  public static string Folder(string? overrideFolder, string userProfile)
  {
    if (!string.IsNullOrWhiteSpace(overrideFolder))
      return Path.Combine(overrideFolder, DataFolderName);

    var baseFolder = userProfile;
    var preInit    = Path.Combine(userProfile, PreInitFile);

    if (File.Exists(preInit) && ReadString(preInit, "AppDataDirPath") is { Length: > 0 } configured && Directory.Exists(configured))
      baseFolder = configured;

    return Path.Combine(baseFolder, DataFolderName);
  }

  /// <summary>The sm20.exe that SMA was set up with (CoreCfg.json), or null when SMA has no usable setting.</summary>
  public static string? ConfiguredExe(string dataFolder)
  {
    var cfg = Path.Combine(dataFolder, "Configs", "Core", "CoreCfg.json");

    if (!File.Exists(cfg))
      return null;

    try
    {
      var exe = JsonNode.Parse(File.ReadAllText(cfg))?["SuperMemo"]?["SMBinPath"]?.GetValue<string>();

      return !string.IsNullOrWhiteSpace(exe) && File.Exists(exe) ? exe : null;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
    {
      return null;
    }
  }

  /// <summary>The user's own theme library, in the data folder of the Themes plugin.</summary>
  public static string UserLibrary(string dataFolder) => Path.Combine(dataFolder, "Configs", ThemesPluginFolder, "user-themes.json");

  private static string? ReadString(string file, string name)
  {
    try
    {
      return JsonNode.Parse(File.ReadAllText(file))?[name]?.GetValue<string>();
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
    {
      return null;
    }
  }
}
