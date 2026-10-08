// Turns library entries into SuperMemo window styles (names, resource ids, recolored blobs) and reads a palette back from any style.
using System.Text;
using System.Text.RegularExpressions;
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Styles;

namespace SuperMemoAssistant.Themes;

internal static partial class StyleNames
{
  /// <summary>The built-in style that each variant is recolored from.</summary>
  public static readonly IReadOnlyDictionary<string, string> BaseStyles = new Dictionary<string, string> { ["dark"] = "WINDOWSDARK", ["light"] = "WINDOWS" };

  private static readonly Dictionary<string, string> DelphiColors = new()
  {
    ["clBlack"] = "#000000", ["clWhite"] = "#FFFFFF", ["clGray"] = "#808080", ["clSilver"] = "#C0C0C0",
    ["clMaroon"] = "#800000", ["clGreen"] = "#008000", ["clOlive"] = "#808000", ["clNavy"] = "#000080",
    ["clPurple"] = "#800080", ["clTeal"] = "#008080", ["clRed"] = "#FF0000", ["clLime"] = "#00FF00",
    ["clYellow"] = "#FFFF00", ["clBlue"] = "#0000FF", ["clFuchsia"] = "#FF00FF", ["clAqua"] = "#00FFFF",
  };

  [GeneratedRegex("[^A-Z0-9]+")]
  private static partial Regex NonResource();

  /// <summary>The sm20.exe resource name of an installed theme: SMC_ plus the id in capitals, at most 60 characters.</summary>
  public static string ResourceName(ThemeEntry entry)
  {
    var name = (ExeBuilder.CustomPrefix + NonResource().Replace(entry.Id.ToUpperInvariant(), "_")).Trim('_');

    return name.Length > 60 ? name[..60] : name;
  }

  /// <summary>
  ///   The name SuperMemo shows. It reads collection.ini as ANSI, so the name is ASCII ("Rosé" becomes "Rose"), and a
  ///   name that a built-in style already uses gets a suffix.
  /// </summary>
  public static string WindowStyleName(ThemeEntry entry, ISet<string> builtinNames)
  {
    var ascii = new StringBuilder();

    foreach (var c in entry.Name.Normalize(NormalizationForm.FormKD).Where(c => c < 128))
      ascii.Append(c);

    var name = ascii.ToString();

    return builtinNames.Contains(name) ? $"{name} (smcards)" : name;
  }

  public static byte[] BuildStyle(ThemeEntry entry, IReadOnlyDictionary<string, byte[]> styles, string name)
  {
    var baseStyle = VsfFile.Load(styles[BaseStyles[entry.Variant]]);

    return VsfFile.Dump(VsfRecolor.Recolor(baseStyle, entry.Roles, name, entry.Variant));
  }

  private static string? StyleColor(string value)
  {
    if (value.StartsWith('$') && value.Length == 9) // $00BBGGRR
      return $"#{value[7..9]}{value[5..7]}{value[3..5]}";

    return DelphiColors.GetValueOrDefault(value);
  }

  /// <summary>A card palette for any window style, built from the style's own system colors.</summary>
  public static ThemeEntry EntryFromStyle(byte[] blob)
  {
    var values = new Dictionary<string, string?>();

    foreach (var item in VsfFile.Load(blob).Tail.Where(t => t.IsPair && !t.Value!.Contains(',')))
      values[item.Key!] = StyleColor(item.Value!);

    var pick = new (string Role, string Key)[]
    {
      ("bg", "clWindow"), ("fg", "clWindowText"), ("bg_alt", "clBtnFace"), ("border", "clBtnShadow"),
      ("selection", "clHighlight"), ("accent", "FocusEffect"),
    };
    var roles = new Dictionary<string, string?>();

    foreach (var (role, key) in pick)
    {
      if (!string.IsNullOrEmpty(values.GetValueOrDefault(key)))
        roles[role] = values[key];
    }

    if (!roles.ContainsKey("accent") && !string.IsNullOrEmpty(values.GetValueOrDefault("clHighlight")))
      roles["accent"] = values["clHighlight"];

    return Palette.Complete(roles, VsfFile.StyleName(blob), "SuperMemo window style");
  }
}
