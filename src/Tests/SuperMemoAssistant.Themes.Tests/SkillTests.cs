// Guards the supermemo-appearance agent skill that ships in the repository: it names no machine location and its links resolve.
using System.Text.RegularExpressions;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests;

public sealed partial class SkillTests
{
  private static readonly string SkillFolder = FindSkillFolder();

  private static string FindSkillFolder()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);

    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
      dir = dir.Parent;

    return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Repository root not found."), "skills", "supermemo-appearance");
  }

  private static IEnumerable<string> TextFiles() =>
    Directory.EnumerateFiles(SkillFolder, "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".md") || f.EndsWith(".yaml"));

  [Fact]
  public void SkillFile_HasTheNameAndADescription()
  {
    var text = File.ReadAllText(Path.Combine(SkillFolder, "SKILL.md")).ReplaceLineEndings("\n");

    Assert.StartsWith("---", text);
    Assert.Contains("\nname: supermemo-appearance\n", text);
    Assert.Matches(@"\ndescription: "".{80,}""\n", text);
  }

  [Fact]
  public void SkillFiles_NameNoDriveOrUserFolder()
  {
    var found = TextFiles()
                .SelectMany(f => File.ReadLines(f).Select((line, i) => (File: Path.GetFileName(f), Line: i + 1, Text: line)))
                .Where(x => DrivePath().IsMatch(x.Text) || x.Text.Contains(@"\Users\", StringComparison.OrdinalIgnoreCase))
                .Select(x => $"{x.File}:{x.Line}: {x.Text.Trim()}")
                .ToList();

    Assert.Empty(found);
  }

  [Fact]
  public void SkillLinks_PointToFilesThatExist()
  {
    var missing = TextFiles()
                  .SelectMany(f => RelativeLink().Matches(File.ReadAllText(f)).Select(m => (Folder: Path.GetDirectoryName(f)!, Target: m.Groups[1].Value)))
                  .Where(x => !File.Exists(Path.GetFullPath(Path.Combine(x.Folder, x.Target))))
                  .Select(x => x.Target)
                  .ToList();

    Assert.Empty(missing);
  }

  [GeneratedRegex(@"\b[A-Za-z]:\\(?!\\)")]
  private static partial Regex DrivePath();

  [GeneratedRegex(@"\]\(((?!https?:|#)[^)\s]+\.md)\)")]
  private static partial Regex RelativeLink();
}
