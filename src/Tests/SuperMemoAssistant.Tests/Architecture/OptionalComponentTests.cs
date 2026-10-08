// Guards the architecture: the core and the plugin API never depend on Themes, and Themes ships only through the plugin feed.
namespace SuperMemoAssistant.Tests.Architecture;

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.Hooks.Symbols;
using Xunit;

public class OptionalComponentTests
{
  private const string ThemesPackage = "SuperMemoAssistant.Plugins.Themes";

  internal static IReadOnlyList<string> ReferencesWithPrefix(Assembly assembly, string prefix) =>
    assembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith(prefix, StringComparison.Ordinal)).Order().ToList();

  private static string RepositoryRoot()
  {
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
      if (File.Exists(Path.Combine(dir.FullName, "SuperMemoAssistant.slnx")))
        return dir.FullName;
    }

    throw new InvalidOperationException("repository root not found");
  }

  [Theory]
  [InlineData(typeof(SMA.SMA))]
  [InlineData(typeof(ISuperMemoAssistant))]
  [InlineData(typeof(SymbolResolver))]
  public void CoreAndPluginApi_DoNotReferenceThemes(Type fromAssembly)
  {
    Assert.Empty(ReferencesWithPrefix(fromAssembly.Assembly, "SuperMemoAssistant.Themes"));
    Assert.Empty(ReferencesWithPrefix(fromAssembly.Assembly, ThemesPackage));
  }

  [Fact]
  public void ReferenceCheck_CanFail_BecauseThisAssemblyDoesReferenceTheThemesPlugin()
  {
    Assert.NotEmpty(ReferencesWithPrefix(typeof(OptionalComponentTests).Assembly, ThemesPackage));
  }

  [Fact]
  public void Themes_IsNotBundledIntoTheInstaller()
  {
    var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "build", "pack.ps1"));
    var list   = Regex.Match(script, @"\$bundledPlugins\s*=\s*@\((?<list>.*?)\r?\n\)", RegexOptions.Singleline);

    Assert.True(list.Success, "the $bundledPlugins list in pack.ps1 was not found");
    Assert.Contains("Plugins.Writing", list.Groups["list"].Value);
    Assert.DoesNotContain("Themes", list.Groups["list"].Value);
  }

  [Fact]
  public void Themes_IsInThePluginFeedCatalog_SoItIsAnOptionalInstall()
  {
    var catalog = File.ReadAllText(Path.Combine(RepositoryRoot(), "build", "plugin-feed", "catalog.json"));
    var plugins = JsonDocument.Parse(catalog, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }).RootElement.EnumerateArray();
    var themes  = plugins.Single(p => p.GetProperty("PackageName").GetString() == ThemesPackage);

    Assert.Equal("Themes", themes.GetProperty("DisplayName").GetString());
    Assert.Contains("Themes", themes.GetProperty("Project").GetString());
  }
}
