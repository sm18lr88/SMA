// Proves SMA's symbol resolver gives the same answer for an sm20.exe that this engine rebuilt as for the original exe and the pinned table.
using SuperMemoAssistant.Hooks.Symbols;
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Styles;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Exe;

public class PatchedExeSymbolTests
{
  private static ExeState State(string kind, string resource, byte[] style) => kind switch
  {
    "styles"   => new ExeState { Styles = { [resource] = style } },
    "elements" => new ExeState { Elements = true },
    "patch"    => new ExeState { Patched = true },
    _          => new ExeState { Styles = { [resource] = style }, Elements = true, Patched = true },
  };

  [Theory]
  [InlineData("styles")]
  [InlineData("elements")]
  [InlineData("patch")]
  [InlineData("all")]
  public void Resolve_RebuiltExe_EqualsTheOriginalAndThePinnedTable(string kind)
  {
    Assert.SkipWhen(GoldenData.Sm20OriginalPath is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box = new TestSandbox();

    var exe    = box.Path_("sm20.exe");
    var backup = box.Path_("backup");

    File.Copy(GoldenData.Sm20OriginalPath!, exe);
    Directory.CreateDirectory(backup);
    File.Copy(GoldenData.Sm20OriginalPath!, Path.Combine(backup, "sm20.exe.original"));

    var styles   = ExeResources.ReadStyles(exe);
    var entry    = ThemeLibrary.Load().Find("nord");
    var resource = StyleNames.ResourceName(entry);
    var style    = StyleNames.BuildStyle(entry, styles, "Nord");

    ExeBuilder.Build(exe, backup, State(kind, resource, style));

    Assert.True(ExeBuilder.HasChanges(exe), "the build must have changed the exe, or this test proves nothing");

    var original = SymbolResolver.Resolve(GoldenData.Sm20OriginalPath!);
    var rebuilt  = SymbolResolver.Resolve(exe);

    Assert.Equal(original.BuildId, rebuilt.BuildId);
    Assert.Empty(rebuilt.Diff(original));
    Assert.Empty(rebuilt.Diff(SymbolResolver.Pinned(rebuilt.BuildId)!));
  }

  [Fact]
  public void Resolve_TheLiveExeThatSmcardsPatched_EqualsTheOriginal()
  {
    Assert.SkipWhen(GoldenData.Sm20OriginalPath is null || GoldenData.Sm20Path is null, "the original and the live sm20.exe are both needed.");
    Assert.SkipUnless(ExeBuilder.ProgramHash(File.ReadAllBytes(GoldenData.Sm20Path!)) == ExeBuilder.ProgramHash(File.ReadAllBytes(GoldenData.Sm20OriginalPath!)), "the live exe is another build.");

    Assert.Empty(SymbolResolver.Resolve(GoldenData.Sm20Path!).Diff(SymbolResolver.Resolve(GoldenData.Sm20OriginalPath!)));
  }

  [Fact]
  public void Diff_ReportsADifferentBuild_SoTheEqualityChecksAboveCanFail()
  {
    Assert.SkipWhen(GoldenData.Sm20OriginalPath is null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box = new TestSandbox();

    var exe   = box.Path_("sm20.exe");
    var bytes = File.ReadAllBytes(GoldenData.Sm20OriginalPath!);
    var coff  = BitConverter.ToInt32(bytes, 0x3C) + 8; // COFF TimeDateStamp, part of the build id

    BitConverter.GetBytes(BitConverter.ToUInt32(bytes, coff) + 1).CopyTo(bytes, coff);
    File.WriteAllBytes(exe, bytes);

    var diffs = SymbolResolver.Resolve(exe).Diff(SymbolResolver.Resolve(GoldenData.Sm20OriginalPath!)).ToList();

    Assert.Contains(diffs, d => d.StartsWith("BuildId"));
  }
}