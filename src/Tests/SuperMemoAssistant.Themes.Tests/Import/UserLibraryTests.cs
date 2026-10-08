// Checks how imports are stored: id clashes, the user library file, the library that reads it back, and the real Obsidian defaults when installed.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SuperMemoAssistant.Themes.Import;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Import;

public class UserLibraryTests
{
  private static readonly JsonElement MergeData = ImportFixtures.Data.GetProperty("merge");

  private static ThemeEntry EntryOf(JsonElement e) =>
    new(e.GetProperty("id").GetString()!, e.GetProperty("name").GetString()!, e.GetProperty("variant").GetString()!, e.GetProperty("source").GetString()!, ImportFixtures.Roles(e.GetProperty("roles")));

  [Fact]
  public void Merge_TagsAClashingIdWithItsSource_LikeTheReference()
  {
    var library = MergeData.GetProperty("library").EnumerateArray().Select(EntryOf).ToList();
    var added   = ThemeImporter.Merge(library, MergeData.GetProperty("new").EnumerateArray().Select(EntryOf));

    ImportFixtures.AssertSameList(MergeData.GetProperty("added"), added, "merge");
  }

  [Fact]
  public void Import_WritesTheUserLibrary_AndTheLibraryReadsItBack()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, ImportFixtures.Data.GetProperty("yaml").GetProperty("files"));

    var library  = ThemeLibrary.Load();
    var importer = new ThemeImporter(library, box.Path_("user-themes.json"));
    var result   = importer.Import(box.Path_("a-dark.yaml"));
    var reloaded = ThemeLibrary.Load(box.Path_("user-themes.json"));

    Assert.Equal("Test Nord", Assert.Single(result.Added).Name);
    Assert.Equal(library.Entries.Count + 1, reloaded.Entries.Count);
    Assert.Equal(result.Added[0].Roles, reloaded.Find("test-nord").Roles);
  }

  [Fact]
  public void ImportingTheSameThemeTwice_KeepsOneEntry_AndANewerFileWins()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, ImportFixtures.Data.GetProperty("yaml").GetProperty("files"));

    var importer = new ThemeImporter(ThemeLibrary.From([]), box.Path_("user-themes.json"));

    importer.Import(box.Path_("a-dark.yaml"));
    box.Write("a-dark.yaml", box.Read("a-dark.yaml").Replace("2e3440", "112233"));
    importer.Import(box.Path_("a-dark.yaml"));

    var stored = JsonSerializer.Deserialize<List<ThemeEntry>>(File.ReadAllText(box.Path_("user-themes.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    Assert.Equal("#112233", Assert.Single(stored).Roles["bg"]);
  }

  [Fact]
  public void TheUserLibraryFile_IsSortedByName_UsesLowerCaseKeys_AndLeavesNoTempFile()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, ImportFixtures.Data.GetProperty("yaml").GetProperty("files"));
    new ThemeImporter(ThemeLibrary.From([]), box.Path_("user-themes.json")).Import(box.Root);

    using var document = JsonDocument.Parse(File.ReadAllText(box.Path_("user-themes.json")));
    var       names    = document.RootElement.EnumerateArray().Select(e => e.GetProperty("name").GetString()!).ToList();

    Assert.Equal(names.OrderBy(n => n.ToLowerInvariant(), StringComparer.Ordinal), names);
    Assert.True(document.RootElement[0].TryGetProperty("roles", out _));
    Assert.False(File.Exists(box.Path_("user-themes.json.tmp")));
  }

  [Fact]
  public void ADamagedUserLibrary_IsReportedAndNotOverwritten()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, ImportFixtures.Data.GetProperty("yaml").GetProperty("files"));
    box.Write("user-themes.json", "{ not json");

    var ex = Assert.Throws<ThemeException>(() => new ThemeImporter(ThemeLibrary.From([]), box.Path_("user-themes.json")).Import(box.Path_("a-dark.yaml")));

    Assert.Contains("damaged", ex.Message);
    Assert.Equal("{ not json", box.Read("user-themes.json"));
  }

  [Fact]
  public void WithoutObsidian_AnObsidianImportUsesTheBuiltInDefaults_AndSaysSo()
  {
    var saved = Environment.GetEnvironmentVariable("OBSIDIAN_ASAR");

    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, ImportFixtures.Data.GetProperty("obsidian").GetProperty("files"));

    var css = ObsidianBase.Load(box.Path_("none.css"), out var note);

    if (ObsidianBase.FindAsar() is null)
    {
      Assert.Equal(ObsidianBase.FallbackCss, css);
      Assert.Equal(ObsidianBase.NotInstalledNote, note);
    }
    else
    {
      Assert.Null(note); // an installed Obsidian supplies the real defaults
    }

    Assert.Equal(saved, Environment.GetEnvironmentVariable("OBSIDIAN_ASAR"));
  }

  [Fact]
  public void RealObsidian_DefaultsAndImportMatchTheReference_WhenTheSameBuildIsInstalled()
  {
    var real = ImportFixtures.Data.GetProperty("obsidian").GetProperty("real");
    var asar = ObsidianBase.FindAsar();

    Assert.SkipWhen(asar is null || real.ValueKind == JsonValueKind.Null, "Obsidian is not installed here.");
    Assert.SkipUnless(new FileInfo(asar!).Length == real.GetProperty("asar_size").GetInt64(), "another Obsidian build than the one the golden hashes came from.");

    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, ImportFixtures.Data.GetProperty("obsidian").GetProperty("files"));

    var cache = box.Path_("cache.css");
    var css   = ObsidianBase.Load(cache, out var note);

    Assert.Null(note);
    Assert.Equal(real.GetProperty("base_sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(css.Replace("\r\n", "\n")))));
    Assert.Equal(real.GetProperty("rule_count").GetInt32(), CssSheet.Rules(css).Count);
    Assert.Equal(css, ObsidianBase.Load(cache, out _)); // the second read comes from the cache

    var imported = ObsidianImporter.Import(box.Path_("MyTheme"), css);

    Assert.Equal(real.GetProperty("entry_names").EnumerateArray().Select(x => x.GetString()), imported.Select(e => e.Name));
    Assert.Equal(real.GetProperty("entry_hashes").EnumerateArray().Select(x => x.GetString()), imported.Select(HashOf));
  }

  private static string HashOf(ThemeEntry e)
  {
    var sorted = new SortedDictionary<string, object>(StringComparer.Ordinal)
    {
      ["id"] = e.Id, ["name"] = e.Name, ["variant"] = e.Variant, ["source"] = e.Source,
      ["roles"] = new SortedDictionary<string, string>(e.Roles.ToDictionary(k => k.Key, k => k.Value), StringComparer.Ordinal),
    };

    return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(PythonJson(sorted))));
  }

  /// <summary>json.dumps(obj, sort_keys=True) for string-only objects: ", " and ": " separators.</summary>
  private static string PythonJson(object value) => value switch
  {
    string s                             => JsonSerializer.Serialize(s),
    SortedDictionary<string, object> d   => "{" + string.Join(", ", d.Select(kv => $"{JsonSerializer.Serialize(kv.Key)}: {PythonJson(kv.Value)}")) + "}",
    SortedDictionary<string, string> d   => "{" + string.Join(", ", d.Select(kv => $"{JsonSerializer.Serialize(kv.Key)}: {JsonSerializer.Serialize(kv.Value)}")) + "}",
    _                                    => throw new NotSupportedException(),
  };
}
