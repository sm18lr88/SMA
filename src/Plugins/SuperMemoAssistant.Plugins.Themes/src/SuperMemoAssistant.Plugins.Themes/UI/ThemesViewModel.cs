// The logic of the settings window: filtering, choosing themes, importing, reading what sm20.exe holds, and saving. No WPF types, so it is unit-tested.
namespace SuperMemoAssistant.Plugins.Themes.UI
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using SuperMemoAssistant.Themes;

  internal sealed class ThemesViewModel : INotifyPropertyChanged
  {
    public const string AllVariants = "All";

    private readonly IThemeCatalog           _catalog;
    private readonly ISettingsStore          _store;
    private readonly Func<SuperMemoInstall?> _install;
    private          List<ThemeRow>          _all = [];

    public ThemesViewModel(IThemeCatalog catalog, ISettingsStore store, Func<SuperMemoInstall?> install)
    {
      _catalog = catalog;
      _store   = store;
      _install = install;

      var config = store.Load();
      var status = ReadStatus();

      Enabled       = config.Enabled;
      ThemeElements = config.ThemeElements;
      LiveSwitching = config.LiveSwitching;

      LoadRows(config.InstalledThemeIds.Concat(status?.InstalledThemeIds ?? []).ToHashSet(), config.ActiveThemeId);

      Status = Describe(status);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Enabled { get; set; }

    public bool ThemeElements { get; set; }

    public bool LiveSwitching { get; set; }

    public string Search { get; set; } = "";

    public string VariantFilter { get; set; } = AllVariants;

    public IReadOnlyList<ThemeRow> Rows { get; private set; } = [];

    public string Status { get; private set; } = "";

    public string ImportMessage { get; private set; } = "";

    public int InstallCount => _all.Count(r => r.Install);

    public string? ActiveThemeId => _all.FirstOrDefault(r => r.IsActive)?.Entry.Id;

    public void OnSearchChanged() => Refilter();

    public void OnVariantFilterChanged() => Refilter();

    /// <summary>Checks the curated native set (themes that look right as SuperMemo window styles).</summary>
    public void UseCuratedSet()
    {
      var curated = _catalog.Library.CuratedIds.ToHashSet();

      foreach (var row in _all.Where(r => curated.Contains(r.Entry.Id)))
        row.Install = true;
    }

    /// <summary>
    ///   Adds the themes in a base16/base24 file or folder, a VS Code theme or extension, or an Obsidian theme folder to the
    ///   user's library. The list shows them at once, unchecked, and the choices already made stay.
    /// </summary>
    public void Import(string path)
    {
      var installed = _all.Where(r => r.Install).Select(r => r.Entry.Id).ToHashSet();
      var active    = ActiveThemeId;

      try
      {
        var result = _catalog.Import(path);

        LoadRows(installed, active);
        ImportMessage = Summarize(result);
      }
      catch (ThemeException ex)
      {
        ImportMessage = $"Import failed: {ex.Message}";
      }
    }

    /// <summary>Saves the choices. SMA applies them the next time SuperMemo starts through it.</summary>
    public void Save()
    {
      var config = _store.Load();

      config.Enabled           = Enabled;
      config.ThemeElements     = ThemeElements;
      config.LiveSwitching     = LiveSwitching;
      config.ActiveThemeId     = ActiveThemeId;
      config.InstalledThemeIds = _all.Where(r => r.Install).Select(r => r.Entry.Id).ToList();

      _store.Save(config);
    }

    private void LoadRows(ISet<string> installed, string? activeId)
    {
      foreach (var old in _all)
        old.PropertyChanged -= HandleRowChange;

      _all = _catalog.Library.Entries.Select(e => new ThemeRow(e)).ToList();

      foreach (var row in _all)
      {
        row.Install         = installed.Contains(row.Entry.Id);
        row.IsActive        = row.Entry.Id == activeId;
        row.PropertyChanged += HandleRowChange;
      }

      Refilter();
    }

    private static string Summarize(ImportResult result)
    {
      var parts = new List<string>
      {
        result.Added.Count == 0
          ? "No themes were found there."
          : $"Imported {result.Added.Count} theme(s): {string.Join(", ", result.Added.Take(6).Select(e => e.Name))}{(result.Added.Count > 6 ? ", ..." : "")}. Tick Install to use them.",
      };

      if (result.Skipped.Count > 0)
        parts.Add($"Skipped {result.Skipped.Count}: {string.Join("; ", result.Skipped.Take(3))}{(result.Skipped.Count > 3 ? "; ..." : "")}");

      parts.AddRange(result.Notes);

      return string.Join(" ", parts);
    }

    private ThemeStatus? ReadStatus()
    {
      try
      {
        return _install() is { } install ? _catalog.Installer.ReadStatus(install) : null;
      }
      catch (Exception ex) when (ex is ThemeException or System.IO.IOException or System.ComponentModel.Win32Exception)
      {
        return null;
      }
    }

    private static string Describe(ThemeStatus? status)
    {
      if (status is null)
        return "SuperMemo's current state could not be read.";

      var custom = status.Styles.Count(s => !s.BuiltIn);

      return $"sm20.exe now has {custom} installed theme(s). Card area and status bar: {(status.ElementsThemed ? "themed" : "SuperMemo's own colors")}. "
             + $"Live switching: {(status.LivePatched ? "on" : status.LivePatchSupported ? "off" : "not supported by this SuperMemo build")}.";
    }

    private void HandleRowChange(object? sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(ThemeRow.IsActive) && sender is ThemeRow { IsActive: true } active)
      {
        foreach (var other in _all.Where(r => !ReferenceEquals(r, active) && r.IsActive))
          other.IsActive = false;
      }

      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstallCount)));
    }

    private void Refilter()
    {
      var text = Search.Trim();

      Rows = _all.Where(r => (VariantFilter == AllVariants || string.Equals(r.Variant, VariantFilter, StringComparison.OrdinalIgnoreCase))
                             && (text.Length == 0 || r.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || r.Entry.Id.Contains(text, StringComparison.OrdinalIgnoreCase)))
                 .ToList();
    }
  }
}
