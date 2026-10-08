// Guards the theme engine's boundary: a small public contract, and no dependency on SMA, WPF or the plugin framework.
using System.Reflection;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests;

public class ArchitectureTests
{
  /// <summary>The documented contract of the engine (ADR 0003). Everything else is internal.</summary>
  private static readonly string[] ExpectedPublicTypes =
  [
    nameof(AppliedState), nameof(CardCommandLine), nameof(ImportResult), nameof(InstalledStyle), nameof(SuperMemoInstall), nameof(ThemeEntry),
    nameof(ThemeException), nameof(ThemeImporter), nameof(ThemeInstaller), nameof(ThemeLibrary), nameof(ThemeReport),
    nameof(ThemeSettings), nameof(ThemeStatus),
  ];

  private static readonly string[] ForbiddenReferencePrefixes =
  [
    "SuperMemoAssistant", "PluginManager", "PresentationFramework", "PresentationCore", "WindowsBase", "System.Windows", "Newtonsoft",
  ];

  private static Assembly Engine => typeof(ThemeInstaller).Assembly;

  /// <summary>Names of referenced assemblies that start with one of the forbidden prefixes.</summary>
  internal static IReadOnlyList<string> ForbiddenReferences(Assembly assembly, IEnumerable<string> prefixes)
  {
    var list = prefixes.ToList();

    return assembly.GetReferencedAssemblies()
                   .Select(a => a.Name!)
                   .Where(name => list.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
                   .Order()
                   .ToList();
  }

  [Fact]
  public void Engine_PublicSurface_IsExactlyTheDocumentedContract()
  {
    var actual = Engine.GetExportedTypes().Select(t => t.Name).Order().ToList();

    Assert.Equal(ExpectedPublicTypes.Order(), actual);
  }

  [Fact]
  public void Engine_DependsOnlyOnTheBaseClassLibrary()
  {
    Assert.Empty(ForbiddenReferences(Engine, ForbiddenReferencePrefixes));
  }

  [Fact]
  public void DependencyCheck_CanFail_BecauseItFlagsAnAssemblyThatDoesReferenceTheEngine()
  {
    var flagged = ForbiddenReferences(typeof(ArchitectureTests).Assembly, ["SuperMemoAssistant.Themes"]);

    Assert.Contains("SuperMemoAssistant.Themes", flagged);
  }

  [Fact]
  public void TheCardTool_ReferencesOnlyTheEngine_AndHoldsNoLogicOfItsOwn()
  {
    var root    = FindRepositoryRoot();
    var project = File.ReadAllText(Path.Combine(root, "src", "Tools", "SuperMemoAssistant.CardTool", "SuperMemoAssistant.CardTool.csproj"));
    var program = File.ReadAllLines(Path.Combine(root, "src", "Tools", "SuperMemoAssistant.CardTool", "Program.cs")).Where(l => !l.StartsWith("//") && l.Trim().Length > 0).ToList();

    Assert.Equal(["SuperMemoAssistant.Themes.csproj"], System.Text.RegularExpressions.Regex.Matches(project, @"ProjectReference Include=""[^""]*?([^\\""]+\.csproj)""").Select(m => m.Groups[1].Value));
    Assert.True(program.Count <= 6, "the tool is a console front end; its logic belongs in the engine where it is tested");
  }

  [Fact]
  public void EveryEngineSourceFile_StaysUnder250Lines()
  {
    var root  = FindRepositoryRoot();
    var files = Directory.GetFiles(Path.Combine(root, "src", "Themes", "SuperMemoAssistant.Themes"), "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"));

    var tooLong = files.Select(f => (File: Path.GetFileName(f), Lines: File.ReadLines(f).Count())).Where(f => f.Lines > 250).ToList();

    Assert.Empty(tooLong);
  }

  private static string FindRepositoryRoot()
  {
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
      if (File.Exists(Path.Combine(dir.FullName, "SuperMemoAssistant.slnx")))
        return dir.FullName;
    }

    throw new InvalidOperationException("repository root (SuperMemoAssistant.slnx) not found above the test output");
  }
}
