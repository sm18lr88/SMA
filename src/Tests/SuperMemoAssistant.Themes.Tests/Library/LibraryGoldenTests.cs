// Checks palette completion, slugs, style names, library lookup and palettes read from styles against the Python reference.
using System.Text.Json;
using SuperMemoAssistant.Themes.Styles;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Library;

public class LibraryGoldenTests
{
  private static readonly JsonElement Data = GoldenData.Load("library.json");

  private static Dictionary<string, string> Roles(JsonElement e) => e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);

  private static void AssertEntry(JsonElement expected, ThemeEntry actual, string context)
  {
    Assert.True(expected.GetProperty("id").GetString() == actual.Id, $"{context}: id");
    Assert.True(expected.GetProperty("name").GetString() == actual.Name, $"{context}: name");
    Assert.True(expected.GetProperty("variant").GetString() == actual.Variant, $"{context}: variant");
    Assert.True(expected.GetProperty("source").GetString() == actual.Source, $"{context}: source");
    Assert.True(Roles(expected.GetProperty("roles")).OrderBy(k => k.Key).SequenceEqual(actual.Roles.OrderBy(k => k.Key)), $"{context}: roles");
    Assert.Equal(Palette.Roles, actual.Roles.Keys);
  }

  [Fact]
  public void Slug_MatchesReference()
  {
    foreach (var c in Data.GetProperty("slug").EnumerateArray())
      Assert.Equal(c.GetProperty("out").GetString(), Palette.Slug(c.GetProperty("in").GetString()!));
  }

  [Fact]
  public void Complete_MatchesReference_ForPartialRoleSets()
  {
    foreach (var c in Data.GetProperty("complete").EnumerateArray())
    {
      var roles   = c.GetProperty("roles").EnumerateObject().ToDictionary(p => p.Name, p => (string?)p.Value.GetString());
      var variant = c.GetProperty("variant").GetString();
      var actual  = Palette.Complete(roles, c.GetProperty("name").GetString()!, c.GetProperty("source").GetString()!, variant);

      AssertEntry(c.GetProperty("out"), actual, c.GetProperty("name").GetString()!);
    }
  }

  [Fact]
  public void ResourceAndWindowStyleNames_MatchReference()
  {
    var builtin = Data.GetProperty("builtin_names").EnumerateArray().Select(x => x.GetString()!).ToHashSet();

    foreach (var c in Data.GetProperty("names").EnumerateArray())
    {
      var entry = new ThemeEntry(c.GetProperty("id").GetString()!, c.GetProperty("name").GetString()!, "dark", "t", new Dictionary<string, string>());

      Assert.Equal(c.GetProperty("resource").GetString(), StyleNames.ResourceName(entry));
      Assert.Equal(c.GetProperty("window_name").GetString(), StyleNames.WindowStyleName(entry, builtin));
    }
  }

  [Fact]
  public void WindowStyleName_AddsASuffixOnlyForBuiltinNames_AndKeepsTheNameAscii()
  {
    var plain = new ThemeEntry("rose", "Rosé Pine", "dark", "t", new Dictionary<string, string>());

    Assert.Equal("Rose Pine", StyleNames.WindowStyleName(plain, new HashSet<string>()));
    Assert.Equal("Rose Pine (smcards)", StyleNames.WindowStyleName(plain, new HashSet<string> { "Rose Pine" }));
  }

  [Fact]
  public void Find_MatchesReference_IncludingTheErrorText()
  {
    var library = ThemeLibrary.Load();

    foreach (var c in Data.GetProperty("find").EnumerateArray())
    {
      var query = c.GetProperty("query").GetString()!;

      if (c.TryGetProperty("error", out var error))
        Assert.Equal(error.GetString(), Assert.Throws<ThemeException>(() => library.Find(query)).Message);
      else
        Assert.Equal(c.GetProperty("id").GetString(), library.Find(query).Id);
    }
  }

  [Fact]
  public void EntryFromStyle_MatchesReference()
  {
    foreach (var c in Data.GetProperty("from_style").EnumerateArray())
      AssertEntry(c.GetProperty("entry"), StyleNames.EntryFromStyle(Convert.FromHexString(c.GetProperty("blob_hex").GetString()!)), "from style");
  }

  [Fact]
  public void Load_ReadsTheShippedLibraryAndTheCuratedSet()
  {
    var library = ThemeLibrary.Load();

    Assert.Equal(Data.GetProperty("library_count").GetInt32(), library.Entries.Count);
    Assert.Equal(Data.GetProperty("first_ids").EnumerateArray().Select(x => x.GetString()), library.Entries.Take(5).Select(e => e.Id));
    Assert.NotEmpty(library.CuratedIds);
    Assert.All(library.CuratedIds, id => Assert.NotNull(library.TryGet(id)));
  }

  [Fact]
  public void Load_LetsTheUsersEntryWinOnTheSameId()
  {
    using var box = new TestSandbox();

    box.Write("user.json", """[{"id":"nord","name":"Nord Mine","variant":"dark","source":"user","roles":{"bg":"#000000"}}]""");

    var library = ThemeLibrary.Load(box.Path_("user.json"));

    Assert.Equal("Nord Mine", library.TryGet("nord")!.Name);
    Assert.Equal(ThemeLibrary.Load().Entries.Count, library.Entries.Count);
  }
}
