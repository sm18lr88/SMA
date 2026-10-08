// Checks the exe editor and live theme switching byte for byte against the recordings of the former Python tool.
using System.Text.Json;
using SuperMemoAssistant.Themes.Exe;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Exe;

public class PeAndLiveSwitchGoldenTests
{
  private static readonly JsonElement Pe   = GoldenData.Load("pe.json");
  private static readonly JsonElement LiveSwitch = GoldenData.Load("liveswitch.json");

  private static byte[] Hex(JsonElement e) => Convert.FromHexString(e.GetString()!);

  [Fact]
  public void PeImage_ReadsSectionsAndOffsets()
  {
    var pe     = new PeImage(Hex(Pe.GetProperty("image_hex")));
    var before = Pe.GetProperty("before");

    var expected = before.GetProperty("sections").EnumerateArray().Select(s => (s.GetProperty("name").GetString()!, s.GetProperty("vsize").GetUInt32(), s.GetProperty("va").GetUInt32(), s.GetProperty("rsize").GetUInt32(), s.GetProperty("rptr").GetUInt32()));

    Assert.Equal(expected, pe.Sections().Select(s => (s.Name, s.VirtualSize, s.VirtualAddress, s.RawSize, s.RawPointer)));
    Assert.Equal(before.GetProperty("next_va").GetUInt64(), pe.NextVa());
    Assert.Equal(before.GetProperty("file_offset_text").GetInt32(), pe.FileOffset(0x401010));
    Assert.Equal(before.GetProperty("read").GetString(), Convert.ToHexStringLower(pe.Read(0x401010, 12)));
  }

  [Fact]
  public void PeImage_AddSection_MatchesReferenceImage()
  {
    var pe      = new PeImage(Hex(Pe.GetProperty("image_hex")));
    var payload = Hex(Pe.GetProperty("payload_hex"));

    pe.Write(0x401010, [0xAA, 0xBB, 0xCC, 0xDD]);
    Assert.Equal(Pe.GetProperty("written").GetString(), Convert.ToHexStringLower(pe.Read(0x401008, 12)));

    var after = Pe.GetProperty("after");
    var va    = pe.AddSection(".smc", payload);

    Assert.Equal(after.GetProperty("va").GetUInt64(), va);
    Assert.Equal(after.GetProperty("next_va").GetUInt64(), pe.NextVa());
    Assert.Equal(after.GetProperty("image_hex").GetString(), Convert.ToHexStringLower(pe.Bytes));
    Assert.True(pe.HasSection(".smc"));
  }

  [Fact]
  public void PeImage_RejectsAddressesOutsideSections()
  {
    var pe = new PeImage(Hex(Pe.GetProperty("image_hex")));

    Assert.Contains("not inside a section", Assert.Throws<InvalidOperationException>(() => pe.FileOffset(0x500000)).Message);
  }

  [Fact]
  public void LiveThemePatch_Build_MatchesKeystoneAtEveryBase()
  {
    foreach (var c in LiveSwitch.GetProperty("blobs").EnumerateArray())
    {
      var (blob, pathEntry, okEntry) = LiveThemePatch.Build(c.GetProperty("base").GetUInt64());

      Assert.Equal(c.GetProperty("hex").GetString(), Convert.ToHexStringLower(blob));
      Assert.Equal(c.GetProperty("path").GetUInt64(), pathEntry);
      Assert.Equal(c.GetProperty("ok").GetUInt64(), okEntry);
    }
  }

  [Fact]
  public void LiveThemePatch_CallBytes_MatchReference()
  {
    foreach (var s in LiveSwitch.GetProperty("sites").EnumerateArray())
    {
      var site = s.GetProperty("site").GetUInt64();

      Assert.Equal(s.GetProperty("hex").GetString(), Convert.ToHexStringLower(LiveThemePatch.CallPatch(site, s.GetProperty("cave").GetUInt64())));

      if (site != LiveThemePatch.OkCallSite)
        Assert.Equal(s.GetProperty("original").GetString(), Convert.ToHexStringLower(LiveThemePatch.OriginalCall(site)));
    }

    Assert.Equal(LiveSwitch.GetProperty("ok_site_original").GetString(), Convert.ToHexStringLower(LiveThemePatch.OriginalCall(LiveThemePatch.OkCallSite, LiveThemePatch.OkCallOriginalTarget)));
  }
}
