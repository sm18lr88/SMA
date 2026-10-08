// Reads the parts of a CSS file that theme importers need: flattened rules, custom properties, and body-level selectors.
using System.Text;
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes.Import;

/// <summary>Tracks quotes and parentheses so ';', '{' and '}' inside url("data:...;...") stay text.</summary>
internal sealed class CssScanner
{
  private char _quote;
  private int  _parens;
  private char _last;

  /// <summary>True when <paramref name="ch" /> is plain text, not a structural character.</summary>
  public bool Literal(char ch)
  {
    var escaped = _last == '\\';

    _last = escaped ? '\0' : ch;

    if (_quote != '\0')
    {
      if (ch == _quote && !escaped)
        _quote = '\0';

      return true;
    }

    if (ch is '"' or '\'')
    {
      _quote = ch;

      return true;
    }

    if (ch is '(' or ')')
    {
      _parens = Math.Max(0, _parens + (ch == '(' ? 1 : -1));

      return true;
    }

    return _parens > 0;
  }
}

internal static partial class CssSheet
{
  [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
  private static partial Regex Comment();

  [GeneratedRegex(@"^\s*(--[\w-]+)\s*:(.*)$", RegexOptions.Singleline)]
  private static partial Regex CustomProperty();

  [GeneratedRegex(@"^(body|html|:root)?((?:\.[\w-]+)*)$")]
  private static partial Regex BodySelector();

  [GeneratedRegex(@"\.([\w-]+)")]
  private static partial Regex ClassName();

  /// <summary>Flattens CSS into (selector, declarations). Rules nested in @-blocks are dropped.</summary>
  public static List<(string Selector, string Body)> Rules(string css)
  {
    css = Comment().Replace(css, "");

    var output = new List<(string, string)>();
    var stack  = new List<string>();
    var cur    = new List<StringBuilder> { new() };
    var buf    = new StringBuilder();
    var scan   = new CssScanner();

    foreach (var ch in css)
    {
      if (scan.Literal(ch))
      {
        buf.Append(ch);
      }
      else if (ch == '{')
      {
        stack.Add(buf.ToString().Trim());
        cur.Add(new StringBuilder());
        buf.Clear();
      }
      else if (ch == '}')
      {
        var body = (cur.Count > 0 ? Pop(cur) : "") + buf;
        var sel  = stack.Count > 0 ? Pop(stack) : "";

        if (!stack.Any(s => s.StartsWith('@')) && !sel.StartsWith('@'))
          output.Add((sel, body));

        buf.Clear();
      }
      else if (ch == ';')
      {
        if (cur.Count == 0)
          cur.Add(new StringBuilder());

        cur[^1].Append(buf).Append(';');
        buf.Clear();
      }
      else
      {
        buf.Append(ch);
      }
    }

    return output;
  }

  private static string Pop(List<StringBuilder> list)
  {
    var last = list[^1].ToString();

    list.RemoveAt(list.Count - 1);

    return last;
  }

  private static string Pop(List<string> list)
  {
    var last = list[^1];

    list.RemoveAt(list.Count - 1);

    return last;
  }

  /// <summary>The "--custom: value" declarations of one rule body. A value may contain a quoted ';'.</summary>
  public static Dictionary<string, string> CustomProperties(string body)
  {
    var declarations = new List<string>();
    var buf          = new StringBuilder();
    var scan         = new CssScanner();

    foreach (var ch in body)
    {
      if (scan.Literal(ch) || ch != ';')
      {
        buf.Append(ch);
      }
      else
      {
        declarations.Add(buf.ToString());
        buf.Clear();
      }
    }

    declarations.Add(buf.ToString());

    var found = new Dictionary<string, string>();

    foreach (var declaration in declarations)
    {
      var m = CustomProperty().Match(declaration);

      if (m.Success)
        found[m.Groups[1].Value] = m.Groups[2].Value.Trim();
    }

    return found;
  }

  /// <summary>The classes of a selector part like "body.theme-dark.mine", or null when the part is anything else.</summary>
  public static HashSet<string>? BodyClasses(string part)
  {
    var trimmed = part.Trim();

    if (trimmed.Length == 0 || !BodySelector().IsMatch(trimmed))
      return null;

    return ClassName().Matches(BodySelector().Match(trimmed).Groups[2].Value).Select(m => m.Groups[1].Value).ToHashSet();
  }

  /// <summary>Splits at commas that are not inside parentheses.</summary>
  public static List<string> SplitTop(string text)
  {
    var parts = new List<string>();
    var depth = 0;
    var cur   = new StringBuilder();

    foreach (var ch in text)
    {
      depth += (ch == '(' ? 1 : 0) - (ch == ')' ? 1 : 0);

      if (ch == ',' && depth == 0)
      {
        parts.Add(cur.ToString());
        cur.Clear();
      }
      else
      {
        cur.Append(ch);
      }
    }

    parts.Add(cur.ToString());

    return parts;
  }
}
