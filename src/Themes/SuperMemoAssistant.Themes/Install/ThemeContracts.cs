// The public contract of the theme engine: what it is given (install, settings, applied state) and what it reports.
namespace SuperMemoAssistant.Themes;

/// <summary>A SuperMemo installation and the collection that is about to open or has just closed.</summary>
/// <param name="ExePath">Full path of sm20.exe.</param>
/// <param name="CollectionFolder">Folder of the collection (the folder next to its .KNO file).</param>
public sealed record SuperMemoInstall(string ExePath, string CollectionFolder)
{
  public string Root => Path.GetDirectoryName(ExePath) ?? throw new ThemeException($"'{ExePath}' has no folder");

  public string BinFolder => Path.Combine(Root, "bin");

  /// <summary>Where backups go; the original sm20.exe is kept in its "exe" subfolder.</summary>
  public string BackupFolder => Path.Combine(Root, "smcards-backups");

  public string ExeBackupFolder => Path.Combine(BackupFolder, "exe");

  public string CollectionBackupFolder => Path.Combine(BackupFolder, Path.GetFileName(CollectionFolder.TrimEnd('\\', '/')));
}

/// <summary>What the user wants. The installer makes SuperMemo match it before each start.</summary>
public sealed record ThemeSettings
{
  /// <summary>Off restores the original sm20.exe, but only if this tool turned theming on (see <see cref="AppliedState.Engaged" />).</summary>
  public bool Enabled { get; init; }

  /// <summary>Theme to activate. It is written to the collection once, when this value changes; later picks in SuperMemo stay.</summary>
  public string? ActiveThemeId { get; init; }

  /// <summary>Themes to add to SuperMemo's Window > Themes list. The active theme is always added.</summary>
  public IReadOnlyList<string> InstalledThemeIds { get; init; } = [];

  /// <summary>The card area and the status bar follow the window theme.</summary>
  public bool ThemeElements { get; init; } = true;

  /// <summary>Cards follow a theme picked in Window > Themes at once (a code patch; only on the supported build).</summary>
  public bool LiveSwitching { get; init; } = true;
}

/// <summary>What the installer did on earlier starts. The caller keeps it between runs.</summary>
public sealed record AppliedState
{
  /// <summary>True once this tool has taken over sm20.exe. Until then, changes that another tool made to the exe are left alone.</summary>
  public bool Engaged { get; init; }

  /// <summary>The last theme id whose slot and mode were written to the collection.</summary>
  public string? ActiveThemeId { get; init; }

  /// <summary>The sm20.exe style resources this tool added. Only these are ever removed.</summary>
  public IReadOnlyList<string> ManagedStyleResources { get; init; } = [];
}

/// <summary>The result of one run. <see cref="State" /> must be saved by the caller.</summary>
public sealed record ThemeReport(IReadOnlyList<string> Actions, IReadOnlyList<string> Warnings, AppliedState State)
{
  public bool Changed => Actions.Count > 0;
}

/// <summary>One window style that is inside sm20.exe.</summary>
public sealed record InstalledStyle(string Resource, string Name, bool BuiltIn);

/// <summary>What sm20.exe and the collection hold now. <see cref="InstalledThemeIds" /> lists the library themes that already have a style in sm20.exe.</summary>
public sealed record ThemeStatus(
  IReadOnlyList<InstalledStyle> Styles,
  string ActiveLight,
  string ActiveDark,
  bool DarkMode,
  bool ElementsThemed,
  bool LivePatched,
  bool LivePatchSupported,
  IReadOnlyList<string> InstalledThemeIds);
