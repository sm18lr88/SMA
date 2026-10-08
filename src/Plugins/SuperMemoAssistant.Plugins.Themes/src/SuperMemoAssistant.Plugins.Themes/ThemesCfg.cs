// The plugin's saved choices (what the user wants) and bookkeeping (what the engine did), and their mapping to engine types.
namespace SuperMemoAssistant.Plugins.Themes
{
  using System.Collections.Generic;
  using System.Linq;
  using SuperMemoAssistant.Themes;

  /// <summary>Saved as JSON in the plugin's configuration folder. Theming is off until the user turns it on.</summary>
  public class ThemesCfg
  {
    public bool Enabled { get; set; }

    public string? ActiveThemeId { get; set; }

    public List<string> InstalledThemeIds { get; set; } = new();

    public bool ThemeElements { get; set; } = true;

    public bool LiveSwitching { get; set; } = true;

    public bool Engaged { get; set; }

    public string? AppliedActiveThemeId { get; set; }

    public List<string> ManagedStyleResources { get; set; } = new();

    internal ThemeSettings ToSettings() => new()
    {
      Enabled           = Enabled,
      ActiveThemeId     = string.IsNullOrWhiteSpace(ActiveThemeId) ? null : ActiveThemeId,
      InstalledThemeIds = InstalledThemeIds.ToList(),
      ThemeElements     = ThemeElements,
      LiveSwitching     = LiveSwitching,
    };

    internal AppliedState ToState() => new()
    {
      Engaged               = Engaged,
      ActiveThemeId         = AppliedActiveThemeId,
      ManagedStyleResources = ManagedStyleResources.ToList(),
    };

    /// <summary>Takes over what the engine did. Returns true when that differs from what was saved.</summary>
    internal bool Apply(AppliedState state)
    {
      var changed = Engaged != state.Engaged
                    || AppliedActiveThemeId != state.ActiveThemeId
                    || !ManagedStyleResources.SequenceEqual(state.ManagedStyleResources);

      Engaged               = state.Engaged;
      AppliedActiveThemeId  = state.ActiveThemeId;
      ManagedStyleResources = state.ManagedStyleResources.ToList();

      return changed;
    }
  }
}
