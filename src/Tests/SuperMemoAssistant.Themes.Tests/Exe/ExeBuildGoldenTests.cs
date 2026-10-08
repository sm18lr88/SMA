// Rebuilds a copy of the original sm20.exe with the C# builder and compares it with the Python reference build (resources and code hash).
using System.Security.Cryptography;
using System.Text.Json;
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Styles;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Exe;

public class ExeBuildGoldenTests
{
  private static readonly JsonElement Data = GoldenData.Load("build.json");

  private static string Hash(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data));

  /// <summary>Builds the reference state (four themes, elements, live patch) into a sandbox copy of the original exe.</summary>
  internal static string BuildReferenceExe(TestSandbox box, ThemeLibrary library, IEnumerable<string> ids)
  {
    var exe    = box.Path_("sm20.exe");
    var backup = box.Path_("backup");

    File.Copy(GoldenData.Sm20OriginalPath!, exe);
    Directory.CreateDirectory(backup);
    File.Copy(GoldenData.Sm20OriginalPath!, Path.Combine(backup, "sm20.exe.original"));

    var styles  = ExeResources.ReadStyles(exe);
    var builtin = styles.Where(kv => !kv.Key.StartsWith("SMC_")).Select(kv => VsfFile.StyleName(kv.Value)).ToHashSet();
    var state   = new ExeState { Elements = true, Patched = true };

    foreach (var entry in ids.Select(id => library.TryGet(id)!))
      state.Styles[StyleNames.ResourceName(entry)] = StyleNames.BuildStyle(entry, styles, StyleNames.WindowStyleName(entry, builtin));

    ExeBuilder.Build(exe, backup, state);

    return exe;
  }

  private static bool CanRun => GoldenData.Sm20OriginalPath is not null && Data.ValueKind != JsonValueKind.Null;

  [Fact]
  public void Build_ProducesTheSameResourcesAndProgramCodeAsTheReference()
  {
    Assert.SkipUnless(CanRun, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");
    Assert.SkipUnless(ExeBuilder.ProgramHash(File.ReadAllBytes(GoldenData.Sm20OriginalPath!)) == Data.GetProperty("original_program_hash").GetString(), "the exe is another build than the one the golden hashes came from.");

    using var box = new TestSandbox();

    var exe = BuildReferenceExe(box, ThemeLibrary.Load(), Data.GetProperty("entry_ids").EnumerateArray().Select(x => x.GetString()!));

    Assert.Equal(Data.GetProperty("program_hash").GetString(), ExeBuilder.ProgramHash(File.ReadAllBytes(exe)));
    Assert.Equal(Data.GetProperty("original_program_hash").GetString(), ExeBuilder.ProgramHash(File.ReadAllBytes(exe)));

    var styles = ExeResources.ReadStyles(exe);

    Assert.Equal(Data.GetProperty("style_count").GetInt32(), styles.Count);

    foreach (var expected in Data.GetProperty("style_raw_sha256").EnumerateObject())
      Assert.Equal(expected.Value.GetString(), Hash(VsfFile.Inflate(styles[expected.Name])));

    foreach (var form in Data.GetProperty("forms").EnumerateObject())
      Assert.Equal(form.Value.GetString(), Hash(ExeResources.ReadForm(exe, form.Name)));

    var state = ExeBuilder.ReadState(exe);

    Assert.Equal(Data.GetProperty("elements").GetBoolean(), state.Elements);
    Assert.Equal(Data.GetProperty("patched").GetBoolean(), state.Patched);
  }

  [Fact]
  public void Build_PatchSection_IsConsistentWithItsOwnAddress()
  {
    Assert.SkipUnless(CanRun, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box = new TestSandbox();

    var exe     = BuildReferenceExe(box, ThemeLibrary.Load(), ["nord"]);
    var pe      = new PeImage(File.ReadAllBytes(exe));
    var section = pe.Sections().Single(s => s.Name == ExeBuilder.PatchSection);
    var va      = pe.ImageBase + section.VirtualAddress;
    var (blob, pathEntry, okEntry) = LiveThemePatch.Build(va);

    // The section address depends on the size of the rewritten resources, so the code is checked against its own address.
    Assert.Equal(blob, pe.Bytes.AsSpan((int)section.RawPointer, blob.Length).ToArray());

    foreach (var site in LiveThemePatch.CallSites)
      Assert.Equal(LiveThemePatch.CallPatch(site, pathEntry), pe.Read(site, 5));

    Assert.Equal(LiveThemePatch.CallPatch(LiveThemePatch.OkCallSite, okEntry), pe.Read(LiveThemePatch.OkCallSite, 5));
  }

  [Fact]
  public void Build_Restore_ReturnsTheOriginalByteForByte()
  {
    Assert.SkipUnless(CanRun, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box = new TestSandbox();

    var exe = BuildReferenceExe(box, ThemeLibrary.Load(), ["nord"]);

    Assert.NotEqual(File.ReadAllBytes(GoldenData.Sm20OriginalPath!), File.ReadAllBytes(exe));

    ExeBuilder.Restore(exe, box.Path_("backup"));

    Assert.Equal(File.ReadAllBytes(GoldenData.Sm20OriginalPath!), File.ReadAllBytes(exe));
    Assert.False(ExeBuilder.HasChanges(exe));
  }

  [Fact]
  public void Build_RefusesAnExeWithChangesButNoMatchingOriginalBackup()
  {
    Assert.SkipUnless(CanRun, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");

    using var box = new TestSandbox();

    var exe = BuildReferenceExe(box, ThemeLibrary.Load(), ["nord"]);

    File.Delete(Path.Combine(box.Path_("backup"), "sm20.exe.original"));

    var ex = Assert.Throws<ThemeException>(() => ExeBuilder.Build(exe, box.Path_("backup"), new ExeState()));

    Assert.Contains("no matching original backup", ex.Message);
  }
}
