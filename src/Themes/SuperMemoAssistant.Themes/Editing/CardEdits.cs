// Plans the card edits that change compon.dat or the stylesheets. A plan is the new file contents; nothing is written here.
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using SuperMemoAssistant.Themes.Cards;

namespace SuperMemoAssistant.Themes.Editing;

/// <summary>The new contents of the files an edit changes (in the order they are shown), and lines to show before them.</summary>
internal sealed record CardEditPlan(List<KeyValuePair<string, byte[]>> Changes, List<string> Notes);

internal enum CssMode { Active, Light, Dark, Both }

internal static partial class CardEdits
{
  private const ushort Signature = 0xD431;

  [GeneratedRegex(@"(?<sel>[^{}]+?)\s*\{(?<body>[^}]*)\}", RegexOptions.Singleline)]
  private static partial Regex Rule();

  /// <summary>Sets the element window background of the given records in compon.dat.</summary>
  public static CardEditPlan ElementColor(CardCollection collection, IEnumerable<CardRecord> records, uint color)
  {
    var data    = (byte[])collection.Compon.Clone();
    var touched = 0;

    foreach (var record in records.Where(r => r.ComponPosition >= 0))
    {
      if (BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(record.ComponPosition)) != Signature)
        throw new ThemeException($"compon.dat changed under us at {record.Kind} {record.Number}; re-run");

      BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(record.ComponPosition + 11), color);
      touched++;
    }

    return new CardEditPlan([new(collection.ComponPath, data)], [$"{touched} record(s) selected for element color change."]);
  }

  /// <summary>The stylesheets a "css" command touches: the live one and the theme file(s) chosen by <paramref name="mode" />.</summary>
  public static List<string> StylesheetTargets(string smRoot, CssMode mode, Encoding ansi)
  {
    var bin  = Path.Combine(smRoot, "bin");
    var ini  = Path.Combine(bin, "supermemo.ini");
    var dark = File.Exists(ini) && new IniText(ansi.GetString(File.ReadAllBytes(ini))).Get("SuperMemo", "Dark mode", "0") == "1";
    var live = Path.Combine(bin, "supermemo.css");
    var light = Path.Combine(bin, "LightMode.css");
    var darkFile = Path.Combine(bin, "DarkMode.css");

    return mode switch
    {
      CssMode.Both  => [live, light, darkFile],
      CssMode.Light => dark ? [light] : [light, live],
      CssMode.Dark  => dark ? [darkFile, live] : [darkFile],
      _             => [live, dark ? darkFile : light],
    };
  }

  public static CardEditPlan Css(IEnumerable<string> files, string selector, string property, string? value, Encoding ansi)
  {
    var changes = new List<KeyValuePair<string, byte[]>>();

    foreach (var file in files)
    {
      var old = File.Exists(file) ? ansi.GetString(File.ReadAllBytes(file)) : "";

      changes.Add(new(file, ansi.GetBytes(CardCss.Set(old, selector, property, value))));
    }

    return new CardEditPlan(changes, []);
  }

  /// <summary>The lines that "css show" prints: each file, then its rules (all of them, or the ones for one selector).</summary>
  public static List<string> ShowCss(IEnumerable<string> files, string? selector, Encoding ansi)
  {
    var lines = new List<string>();

    foreach (var file in files)
    {
      lines.Add($"== {file}");

      var css = File.Exists(file) ? ansi.GetString(File.ReadAllBytes(file)) : "";

      foreach (Match rule in Rule().Matches(css))
      {
        if (selector is null || string.Equals(rule.Groups["sel"].Value.Trim(), selector, StringComparison.OrdinalIgnoreCase))
          lines.Add($"  {rule.Groups["sel"].Value.Trim()} {{{rule.Groups["body"].Value.Trim()}}}");
      }
    }

    return lines;
  }
}
