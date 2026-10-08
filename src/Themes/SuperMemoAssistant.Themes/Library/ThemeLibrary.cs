// The theme library: the shipped palettes, optionally merged with a user's own file; lookup by id or name.
using System.Reflection;
using System.Text.Json;

namespace SuperMemoAssistant.Themes;

public sealed class ThemeLibrary
{
  private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

  private ThemeLibrary(IReadOnlyList<ThemeEntry> entries, IReadOnlyList<string> curated)
  {
    Entries   = entries;
    CuratedIds = curated;
  }

  public IReadOnlyList<ThemeEntry> Entries { get; }

  /// <summary>Ids of the curated native set: themes that look right as SuperMemo window styles.</summary>
  public IReadOnlyList<string> CuratedIds { get; }

  /// <summary>The shipped palettes plus the user's file when it exists; on the same id the user's entry wins.</summary>
  public static ThemeLibrary Load(string? userLibraryPath = null)
  {
    var merged = new Dictionary<string, ThemeEntry>();

    foreach (var e in Parse(ReadResource("themes.json")))
      merged[e.Id] = e;

    if (userLibraryPath is not null && File.Exists(userLibraryPath))
    {
      foreach (var e in Parse(File.ReadAllText(userLibraryPath)))
        merged[e.Id] = e;
    }

    var curated = ReadResource("native-themes.txt").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    return new ThemeLibrary([.. merged.Values], curated);
  }

  /// <summary>Creates a library from entries, for tests and tools.</summary>
  internal static ThemeLibrary From(IEnumerable<ThemeEntry> entries, IEnumerable<string>? curated = null) => new([.. entries], [.. curated ?? []]);

  /// <summary>Finds one theme by id or name. A partial id works when it names exactly one theme.</summary>
  public ThemeEntry Find(string query)
  {
    var q     = Palette.Slug(query);
    var exact = Entries.Where(e => e.Id == q || string.Equals(e.Name, query, StringComparison.OrdinalIgnoreCase)).ToList();
    var hits  = exact.Count > 0 ? exact : Entries.Where(e => e.Id.Contains(q, StringComparison.Ordinal)).ToList();

    if (hits.Count == 1)
      return hits[0];

    throw new ThemeException($"theme '{query}' matched {hits.Count}: {string.Join(", ", hits.Take(15).Select(e => e.Name))}");
  }

  public ThemeEntry? TryGet(string id) => Entries.FirstOrDefault(e => e.Id == id);

  private static List<ThemeEntry> Parse(string json) => JsonSerializer.Deserialize<List<ThemeEntry>>(json, Json) ?? [];

  private static string ReadResource(string name)
  {
    using var stream = typeof(ThemeLibrary).Assembly.GetManifestResourceStream(name)
                       ?? throw new InvalidOperationException($"embedded resource {name} is missing");
    using var reader = new StreamReader(stream);

    return reader.ReadToEnd();
  }
}
