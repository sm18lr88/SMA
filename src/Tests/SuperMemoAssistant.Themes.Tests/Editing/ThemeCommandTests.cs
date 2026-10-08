// Checks the read-only theme commands of the card command line: list, show, and status of a sandbox sm20.exe.
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Editing;

public sealed class ThemeCommandTests
{
  private static (int Code, string Output, string Error) Run(params string[] args)
  {
    var output = new StringWriter();
    var error  = new StringWriter();
    var code   = CardCommandLine.Run(args, output, error);

    return (code, output.ToString(), error.ToString());
  }

  private static string UserLibrary(TestSandbox box)
  {
    box.Write("user-themes.json", """[{"id":"my-own","name":"My Own","variant":"light","source":"test","roles":{"bg":"#FAFAFA","fg":"#101010","accent":"#2060C0"}}]""");

    return box.Path_("user-themes.json");
  }

  [Fact]
  public void List_WithSearch_ShowsOnlyThemesWhoseNameOrIdContainsIt()
  {
    var (code, output, _) = Run("theme", "list", "--search", "nord");
    var lines             = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    Assert.Equal(0, code);
    Assert.Contains(lines, l => l.StartsWith("Nord ", StringComparison.Ordinal));
    Assert.All(lines, l => Assert.Contains("nord", l, StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void List_WithVariant_ShowsOnlyThatVariant()
  {
    var (_, light, _) = Run("theme", "list", "--variant", "light");
    var (_, dark, _)  = Run("theme", "list", "--variant", "dark");

    Assert.NotEmpty(light);
    Assert.NotEmpty(dark);
    Assert.All(light.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => Assert.Contains(" light ", l));
    Assert.All(dark.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => Assert.Contains(" dark  ", l));
  }

  [Fact]
  public void List_WithAnUnknownVariant_IsAUsageError()
  {
    var (code, _, error) = Run("theme", "list", "--variant", "blue");

    Assert.Equal(2, code);
    Assert.Contains("invalid choice 'blue'", error);
  }

  [Fact]
  public void List_WithAUserLibrary_IncludesTheUsersThemes()
  {
    using var box = new TestSandbox();

    var (code, output, _) = Run("theme", "list", "--search", "my own", "--user-library", UserLibrary(box));

    Assert.Equal(0, code);
    Assert.StartsWith("My Own", output);
    Assert.Contains("[test]", output);
  }

  [Fact]
  public void Show_PrintsTheNameAndEveryColorRole()
  {
    var (code, output, _) = Run("theme", "show", "nord");
    var nord              = ThemeLibrary.Load().Find("nord");

    Assert.Equal(0, code);
    Assert.StartsWith($"{nord.Name} ({nord.Variant}, {nord.Source}, id nord)", output);
    Assert.All(nord.Roles, role => Assert.Contains($"{role.Key}", output));
    Assert.Contains(nord.Roles["bg"], output);
  }

  [Fact]
  public void Show_WithAnUnknownTheme_FailsWithTheNumberOfMatches()
  {
    var (code, _, error) = Run("theme", "show", "no-such-theme-anywhere");

    Assert.Equal(1, code);
    Assert.Contains("matched 0", error);
  }

  [Fact]
  public void Status_ListsTheStylesOfTheExe_AndTheActiveTheme()
  {
    Assert.SkipWhen(GoldenData.Sm20OriginalPath is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box     = new TestSandbox();
    var install       = box.InstallWithExe(GoldenData.Sm20OriginalPath!, new() { [@"systems\coll\info\compon.dat"] = "x" });
    var library       = ThemeLibrary.Load();
    var settings      = new ThemeSettings { Enabled = true, ActiveThemeId = "nord", InstalledThemeIds = ["tender"] };

    new ThemeInstaller(library, () => []).PrepareLaunch(install, settings, new AppliedState());

    var (code, output, error) = Run("--sm-root", box.Root, "--collection", install.CollectionFolder, "theme", "status");

    Assert.Equal(0, code);
    Assert.Equal("", error);
    Assert.Contains("active dark style: Nord", output);
    Assert.Contains("dark mode: on", output);
    Assert.Contains("live theme switching: on (supported by this build)", output);
    Assert.Contains("Nord", output.Split('\n').Single(l => l.StartsWith("  Nord ", StringComparison.Ordinal)));
    Assert.Contains("added", output.Split('\n').Single(l => l.StartsWith("  Nord ", StringComparison.Ordinal)));
    Assert.Contains("built-in", output);
    Assert.Contains("library themes that are in sm20.exe: ", output);
    Assert.Contains("tender", output.Split('\n').Single(l => l.StartsWith("library themes", StringComparison.Ordinal)));
  }
}
