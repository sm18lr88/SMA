// Chooses which elements a command applies to: by number, kind, title text, or template.
using System.Globalization;

namespace SuperMemoAssistant.Themes.Editing;

internal sealed record CardSelection(string? Elements = null, string? Kind = null, string? Grep = null, string? Template = null)
{
  /// <summary>Numbers like "53,57,60-81".</summary>
  public static HashSet<int> ParseNumbers(string spec)
  {
    var output = new HashSet<int>();

    foreach (var part in spec.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
    {
      var dash = part.IndexOf('-');
      var low  = dash < 0 ? part : part[..dash];
      var high = dash < 0 ? part : part[(dash + 1)..];

      if (!int.TryParse(low, NumberStyles.Integer, CultureInfo.InvariantCulture, out var from)
          || !int.TryParse(high.Length == 0 ? low : high, NumberStyles.Integer, CultureInfo.InvariantCulture, out var to))
        throw new ThemeException($"'{spec}' is not a list of element numbers like 53,57,60-81");

      for (var n = from; n <= to; n++)
        output.Add(n);
    }

    return output;
  }

  public List<CardRecord> Apply(CardCollection collection)
  {
    var wanted = Elements is null ? null : ParseNumbers(Elements);

    return collection.Elements.Where(r => (wanted is null || wanted.Contains(r.Number))
                                          && (Kind is null || r.Kind == Kind)
                                          && (Grep is null || r.Title.Contains(Grep, StringComparison.OrdinalIgnoreCase))
                                          && (Template is null || string.Equals(collection.TemplateName(r.TemplateId), Template, StringComparison.OrdinalIgnoreCase)))
                         .ToList();
  }
}
