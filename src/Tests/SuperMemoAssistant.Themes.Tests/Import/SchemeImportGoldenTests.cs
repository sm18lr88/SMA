// Checks base16/base24, VS Code and Obsidian imports against the reference, and the user library they are stored in.
using System.Text.Json;
using SuperMemoAssistant.Themes.Import;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Import;

public class SchemeImportGoldenTests
{
  private static readonly JsonElement Yaml     = ImportFixtures.Data.GetProperty("yaml");
  private static readonly JsonElement VsCode   = ImportFixtures.Data.GetProperty("vscode");
  private static readonly JsonElement Obsidian = ImportFixtures.Data.GetProperty("obsidian");

  private static ThemeImporter ImporterFor(TestSandbox box, string? obsidianCache = null) =>
    new(ThemeLibrary.From([]), box.Path_("user-themes.json"), obsidianCache);

  [Fact]
  public void EachSchemeFile_ImportsLikeTheReference_OrFailsLikeIt()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, Yaml.GetProperty("files"));

    foreach (var single in Yaml.GetProperty("singles").EnumerateObject())
    {
      var path = box.Path_(single.Name);

      if (single.Value.TryGetProperty("error", out _))
      {
        Assert.Throws<ThemeException>(() => ImporterFor(box).Import(path));

        continue;
      }

      var result = ImporterFor(box).Import(path);

      ImportFixtures.AssertSame(single.Value.GetProperty("entry"), Assert.Single(result.Added), single.Name);
    }
  }

  [Fact]
  public void ASchemeFolder_ImportsTheGoodFilesInNameOrder_AndNamesTheSkippedOnes()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, Yaml.GetProperty("files"));

    var result = ImporterFor(box).Import(box.Root);

    ImportFixtures.AssertSameList(Yaml.GetProperty("folder"), result.Added, "folder");
    Assert.Equal(Yaml.GetProperty("folder_skipped").EnumerateArray().Select(x => x.GetString()!).Order(),
                 result.Skipped.Select(s => s[..s.IndexOf(':')]).Order());
  }

  [Fact]
  public void VsCodeThemeFiles_ImportLikeTheReference()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, VsCode.GetProperty("files"));

    foreach (var file in VsCode.GetProperty("imports").EnumerateObject())
      ImportFixtures.AssertSame(file.Value, Assert.Single(ImporterFor(box).Import(box.Path_($@"vs\{file.Name}.json")).Added), file.Name);

    ImportFixtures.AssertSame(VsCode.GetProperty("named"), Assert.Single(ImporterFor(box).Import(box.Path_(@"vs\plain.json"), "Given Name").Added), "named");
  }

  [Fact]
  public void ABrokenVsCodeFile_IsRefusedWithAnActionableError()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, VsCode.GetProperty("files"));

    Assert.True(VsCode.GetProperty("broken_raises").GetBoolean());
    Assert.Contains("broken.json cannot be imported", Assert.Throws<ThemeException>(() => ImporterFor(box).Import(box.Path_(@"vs\broken.json"))).Message);
  }

  [Fact]
  public void AVsCodeExtension_ImportsEachContributedTheme_WithLocalizedNamesAndASourceTag()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, VsCode.GetProperty("files"));

    ImportFixtures.AssertSameList(VsCode.GetProperty("extension_all"), ImporterFor(box).Import(box.Path_("ext")).Added, "extension");
    ImportFixtures.AssertSameList(VsCode.GetProperty("extension_only"), ImporterFor(box).Import(box.Path_("ext"), only: ["alpha", "BETA"]).Added, "extension (only)");
  }

  [Fact]
  public void ObsidianThemes_ImportLikeTheReference_WithTheBuiltInDefaults()
  {
    using var box = new TestSandbox();

    ImportFixtures.Materialize(box, Obsidian.GetProperty("files"));

    ImportFixtures.AssertSameList(Obsidian.GetProperty("fallback_my_theme"), ObsidianImporter.Import(box.Path_("MyTheme"), ObsidianBase.FallbackCss), "MyTheme");
    ImportFixtures.AssertSameList(Obsidian.GetProperty("fallback_dark_only"), ObsidianImporter.Import(box.Path_("DarkOnly"), ObsidianBase.FallbackCss), "DarkOnly");
  }

  [Fact]
  public void PyTitle_MatchesPythonsTitleMethod()
  {
    foreach (var c in Obsidian.GetProperty("titles").EnumerateArray())
      Assert.Equal(c.GetProperty("out").GetString(), ObsidianImporter.PyTitle(c.GetProperty("text").GetString()!.Replace('-', ' ')));
  }

  [Fact]
  public void SomethingThatIsNotAThemeSource_IsRefused()
  {
    using var box = new TestSandbox();

    box.Write("notes.txt", "hello");

    Assert.Contains("don't know how to import", Assert.Throws<ThemeException>(() => ImporterFor(box).Import(box.Path_("notes.txt"))).Message);
    Assert.False(File.Exists(box.Path_("user-themes.json")));
  }
}
