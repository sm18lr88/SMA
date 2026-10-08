// Reads and edits SuperMemo's ini files in place: set one key keeping the file's style, and read keys the way Python's configparser does.
namespace SuperMemoAssistant.Themes.Cards;

internal sealed class IniText
{
  private readonly Dictionary<string, Dictionary<string, string>> _sections = [];

  /// <summary>Parses sections and key/value lines. Option names are case-insensitive; "#" and ";" start comment lines.</summary>
  public IniText(string text)
  {
    Dictionary<string, string>? current = null;
    string? lastKey = null;

    foreach (var raw in text.Split('\n').Select(l => l.TrimEnd('\r')))
    {
      var trimmed = raw.Trim();

      if (trimmed.Length == 0 || trimmed[0] is '#' or ';')
        continue;

      if (char.IsWhiteSpace(raw[0]) && current is not null && lastKey is not null)
      {
        current[lastKey] += "\n" + trimmed; // continuation of the previous value

        continue;
      }

      if (trimmed[0] == '[' && trimmed.EndsWith(']'))
      {
        var name = trimmed[1..^1];

        if (!_sections.TryGetValue(name, out current))
          _sections[name] = current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        lastKey = null;

        continue;
      }

      var at = trimmed.IndexOfAny(['=', ':']);

      if (current is null || at < 0)
        continue;

      lastKey          = trimmed[..at].Trim();
      current[lastKey] = trimmed[(at + 1)..].Trim();
    }
  }

  public string Get(string section, string key, string fallback = "") =>
    _sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v) ? v : fallback;

  /// <summary>Sets or adds <paramref name="key" /> in <paramref name="section" />, keeping line endings and surrounding lines.</summary>
  public static string Set(string text, string section, string key, string value)
  {
    var nl    = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    var lines = text.Split(nl).ToList();
    var start = lines.FindIndex(l => string.Equals(l.Trim(), $"[{section}]", StringComparison.OrdinalIgnoreCase));

    if (start < 0)
      return text.TrimEnd(nl.ToCharArray()) + $"{nl}[{section}]{nl}{key}={value}{nl}";

    var end = lines.FindIndex(start + 1, l => l.StartsWith('['));

    if (end < 0)
      end = lines.Count;

    for (var i = start + 1; i < end; i++)
    {
      var name = lines[i].Split('=', 2)[0].Trim();

      if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
      {
        lines[i] = $"{key}={value}";

        return string.Join(nl, lines);
      }
    }

    lines.Insert(lines[end - 1].Trim().Length > 0 ? end : end - 1, $"{key}={value}");

    return string.Join(nl, lines);
  }
}
