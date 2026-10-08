// What the card editor reads from a collection: elements and templates with their components, and Delphi color values.
using System.Globalization;

namespace SuperMemoAssistant.Themes.Editing;

/// <summary>One component of an element: its kind, where its body sits in compon.dat, and the HTML file or inline text it shows.</summary>
internal sealed record CardComponent(
  int     Index,
  string  Kind,
  int     BodyOffset,
  int     DisplayAt,
  uint?   RegistryId,
  uint?   Color,
  int     Size,
  string? Path,
  string? InlineText);

/// <summary>One element or template entry in compon.dat.</summary>
internal sealed class CardRecord(int number, string kind, string title, int componPosition)
{
  /// <summary>The element number, or the template registry number for a template.</summary>
  public int Number { get; } = number;

  public string Kind { get; } = kind;

  public string Title { get; } = title;

  public int ComponPosition { get; } = componPosition;

  /// <summary>The element window background as a Delphi TColor. Null when the element has no record.</summary>
  public uint? Color { get; set; }

  public int TemplateId { get; set; }

  /// <summary>The first byte after this record in compon.dat.</summary>
  public int End { get; set; } = -1;

  public List<CardComponent> Components { get; } = [];
}

internal static class CardColors
{
  /// <summary>Delphi's "theme window color" (clWindow), which SuperMemo 20 writes for the default background.</summary>
  public const uint ClWindow = 0xFF000005;

  private static readonly Dictionary<uint, string> SystemNames = new() { [5] = "default", [15] = "system-btnface", [16] = "system-btnshadow" };

  public static string ToText(uint? color)
  {
    if (color is not { } c)
      return "-";

    if (c is 0xFFFFFFFF or 0x1FFFFFFF)
      return "none";

    if (c >> 24 is 0xFF or 0x80)
      return SystemNames.TryGetValue(c & 0xFF, out var name) ? name : $"system-{c & 0xFF}";

    if ((c & 0xFF000000) != 0)
      return $"special:0x{c:x8}";

    return $"#{c & 0xFF:X2}{(c >> 8) & 0xFF:X2}{(c >> 16) & 0xFF:X2}";
  }

  /// <summary>"#RRGGBB", "RRGGBB" or "default" to a Delphi TColor ($00BBGGRR).</summary>
  public static uint Parse(string text)
  {
    var value = text.Trim().ToLowerInvariant();

    if (value == "default")
      return ClWindow;

    value = value.StartsWith('#') ? value[1..] : value;

    if (value.Length != 6 || value.Any(c => !Uri.IsHexDigit(c)))
      throw new ThemeException($"color must be #RRGGBB or 'default', got '{text}'");

    var r = uint.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    var g = uint.Parse(value[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    var b = uint.Parse(value[4..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    return r | (g << 8) | (b << 16);
  }
}
