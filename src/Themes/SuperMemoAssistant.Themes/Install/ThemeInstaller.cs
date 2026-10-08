// Makes SuperMemo match the user's theme settings before it starts, and recolors cards after it closes.
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Styles;

namespace SuperMemoAssistant.Themes;

/// <summary>
///   The theme engine's entry point. Call <see cref="PrepareLaunch" /> before SuperMemo starts and
///   <see cref="SyncAfterExit" /> after it has exited. Both are safe to run on every start: they change only what differs,
///   back up every file first, and do nothing while a SuperMemo process runs.
/// </summary>
public sealed class ThemeInstaller
{
  private readonly ThemeLibrary _library;
  private readonly Func<IReadOnlyList<string>> _running;

  public ThemeInstaller(ThemeLibrary library) : this(library, SafeWriter.RunningSuperMemo) { }

  internal ThemeInstaller(ThemeLibrary library, Func<IReadOnlyList<string>> running)
  {
    _library = library;
    _running = running;
  }

  /// <summary>Applies the settings to sm20.exe and the collection. Expected problems come back as warnings, not exceptions.</summary>
  public ThemeReport PrepareLaunch(SuperMemoInstall install, ThemeSettings settings, AppliedState previous)
  {
    var report = new ReportBuilder();

    if (_running().Count > 0)
    {
      report.Warn("SuperMemo is already running, so theme changes were skipped.");

      return report.Build(previous);
    }

    try
    {
      var state = settings.Enabled ? Enable(install, settings, previous, report) : Disable(install, previous, report);

      return report.Build(state);
    }
    catch (ThemeException ex)
    {
      report.Warn(ex.Message);

      return report.Build(previous);
    }
  }

  /// <summary>Recolors the cards of the light and dark slots from the styles that are active now (SuperMemo saves a pick from Window > Themes only on exit).</summary>
  public ThemeReport SyncAfterExit(SuperMemoInstall install, ThemeSettings settings, AppliedState previous)
  {
    var report = new ReportBuilder();

    if (!settings.Enabled)
      return report.Build(previous);

    if (_running().Count > 0)
    {
      report.Warn("Another SuperMemo is running, so the card sync was skipped.");

      return report.Build(previous);
    }

    try
    {
      CardSync.Sync(install, _library, new SafeWriter(install, _running), report);
    }
    catch (ThemeException ex)
    {
      report.Warn(ex.Message);
    }

    return report.Build(previous);
  }

  /// <summary>What sm20.exe and the collection hold now, for a settings window.</summary>
  public ThemeStatus ReadStatus(SuperMemoInstall install)
  {
    var state     = ExeBuilder.ReadState(install.ExePath);
    var resources = ExeResources.ReadStyles(install.ExePath);
    var styles    = resources.Select(kv => new InstalledStyle(kv.Key, VsfFile.StyleName(kv.Value), !kv.Key.StartsWith(ExeBuilder.CustomPrefix, StringComparison.Ordinal)))
                             .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                             .ToList();
    var installed = _library.Entries.Where(e => resources.ContainsKey(StyleNames.ResourceName(e))).Select(e => e.Id).ToList();
    var active    = CardSync.ActiveStyles(install);

    return new ThemeStatus(styles, active.Light, active.Dark, active.DarkMode, state.Elements, state.Patched, ExeBuilder.PatchSupported(install.ExePath), installed);
  }

  private AppliedState Disable(SuperMemoInstall install, AppliedState previous, ReportBuilder report)
  {
    if (!previous.Engaged)
      return previous; // this tool never changed the exe, so whatever is in it belongs to someone else

    if (ExeBuilder.HasChanges(install.ExePath))
    {
      ExeBuilder.Restore(install.ExePath, install.ExeBackupFolder);
      report.Did("Restored the original sm20.exe.");
    }

    return new AppliedState();
  }

  private AppliedState Enable(SuperMemoInstall install, ThemeSettings settings, AppliedState previous, ReportBuilder report)
  {
    var writer = new SafeWriter(install, _running);
    var styles = ExeResources.ReadStyles(install.ExePath);
    var now    = ExeBuilder.ReadState(install.ExePath);
    var plan   = ExePlan.Build(settings, _library, previous, styles, now, CardSync.ActiveStyles(install), ExeBuilder.PatchSupported(install.ExePath), report);

    if (ExePlan.Differs(now, plan.State))
    {
      writer.RequireClosed();
      ExeBuilder.Build(install.ExePath, install.ExeBackupFolder, plan.State);
      report.Did($"Rebuilt sm20.exe from the original: {ExePlan.Describe(now, plan.State)}.");
    }

    var activeId = ApplyActiveTheme(install, settings, previous, plan, writer, report);

    CardSync.Sync(install, _library, writer, report);

    return new AppliedState { Engaged = true, ActiveThemeId = activeId, ManagedStyleResources = plan.Managed };
  }

  /// <summary>Writes the chosen theme into its slot once, when the setting changed; later picks made in SuperMemo are left alone.</summary>
  private string? ApplyActiveTheme(SuperMemoInstall install, ThemeSettings settings, AppliedState previous, ExeTarget plan, SafeWriter writer, ReportBuilder report)
  {
    if (settings.ActiveThemeId is not { } id || id == previous.ActiveThemeId)
      return previous.ActiveThemeId;

    if (_library.TryGet(id) is not { } entry || !plan.StyleNames.TryGetValue(id, out var styleName))
      return previous.ActiveThemeId;

    var changes = CardSync.PlanSettings(install, styleName, entry.Variant);

    foreach (var (path, data) in CardSync.PlanCards(install, entry.Variant, entry.Roles, true))
      changes[path] = data;

    writer.Write(changes, $"theme-{entry.Id}");
    report.Did($"Activated '{styleName}' ({entry.Variant}).");

    return id;
  }
}
