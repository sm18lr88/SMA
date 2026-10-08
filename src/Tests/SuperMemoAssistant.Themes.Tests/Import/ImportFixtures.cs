// Loads the importer golden data, writes its fixture files to a temp folder, and compares entries with the reference.
using System.Text;
using System.Text.Json;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Import;

internal static class ImportFixtures
{
  public static readonly JsonElement Data = GoldenData.Load("import.json");

  public static void Materialize(TestSandbox box, JsonElement files)
  {
    foreach (var file in files.EnumerateObject())
    {
      var path = box.Path_(file.Name.Replace('/', '\\'));

      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(file.Value.GetString()!));
    }
  }

  public static Dictionary<string, string> Roles(JsonElement e) => e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);

  public static void AssertSame(JsonElement expected, ThemeEntry actual, string context)
  {
    Assert.True(expected.GetProperty("id").GetString() == actual.Id, $"{context}: id {actual.Id}");
    Assert.True(expected.GetProperty("name").GetString() == actual.Name, $"{context}: name {actual.Name}");
    Assert.True(expected.GetProperty("variant").GetString() == actual.Variant, $"{context}: variant {actual.Variant}");
    Assert.True(expected.GetProperty("source").GetString() == actual.Source, $"{context}: source {actual.Source}");
    Assert.True(Roles(expected.GetProperty("roles")).OrderBy(k => k.Key).SequenceEqual(actual.Roles.OrderBy(k => k.Key)), $"{context}: roles differ");
  }

  public static void AssertSameList(JsonElement expected, IReadOnlyList<ThemeEntry> actual, string context)
  {
    var list = expected.EnumerateArray().ToList();

    Assert.True(list.Count == actual.Count, $"{context}: expected {list.Count} entries, got {actual.Count} ({string.Join(", ", actual.Select(a => a.Name))})");

    for (var i = 0; i < list.Count; i++)
      AssertSame(list[i], actual[i], $"{context}[{i}]");
  }
}
