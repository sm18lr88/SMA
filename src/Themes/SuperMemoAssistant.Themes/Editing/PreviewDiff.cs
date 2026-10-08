// The dry-run preview of an HTML edit: the first changed lines, old and new, trimmed to the part that differs.
namespace SuperMemoAssistant.Themes.Editing;

internal static class PreviewDiff
{
  private const int Width = 90;

  /// <summary>Python's str.splitlines(): the line breaks are \n, \r, \r\n and a few control and separator characters.</summary>
  public static List<string> SplitLines(string text)
  {
    var lines   = new List<string>();
    var current = new System.Text.StringBuilder();

    for (var i = 0; i < text.Length; i++)
    {
      var c = text[i];

      if (c is '\n' or '\r' or '\v' or '\f' or '\u001c' or '\u001d' or '\u001e' or '\u0085' or '\u2028' or '\u2029')
      {
        lines.Add(current.ToString());
        current.Clear();

        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
          i++;
      }
      else
      {
        current.Append(c);
      }
    }

    if (current.Length > 0)
      lines.Add(current.ToString());

    return lines;
  }

  /// <summary>Both lines cut to a window of 90 characters that starts a little before the first difference.</summary>
  public static (string Before, string After) Window(string a, string b)
  {
    var start = 0;

    while (start < Math.Min(a.Length, b.Length) && a[start] == b[start])
      start++;

    var low = Math.Max(0, start - Width / 3);

    string Cut(string s) =>
      (low > 0 ? "..." : "") + Slice(s, low, Width) + (s.Length > low + Width ? "..." : "");

    return (Cut(a), Cut(b));
  }

  private static string Slice(string s, int start, int length) => start >= s.Length ? "" : s.Substring(start, Math.Min(length, s.Length - start));

  public static List<string> Describe(string path, string old, string changed, int limit = 8)
  {
    var a     = SplitLines(old);
    var b     = SplitLines(changed);
    var pairs = new List<(string, string)>();

    foreach (var (tag, i1, i2, j1, j2) in new DifflibMatcher(a, b).Opcodes())
    {
      if (tag == "equal")
        continue;

      var olds = a.Skip(i1).Take(i2 - i1).ToList();
      var news = b.Skip(j1).Take(j2 - j1).ToList();

      for (var k = 0; k < Math.Max(olds.Count, news.Count); k++)
        pairs.Add(Window(k < olds.Count ? olds[k] : "", k < news.Count ? news[k] : ""));
    }

    var lines = new List<string> { $"-- {path}" };

    foreach (var (before, after) in pairs.Take(limit))
    {
      lines.Add($"   - {before}");
      lines.Add($"   + {after}");
    }

    if (pairs.Count > limit)
      lines.Add($"   ... {pairs.Count - limit} more changed line(s)");

    return lines;
  }
}
