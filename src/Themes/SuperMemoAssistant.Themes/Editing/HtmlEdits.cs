// Edits the HTML files of card components: find and replace, and removal of inline CSS properties. Reads and writes in the file's own encoding.
using System.Text;
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes.Editing;

internal static partial class HtmlEdits
{
  [GeneratedRegex(@"\s+style\s*=\s*([""'])(.*?)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
  private static partial Regex StyleAttribute();

  [GeneratedRegex(@"\\(?:g<(?<name>[^>]+)>|(?<digits>[0-9]{1,3})|(?<escape>.))", RegexOptions.Singleline)]
  private static partial Regex ReplacementEscape();

  /// <summary>Reads a file as UTF-8 if it is valid UTF-8, else in the Windows code page (undefined bytes become U+FFFD).</summary>
  public static (string Text, Encoding Encoding) Read(string path, Encoding ansi)
  {
    var raw = File.ReadAllBytes(path);

    try
    {
      return (new UTF8Encoding(false, true).GetString(raw), new UTF8Encoding(false, true));
    }
    catch (DecoderFallbackException)
    {
    }

    string text;

    try
    {
      text = ansi.GetString(raw);
    }
    catch (DecoderFallbackException)
    {
      text = Encoding.GetEncoding(ansi.CodePage, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback).GetString(raw);
    }

    if (!HasUndefinedBytes(ansi, text))
      return (text, ansi);

    return (UndefinedCharacter().Replace(text, "\uFFFD"), ansi);
  }

  /// <summary>
  ///   .NET maps the bytes that a Windows single-byte code page leaves undefined (0x81 in 1252) to the control characters
  ///   U+0080 to U+009F, where Python refuses them. Defined bytes never decode to those characters in these code pages.
  /// </summary>
  private static bool HasUndefinedBytes(Encoding ansi, string decoded) =>
    ansi is { IsSingleByte: true, CodePage: >= 1250 and <= 1258 } && UndefinedCharacter().IsMatch(decoded);

  [GeneratedRegex("[\u0080-\u009f]")]
  private static partial Regex UndefinedCharacter();

  /// <summary>Encodes text; a character that the encoding cannot hold is written as an XML character reference, like &amp;#26085;.</summary>
  public static byte[] Write(string text, Encoding encoding)
  {
    try
    {
      return encoding.GetBytes(text);
    }
    catch (EncoderFallbackException)
    {
      var output = new List<byte>();

      foreach (var rune in text.EnumerateRunes())
      {
        var piece = rune.ToString();

        try
        {
          output.AddRange(encoding.GetBytes(piece));
        }
        catch (EncoderFallbackException)
        {
          output.AddRange(Encoding.ASCII.GetBytes($"&#{rune.Value};"));
        }
      }

      return [.. output];
    }
  }

  /// <summary>The existing HTML files of the components of <paramref name="records" />, once each (paths compare without case).</summary>
  public static List<string> Files(IEnumerable<CardRecord> records)
  {
    var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var component in records.SelectMany(r => r.Components))
    {
      if (component.Path is not null && File.Exists(component.Path))
        seen.TryAdd(component.Path, component.Path);
    }

    return [.. seen.Values];
  }

  public static CardEditPlan Plan(IEnumerable<string> files, Func<string, string> transform, Encoding ansi)
  {
    var plan = new CardEditPlan([], []);

    foreach (var file in files)
    {
      var (old, encoding) = Read(file, ansi);
      var changed         = transform(old);

      if (changed != old)
      {
        plan.Notes.AddRange(PreviewDiff.Describe(file, old, changed));
        plan.Changes.Add(new(file, Write(changed, encoding)));
      }
    }

    return plan;
  }

  /// <summary>A text replacement. In regex mode the replacement uses Python's syntax (\1, \g&lt;name&gt;, \n); otherwise it is literal.</summary>
  public static Func<string, string> Replacer(string find, string replacement, bool regex, bool ignoreCase)
  {
    var options = ignoreCase ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant : RegexOptions.CultureInvariant;

    Regex pattern;

    try
    {
      pattern = new Regex(regex ? ToDotNetPattern(find) : Regex.Escape(find), options);
    }
    catch (ArgumentException ex)
    {
      throw new ThemeException($"'{find}' is not a valid pattern: {ex.Message}", ex);
    }

    if (!regex)
      return text => pattern.Replace(text, _ => replacement);

    var template = ParseReplacement(replacement, pattern);

    return text => pattern.Replace(text, template);
  }

  /// <summary>Python writes named groups as (?P&lt;n&gt;...) and (?P=n); .NET writes (?&lt;n&gt;...) and \k&lt;n&gt;.</summary>
  private static string ToDotNetPattern(string pattern) =>
    Regex.Replace(pattern.Replace("(?P<", "(?<"), @"\(\?P=(\w+)\)", @"\k<$1>");

  private static MatchEvaluator ParseReplacement(string template, Regex pattern)
  {
    var parts = new List<Func<Match, string>>();
    var last  = 0;

    foreach (Match escape in ReplacementEscape().Matches(template))
    {
      if (escape.Index > last)
        parts.Add(Literal(template[last..escape.Index]));

      last = escape.Index + escape.Length;

      if (escape.Groups["name"].Success)
        parts.Add(GroupReference(escape.Groups["name"].Value, pattern));
      else if (escape.Groups["digits"].Success)
        parts.AddRange(Digits(escape.Groups["digits"].Value, pattern));
      else
        parts.Add(Literal(Escaped(escape.Groups["escape"].Value[0])));
    }

    if (last < template.Length)
      parts.Add(Literal(template[last..]));

    return match => string.Concat(parts.Select(p => p(match)));
  }

  /// <summary>\0 and \0NN are octal characters, three octal digits starting 0-3 are one too, and otherwise one or two digits name a group.</summary>
  private static IEnumerable<Func<Match, string>> Digits(string digits, Regex pattern)
  {
    var octal = digits.All(c => c is >= '0' and <= '7');

    if (digits[0] == '0' && octal)
      return [Literal(((char)Convert.ToInt32(digits, 8)).ToString())];

    if (digits.Length == 3 && octal && digits[0] <= '3')
      return [Literal(((char)Convert.ToInt32(digits, 8)).ToString())];

    var reference = digits.Length == 3 ? digits[..2] : digits;

    return digits.Length == 3 ? [GroupReference(reference, pattern), Literal(digits[2].ToString())] : [GroupReference(reference, pattern)];
  }

  private static Func<Match, string> Literal(string text) => _ => text;

  private static Func<Match, string> GroupReference(string name, Regex pattern)
  {
    if (pattern.GroupNumberFromName(name) < 0)
      throw new ThemeException($"the replacement refers to group '{name}', which the pattern does not have");

    return match => match.Groups[name].Value;
  }

  private static string Escaped(char c) => c switch
  {
    'n'  => "\n",
    't'  => "\t",
    'r'  => "\r",
    'a'  => "\a",
    'b'  => "\b",
    'f'  => "\f",
    'v'  => "\v",
    '\\' => "\\",
    _    => char.IsAsciiLetter(c) ? throw new ThemeException($"bad escape \\{c} in the replacement") : "\\" + c,
  };

  /// <summary>Removes the named CSS properties from every style="..." attribute; an attribute with nothing left is removed.</summary>
  public static Func<string, string> StyleStripper(IEnumerable<string> properties)
  {
    var drop = properties.Select(p => p.Trim().ToLowerInvariant()).ToHashSet();

    return text => StyleAttribute().Replace(text, match =>
    {
      var quote = match.Groups[1].Value;
      var keep  = match.Groups[2].Value.Split(';')
                       .Where(d => d.Trim().Length > 0)
                       .Where(d => !drop.Contains(d.Split(':', 2)[0].Trim().ToLowerInvariant()))
                       .Select(d => d.Trim())
                       .ToList();

      return keep.Count > 0 ? $" style={quote}{string.Join("; ", keep)}{quote}" : "";
    });
  }
}
