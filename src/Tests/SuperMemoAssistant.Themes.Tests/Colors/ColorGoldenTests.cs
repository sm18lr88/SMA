// Checks the C# color math and CSS color parser against values produced by the Python reference implementation.
using System.Text.Json;
using SuperMemoAssistant.Themes.Colors;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Colors;

public class ColorGoldenTests
{
  private static readonly JsonElement Data = GoldenData.Load("colors.json");

  private static Rgb ToRgb(JsonElement e) => new(e[0].GetInt32(), e[1].GetInt32(), e[2].GetInt32());

  private static IEnumerable<JsonElement> Cases(string name) => Data.GetProperty(name).EnumerateArray();

  [Fact]
  public void ParseCssColor_MatchesReference()
  {
    foreach (var c in Cases("parse"))
    {
      var input  = c.GetProperty("in").GetString()!;
      var result = CssColor.Parse(input);

      if (c.GetProperty("out").ValueKind == JsonValueKind.Null)
      {
        Assert.True(result is null, $"'{input}' should not parse, got {result}");
        continue;
      }

      var expected = c.GetProperty("out");

      Assert.True(result is not null, $"'{input}' should parse");
      Assert.Equal((expected[0].GetInt32(), expected[1].GetInt32(), expected[2].GetInt32()), (result!.Value.R, result.Value.G, result.Value.B));
      Assert.Equal(expected[3].GetDouble(), result.Value.A, 12);
    }
  }

  [Fact]
  public void Mix_MatchesReference()
  {
    foreach (var c in Cases("mix"))
      Assert.Equal(ToRgb(c.GetProperty("out")), ColorMath.Mix(ToRgb(c.GetProperty("a")), ToRgb(c.GetProperty("b")), c.GetProperty("t").GetDouble()));
  }

  [Fact]
  public void Luminance_And_Contrast_MatchReference()
  {
    foreach (var c in Cases("luminance"))
      Assert.Equal(c.GetProperty("lum").GetDouble(), ColorMath.Luminance(ToRgb(c.GetProperty("c"))), 12);

    foreach (var c in Cases("contrast"))
      Assert.Equal(c.GetProperty("out").GetDouble(), ColorMath.Contrast(ToRgb(c.GetProperty("a")), ToRgb(c.GetProperty("b"))), 12);
  }

  [Fact]
  public void BestText_MatchesReference()
  {
    foreach (var c in Cases("best_text"))
    {
      var candidates = c.GetProperty("candidates").EnumerateArray().Select(ToRgb).ToArray();

      Assert.Equal(ToRgb(c.GetProperty("out")), ColorMath.BestText(ToRgb(c.GetProperty("bg")), candidates));
    }
  }

  [Fact]
  public void Hls_RoundTripsLikeReference()
  {
    foreach (var c in Cases("to_hls"))
    {
      var (h, l, s) = ColorMath.ToHls(ToRgb(c.GetProperty("c")));
      var expected  = GoldenData.Doubles(c.GetProperty("hls"));

      Assert.Equal(expected[0], h, 12);
      Assert.Equal(expected[1], l, 12);
      Assert.Equal(expected[2], s, 12);
    }

    foreach (var c in Cases("from_hls"))
      Assert.Equal(ToRgb(c.GetProperty("out")), ColorMath.FromHls(c.GetProperty("h").GetDouble(), c.GetProperty("l").GetDouble(), c.GetProperty("s").GetDouble()));
  }

  [Fact]
  public void HueDistance_MatchesReference()
  {
    foreach (var c in Cases("hue_distance"))
      Assert.Equal(c.GetProperty("out").GetDouble(), ColorMath.HueDistance(c.GetProperty("a").GetDouble(), c.GetProperty("b").GetDouble()), 12);
  }

  [Fact]
  public void HexConversions_MatchReference()
  {
    foreach (var c in Cases("hex_to_rgb"))
      Assert.Equal(ToRgb(c.GetProperty("out")), ColorMath.HexToRgb(c.GetProperty("in").GetString()!));

    foreach (var c in Cases("rgb_to_hex"))
      Assert.Equal(c.GetProperty("out").GetString(), ColorMath.RgbToHex(ToRgb(c.GetProperty("c"))));
  }

  [Fact]
  public void Over_MatchesReference()
  {
    foreach (var c in Cases("over"))
    {
      var v = c.GetProperty("c");

      Assert.Equal(ToRgb(c.GetProperty("out")), ColorMath.Over(new Rgba(v[0].GetInt32(), v[1].GetInt32(), v[2].GetInt32(), v[3].GetDouble()), ToRgb(c.GetProperty("bg"))));
    }
  }
}
