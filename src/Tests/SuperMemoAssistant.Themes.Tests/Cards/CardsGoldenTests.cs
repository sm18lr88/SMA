// Checks CSS rule editing, card rules, ini editing and the settings and card plans against the Python reference.
using System.Text;
using System.Text.Json;
using SuperMemoAssistant.Themes.Cards;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Cards;

public class CardsGoldenTests
{
  private static readonly JsonElement Data = GoldenData.Load("cards.json");

  private static Dictionary<string, string> Roles(JsonElement e) => e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);

  [Fact]
  public void CssSet_MatchesReference_ForEverySampleAndEdit()
  {
    foreach (var c in Data.GetProperty("css_set").EnumerateArray())
    {
      var value = c.GetProperty("value").ValueKind == JsonValueKind.Null ? null : c.GetProperty("value").GetString();
      var css   = c.GetProperty("css").GetString()!;
      var actual = CardCss.Set(css, c.GetProperty("selector").GetString()!, c.GetProperty("prop").GetString()!, value);

      Assert.True(c.GetProperty("out").GetString() == actual, $"css '{css}' {c.GetProperty("selector")} {c.GetProperty("prop")}\nexpected: {c.GetProperty("out").GetString()}\nactual:   {actual}");
    }
  }

  [Fact]
  public void IniSet_MatchesReference()
  {
    foreach (var c in Data.GetProperty("ini_set").EnumerateArray())
    {
      var actual = IniText.Set(c.GetProperty("text").GetString()!, c.GetProperty("section").GetString()!, c.GetProperty("key").GetString()!, c.GetProperty("value").GetString()!);

      Assert.True(c.GetProperty("out").GetString() == actual, $"ini '{c.GetProperty("text")}' [{c.GetProperty("section")}] {c.GetProperty("key")}");
    }
  }

  [Fact]
  public void CardRules_MatchReference_ForThirtyPalettes()
  {
    foreach (var c in Data.GetProperty("card_rules").EnumerateArray())
    {
      var expected = c.GetProperty("rules").EnumerateArray().Select(r => (r[0].GetString()!, r[1].GetString()!, r[2].GetString()!)).ToList();
      var actual   = CardRules.For(Roles(c.GetProperty("roles"))).Select(r => (r.Selector, r.Property, r.Value)).ToList();

      Assert.True(expected.SequenceEqual(actual), $"card rules differ for {c.GetProperty("id")}");
    }
  }

  [Fact]
  public void ApplyCardRules_MatchesReference()
  {
    var library = ThemeLibrary.Load();

    foreach (var c in Data.GetProperty("apply").EnumerateArray())
    {
      var entry = library.TryGet(c.GetProperty("id").GetString()!)!;

      Assert.Equal(c.GetProperty("out").GetString(), CardRules.Apply(c.GetProperty("css").GetString()!, CardRules.For(entry.Roles)));
    }
  }

  [Fact]
  public void IniText_ReadsKeysTheWayConfigparserDoes()
  {
    var ini = new IniText("; comment\r\n[Defaults]\r\nTheme = Light Style \r\nDark Theme: Dark: Style\r\n\r\n[SuperMemo]\r\ndark mode=1\r\n");

    Assert.Equal("Light Style", ini.Get("Defaults", "theme"));
    Assert.Equal("Dark: Style", ini.Get("Defaults", "Dark Theme"));
    Assert.Equal("1", ini.Get("SuperMemo", "Dark Mode", "0"));
    Assert.Equal("fallback", ini.Get("Missing", "x", "fallback"));
  }

  [Fact]
  public void Plans_MatchReference_OnASandboxInstall()
  {
    var plans = Data.GetProperty("plans");

    using var box = new TestSandbox();

    var install = box.TextOnlyInstall(plans.GetProperty("files").EnumerateObject().ToDictionary(p => p.Name.Replace('/', '\\'), p => p.Value.GetString()!));
    var entry   = plans.GetProperty("entry");
    var variant = entry.GetProperty("variant").GetString()!;
    var roles   = Roles(entry.GetProperty("roles"));

    Dictionary<string, string> Relative(Dictionary<string, byte[]> changes) =>
      changes.ToDictionary(kv => Path.GetRelativePath(box.Root, kv.Key).Replace('\\', '/'), kv => Encoding.ASCII.GetString(kv.Value));

    void AssertPlan(string name, Dictionary<string, byte[]> actual) =>
      Assert.True(plans.GetProperty(name).EnumerateObject().OrderBy(p => p.Name).Select(p => (p.Name, p.Value.GetString())).SequenceEqual(Relative(actual).OrderBy(k => k.Key).Select(k => (k.Key, (string?)k.Value))), $"plan {name} differs");

    AssertPlan("cards_live", CardSync.PlanCards(install, variant, roles, true));
    AssertPlan("cards_not_live", CardSync.PlanCards(install, variant, roles, false));
    AssertPlan("settings", CardSync.PlanSettings(install, "My Style", variant));
    AssertPlan("settings_other", CardSync.PlanSettings(install, "Other", variant == "dark" ? "light" : "dark"));

    var active = CardSync.ActiveStyles(install);

    Assert.Equal(plans.GetProperty("active").GetProperty("light").GetString(), active.Light);
    Assert.Equal(plans.GetProperty("active").GetProperty("dark").GetString(), active.Dark);
    Assert.Equal(plans.GetProperty("active").GetProperty("mode").GetString() == "dark", active.DarkMode);
    Assert.Equal(plans.GetProperty("stylesheet_dark").GetString(), CardRules.Apply(box.Read(@"bin\DarkMode.css"), CardRules.For(roles)));
    Assert.Equal(plans.GetProperty("stylesheet_light").GetString(), CardRules.Apply(box.Read(@"bin\LightMode.css"), CardRules.For(roles)));
  }

  [Fact]
  public void PlanSettings_ExplainsAMissingIniFile()
  {
    using var box = new TestSandbox();

    var install = new SuperMemoInstall(box.Path_("sm20.exe"), box.Path_(@"systems\coll"));

    Assert.Contains("was not found", Assert.Throws<ThemeException>(() => CardSync.PlanSettings(install, "x", "dark")).Message);
  }
}
