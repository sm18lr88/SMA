// Checks that SuperMemo is found from SMA's own setup, with SMA's data folder rules, and that no location is assumed.
using System.Text.Json;
using SuperMemoAssistant.Themes.Editing;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Editing;

public sealed class SmaDataTests
{
  private static string Json(string value) => JsonSerializer.Serialize(value);

  private static string FakeInstall(TestSandbox box, string name)
  {
    box.Write($@"{name}\sm20.exe", "x");
    Directory.CreateDirectory(box.Path_($@"{name}\bin"));

    return box.Path_(name);
  }

  private static string SetUpSma(TestSandbox box, string smExe)
  {
    box.Write(@"data\SuperMemoAssistant\Configs\Core\CoreCfg.json", $$"""{"SuperMemo":{"SMBinPath":{{Json(smExe)}}},"HasImportedCollections":true}""");

    return box.Path_(@"data\SuperMemoAssistant");
  }

  [Fact]
  public void Folder_UsesTheVariableFirst()
  {
    using var box = new TestSandbox();

    box.Write("profile/supermemoassistant.json", $$"""{"AppDataDirPath":{{Json(box.Path_("elsewhere"))}}}""");
    Directory.CreateDirectory(box.Path_("elsewhere"));

    Assert.Equal(Path.Combine("custom", "SuperMemoAssistant"), SmaData.Folder("custom", box.Path_("profile")));
  }

  [Fact]
  public void Folder_UsesTheFolderInThePreInitFile_WhenItExists()
  {
    using var box = new TestSandbox();

    Directory.CreateDirectory(box.Path_("elsewhere"));
    box.Write("profile/supermemoassistant.json", $$"""{"AppDataDirPath":{{Json(box.Path_("elsewhere"))}}}""");

    Assert.Equal(box.Path_(@"elsewhere\SuperMemoAssistant"), SmaData.Folder(null, box.Path_("profile")));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("{ not json")]
  [InlineData("""{"AppDataDirPath":"Z:\\no\\such\\folder"}""")]
  [InlineData("""{"AppDataDirPath":5}""")]
  public void Folder_FallsBackToTheUserProfile_WhenThePreInitFileIsMissingOrUnusable(string? content)
  {
    using var box = new TestSandbox();

    if (content is not null)
      box.Write("profile/supermemoassistant.json", content);

    Directory.CreateDirectory(box.Path_("profile"));

    Assert.Equal(box.Path_(@"profile\SuperMemoAssistant"), SmaData.Folder(null, box.Path_("profile")));
  }

  [Fact]
  public void ConfiguredExe_ReturnsTheExeOfTheSetup_AndNullWhenItIsMissing()
  {
    using var box = new TestSandbox();

    var root = FakeInstall(box, "sm");
    var data = SetUpSma(box, Path.Combine(root, "sm20.exe"));

    Assert.Equal(Path.Combine(root, "sm20.exe"), SmaData.ConfiguredExe(data));
    Assert.Null(SmaData.ConfiguredExe(box.Path_("nothing")));

    File.Delete(Path.Combine(root, "sm20.exe"));
    Assert.Null(SmaData.ConfiguredExe(data));
  }

  [Fact]
  public void FindRoot_ExplicitFolderBeatsTheSmaSetup()
  {
    using var box = new TestSandbox();

    var explicitRoot = FakeInstall(box, "chosen");
    var configured   = FakeInstall(box, "configured");
    var data         = SetUpSma(box, Path.Combine(configured, "sm20.exe"));

    Assert.Equal(explicitRoot, SuperMemoLocator.FindRoot(explicitRoot, data));
  }

  [Fact]
  public void FindRoot_FollowsTheExeThatSmaIsSetUpWith_WhereverItIs()
  {
    Assert.SkipWhen(Environment.GetEnvironmentVariable(SuperMemoLocator.RootVariable) is { Length: > 0 }, $"{SuperMemoLocator.RootVariable} is set.");

    using var box = new TestSandbox();

    var root = FakeInstall(box, @"any\place\you\like");
    var data = SetUpSma(box, Path.Combine(root, "sm20.exe"));

    Assert.Equal(root, SuperMemoLocator.FindRoot(null, data));
  }

  [Fact]
  public void UserLibrary_IsInTheDataFolderOfThePlugin()
  {
    Assert.Equal(Path.Combine("data", "Configs", "SuperMemoAssistant.Plugins.Themes", "user-themes.json"), SmaData.UserLibrary("data"));
  }
}
