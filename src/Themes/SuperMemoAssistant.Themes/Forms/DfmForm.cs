// Edits Delphi binary form resources (TPF0): rewrites one property, StyleElements.
using System.Text;

namespace SuperMemoAssistant.Themes.Forms;

internal static class DfmForm
{
  public static DfmObject Parse(byte[] form)
  {
    if (form.Length < 4 || form[0] != 'T' || form[1] != 'P' || form[2] != 'F' || form[3] != '0')
      throw new InvalidDataException("not a binary Delphi form");

    return DfmReader.Read(form);
  }

  private static byte[] SetBytes(IEnumerable<string> elements)
  {
    var bytes = new List<byte> { 0x0B };

    foreach (var e in elements)
    {
      bytes.Add((byte)e.Length);
      bytes.AddRange(Encoding.ASCII.GetBytes(e));
    }

    bytes.Add(0);

    return [.. bytes];
  }

  /// <summary>Returns the form with StyleElements of each named object set; inserted when absent.</summary>
  public static byte[] SetStyleElements(byte[] form, IReadOnlyDictionary<string, string[]> wanted)
  {
    var root  = Parse(form);
    var edits = new List<(int Start, int End, byte[] Replacement)>();

    foreach (var (name, elements) in wanted)
    {
      var node = root.Find(name) ?? throw new KeyNotFoundException($"object '{name}' not in form {root.Class}");

      if (node.Spans.TryGetValue("StyleElements", out var span))
        edits.Add((span.Start, span.End, SetBytes(elements)));
      else
        edits.Add((node.PropsEnd, node.PropsEnd, [.. new byte[] { 0x0D }, .. "StyleElements"u8.ToArray(), .. SetBytes(elements)]));
    }

    var output = form;

    foreach (var (start, end, replacement) in edits.OrderByDescending(e => e.Start).ThenByDescending(e => e.End))
      output = [.. output.AsSpan(0, start), .. replacement, .. output.AsSpan(end)];

    var check = Parse(output);

    foreach (var (name, elements) in wanted)
    {
      var node = check.Find(name);

      if (node is null || node.Values.GetValueOrDefault("StyleElements") is not HashSet<string> set || !set.SetEquals(elements))
        throw new InvalidOperationException($"StyleElements edit of {name} did not round-trip");
    }

    return output;
  }

  /// <summary>The set of StyleElements names of an object, or null when the property is not stored.</summary>
  public static HashSet<string>? StyleElementsOf(DfmObject node) => node.Values.GetValueOrDefault("StyleElements") as HashSet<string>;
}
