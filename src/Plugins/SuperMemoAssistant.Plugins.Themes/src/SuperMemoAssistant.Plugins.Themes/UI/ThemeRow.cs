// One theme in the settings list: its name, variant, color swatches, and whether it is installed or active.
namespace SuperMemoAssistant.Plugins.Themes.UI
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using SuperMemoAssistant.Themes;

  internal sealed class ThemeRow : INotifyPropertyChanged
  {
    private static readonly string[] SwatchRoles = { "bg", "bg_alt", "fg", "accent", "red", "green", "blue", "yellow", "purple" };

    public ThemeRow(ThemeEntry entry)
    {
      Entry    = entry;
      Swatches = SwatchRoles.Where(entry.Roles.ContainsKey).Select(r => entry.Roles[r]).ToList();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ThemeEntry Entry { get; }

    public string Name => Entry.Name;

    public string Variant => Entry.Variant;

    public IReadOnlyList<string> Swatches { get; }

    /// <summary>Add this theme to SuperMemo's Window > Themes list.</summary>
    public bool Install { get; set; }

    /// <summary>Use this theme now. At most one row is active.</summary>
    public bool IsActive { get; set; }
  }
}
