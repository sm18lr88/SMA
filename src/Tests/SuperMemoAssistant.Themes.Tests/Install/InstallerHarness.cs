// A sandbox SuperMemo install built from a copy of the original sm20.exe, with an installer whose "SuperMemo is running" check is scripted.
using SuperMemoAssistant.Themes.Exe;

namespace SuperMemoAssistant.Themes.Tests.Install;

internal sealed class InstallerHarness : IDisposable
{
  private readonly TestSandbox _box = new();

  public InstallerHarness()
  {
    Install   = _box.InstallWithExe(GoldenData.Sm20OriginalPath!);
    Installer = new ThemeInstaller(Library, () => Running);
  }

  public static ThemeLibrary Library { get; } = ThemeLibrary.Load();

  public SuperMemoInstall Install { get; }

  public ThemeInstaller Installer { get; }

  /// <summary>What the scripted process check reports; empty means SuperMemo is closed.</summary>
  public IReadOnlyList<string> Running { get; set; } = [];

  public string Exe => Install.ExePath;

  public ThemeReport Launch(ThemeSettings settings, AppliedState? state = null) => Installer.PrepareLaunch(Install, settings, state ?? new AppliedState());

  public string Read(string relative) => _box.Read(relative);

  public void Write(string relative, string text) => _box.Write(relative, text);

  public byte[] ExeBytes() => File.ReadAllBytes(Exe);

  public string ProgramHash() => ExeBuilder.ProgramHash(ExeBytes());

  public IReadOnlyList<string> BackupFiles() =>
    Directory.Exists(Install.BackupFolder) ? Directory.GetFiles(Install.BackupFolder, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(Install.BackupFolder, f)).Order().ToList() : [];

  public string StyleName(string themeId) => Installer.ReadStatus(Install).Styles.Single(s => s.Resource == StyleResource(themeId)).Name;

  /// <summary>The window style name SuperMemo shows for a theme (the library name, which can be lower case).</summary>
  public static string NameOf(string themeId) => Library.TryGet(themeId)!.Name;

  public static string StyleResource(string themeId) => StyleNames.ResourceName(Library.TryGet(themeId)!);

  public static ThemeSettings Settings(string? active = "nord", params string[] installed) => new()
  {
    Enabled           = true,
    ActiveThemeId     = active,
    InstalledThemeIds = installed,
  };

  public void Dispose() => _box.Dispose();
}
