// Checks VCL style loading, writing and palette recoloring against the Python reference, and against the real built-in styles when sm20.exe is present.
using System.Security.Cryptography;
using System.Text.Json;
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Styles;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Styles;

public class VsfGoldenTests
{
  private static readonly JsonElement Data = GoldenData.Load("vsf.json");

  private static byte[] Blob => Convert.FromHexString(Data.GetProperty("synthetic_blob_hex").GetString()!);

  private static Dictionary<string, string> Roles(JsonElement e) => e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);

  [Fact]
  public void Load_ThenDumpRaw_RoundTripsTheReferenceBytes()
  {
    Assert.Equal(Data.GetProperty("synthetic_raw_hex").GetString(), Convert.ToHexStringLower(VsfFile.DumpRaw(VsfFile.Load(Blob))));
  }

  [Fact]
  public void StyleName_ReadsTheNameWithoutLoadingTheStyle()
  {
    Assert.Equal(Data.GetProperty("synthetic_name").GetString(), VsfFile.StyleName(Blob));
  }

  [Fact]
  public void Load_KeepsUnparsedBytesAsRawRuns()
  {
    var vsf = VsfFile.Load(Blob);

    Assert.Contains(vsf.Tail, t => !t.IsPair && t.Raw!.SequenceEqual(new byte[] { 0xFF, 0xFE, 0x01 }));
    Assert.Equal(2, vsf.Bitmaps.Count);
    Assert.Equal(["TSomething", "TOther"], vsf.Objects.Select(o => o.Class));
  }

  [Fact]
  public void Dump_ProducesAStreamThatInflatesToTheSameData()
  {
    var vsf  = VsfFile.Load(Blob);
    var dump = VsfFile.Dump(vsf);

    Assert.True(VsfFile.HasMagic(dump));
    Assert.True(VsfFile.SameContent(dump, Blob), "compressors differ, so the inflated data is what must match");
    Assert.Equal(VsfFile.Inflate(Blob), VsfFile.Inflate(dump));
  }

  [Fact]
  public void Load_RejectsDataWithoutTheMagicHeader()
  {
    Assert.Throws<InvalidDataException>(() => VsfFile.Load([1, 2, 3, 4]));
  }

  [Fact]
  public void Recolor_MatchesReferenceForDarkAndLightPalettes()
  {
    var cases = Data.GetProperty("cases").EnumerateArray().ToList();

    Assert.Contains(cases, c => c.GetProperty("variant").GetString() == "dark");
    Assert.Contains(cases, c => c.GetProperty("variant").GetString() == "light");

    foreach (var c in cases)
    {
      var recolored = VsfRecolor.Recolor(VsfFile.Load(Blob), Roles(c.GetProperty("roles")), c.GetProperty("name").GetString()!, c.GetProperty("variant").GetString()!);

      Assert.True(c.GetProperty("raw_hex").GetString() == Convert.ToHexStringLower(VsfFile.DumpRaw(recolored)), $"recolor differs for {c.GetProperty("id").GetString()}");
    }
  }

  [Fact]
  public void Recolor_NamesTheStyleAndKeepsTheBaseObjects()
  {
    var c         = Data.GetProperty("cases")[0];
    var baseStyle = VsfFile.Load(Blob);
    var recolored = VsfRecolor.Recolor(baseStyle, Roles(c.GetProperty("roles")), "My Theme", c.GetProperty("variant").GetString()!);

    Assert.Equal("My Theme", recolored.Name);
    Assert.Equal(baseStyle.Objects.Select(o => o.Data.Length), recolored.Objects.Select(o => o.Data.Length));
    Assert.Equal("My Theme", VsfFile.StyleName(VsfFile.Dump(recolored)));
  }

  [Fact]
  public void RealStyles_MatchTheReference_WhenTheOriginalExeIsAvailable()
  {
    var real = Data.GetProperty("real");

    Assert.SkipWhen(GoldenData.Sm20OriginalPath is null || real.ValueKind == JsonValueKind.Null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");
    Assert.SkipUnless(ExeBuilder.ProgramHash(File.ReadAllBytes(GoldenData.Sm20OriginalPath!)) == real.GetProperty("program_hash").GetString(), "the exe is another build than the one the golden hashes came from.");

    var styles = ExeResources.ReadStyles(GoldenData.Sm20OriginalPath!);

    Assert.Equal(real.GetProperty("style_count").GetInt32(), styles.Count);

    foreach (var name in real.GetProperty("names").EnumerateObject())
      Assert.Equal(name.Value.GetString(), VsfFile.StyleName(styles[name.Name]));

    foreach (var expected in real.GetProperty("recolored").EnumerateArray())
    {
      var entry    = Data.GetProperty("cases").EnumerateArray().First(c => c.GetProperty("id").GetString() == expected.GetProperty("id").GetString());
      var variant  = entry.GetProperty("variant").GetString()!;
      var baseName = variant == "dark" ? "WINDOWSDARK" : "WINDOWS";
      var raw      = VsfFile.DumpRaw(VsfRecolor.Recolor(VsfFile.Load(styles[baseName]), Roles(entry.GetProperty("roles")), entry.GetProperty("name").GetString()!, variant));

      Assert.Equal(expected.GetProperty("raw_sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(raw)));
    }
  }
}
