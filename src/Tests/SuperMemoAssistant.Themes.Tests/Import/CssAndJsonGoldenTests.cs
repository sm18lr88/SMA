// Checks CSS scanning, var() resolution and comment-tolerant JSON against the reference, including the data: URI case that broke the CSS cache once.
using System.Text.Json;
using SuperMemoAssistant.Themes.Import;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Import;

public class CssAndJsonGoldenTests
{
  private static readonly JsonElement Css = ImportFixtures.Data.GetProperty("css");

  [Fact]
  public void Rules_MatchReference_IncludingQuotedSemicolonsAndNestedAtRules()
  {
    foreach (var c in Css.GetProperty("rules").EnumerateArray())
    {
      var expected = c.GetProperty("out").EnumerateArray().Select(r => (r[0].GetString()!, r[1].GetString()!)).ToList();
      var actual   = CssSheet.Rules(c.GetProperty("css").GetString()!);

      Assert.True(expected.SequenceEqual(actual), $"rules differ for: {c.GetProperty("css").GetString()}");
    }
  }

  [Fact]
  public void CustomProperties_MatchReference()
  {
    foreach (var c in Css.GetProperty("custom_properties").EnumerateArray())
    {
      var expected = c.GetProperty("out").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
      var actual   = CssSheet.CustomProperties(c.GetProperty("body").GetString()!);

      Assert.True(expected.OrderBy(k => k.Key).SequenceEqual(actual.OrderBy(k => k.Key)), $"properties differ for: {c.GetProperty("body").GetString()}");
    }
  }

  [Fact]
  public void ADataUriWithSemicolonsAndBraces_StaysOneValue()
  {
    var rules = CssSheet.Rules(".d { --icon: url(\"data:image/svg+xml;charset=UTF-8,<svg a='b'>{x}</svg>\"); --b: 1px solid Canvas; }");
    var props = CssSheet.CustomProperties(rules.Single().Body);

    Assert.Equal(2, props.Count);
    Assert.StartsWith("url(\"data:image/svg+xml;charset=UTF-8,", props["--icon"]);
    Assert.Equal("1px solid Canvas", props["--b"]);
  }

  [Fact]
  public void BodyClasses_MatchReference()
  {
    foreach (var c in Css.GetProperty("body_classes").EnumerateArray())
    {
      var actual = CssSheet.BodyClasses(c.GetProperty("part").GetString()!);

      if (c.GetProperty("out").ValueKind == JsonValueKind.Null)
        Assert.True(actual is null, $"'{c.GetProperty("part").GetString()}' should not be a body selector");
      else
        Assert.Equal(c.GetProperty("out").EnumerateArray().Select(x => x.GetString()!).Order(), actual!.Order());
    }
  }

  [Fact]
  public void SplitTop_MatchesReference()
  {
    foreach (var c in Css.GetProperty("split_top").EnumerateArray())
      Assert.Equal(c.GetProperty("out").EnumerateArray().Select(x => x.GetString()), CssSheet.SplitTop(c.GetProperty("text").GetString()!));
  }

  [Fact]
  public void Resolve_MatchesReference_AndReturnsAtOnceForACycle()
  {
    var env = ImportFixtures.Roles(Css.GetProperty("resolve_env"));

    foreach (var c in Css.GetProperty("resolve").EnumerateArray())
      Assert.Equal(c.GetProperty("out").GetString(), ObsidianPalette.Resolve(c.GetProperty("value").GetString()!, env));

    Assert.Equal("", ObsidianPalette.Resolve("var(--loop)", env));
  }

  [Fact]
  public void Resolve_TwoVariablesThatPointAtEachOther_AreUnsetAndDoNotHang()
  {
    var env = new Dictionary<string, string> { ["--a"] = "var(--b)", ["--b"] = "var(--a, #fff)" };

    Assert.Equal(" #fff", ObsidianPalette.Resolve("var(--a)", env)); // a fallback keeps its leading space; color parsing trims it
  }

  [Fact]
  public void Jsonc_MatchesReference_ForCommentsTrailingCommasAndBadInput()
  {
    foreach (var c in Css.GetProperty("jsonc").EnumerateArray())
    {
      var text = c.GetProperty("text").GetString()!;

      if (c.TryGetProperty("error", out _))
      {
        Assert.ThrowsAny<JsonException>(() => Jsonc.Parse(text));

        continue;
      }

      Assert.Equal(Normalize(c.GetProperty("out")), Normalize(Jsonc.Parse(text)));
    }
  }

  private static string Normalize(JsonElement e) => JsonSerializer.Serialize(e);
}
