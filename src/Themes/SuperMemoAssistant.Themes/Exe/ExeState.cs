// What sm20.exe holds from this tool: installed window styles, the element-theming switch, and the code patch.
namespace SuperMemoAssistant.Themes.Exe;

/// <summary>What the exe holds from this tool: installed styles (SMC_* resources), the element switch and the code patch.</summary>
internal sealed class ExeState
{
  /// <summary>SMC_* resource name to .vsf bytes.</summary>
  public Dictionary<string, byte[]> Styles { get; init; } = [];

  public bool Elements { get; set; }

  public bool Patched { get; set; }

  public ExeState Clone() => new() { Styles = new Dictionary<string, byte[]>(Styles), Elements = Elements, Patched = Patched };
}
