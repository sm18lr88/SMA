// Checks the small pieces of card editing against the Python reference: diff opcodes, replacement templates, HTML coding, colors, slots.
using System.Text;
using System.Text.Json;
using SuperMemoAssistant.Themes.Editing;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Editing;

public class CardUnitGoldenTests
{
  private static readonly JsonElement Data = GoldenData.Load("cardunits.json");

  private static readonly Encoding Cp1252 = Ansi.ForCodePage(1252);

  private static string[] Strings(JsonElement array) => array.EnumerateArray().Select(x => x.GetString()!).ToArray();

  [Fact]
  public void Opcodes_MatchDifflib_IncludingTheAutojunkPathForLongInputs()
  {
    var cases = Data.GetProperty("difflib").EnumerateArray().ToList();

    Assert.Contains(cases, c => c.GetProperty("a").GetArrayLength() >= 200);

    foreach (var c in cases)
    {
      var expected = c.GetProperty("ops").EnumerateArray().Select(o => (o[0].GetString()!, o[1].GetInt32(), o[2].GetInt32(), o[3].GetInt32(), o[4].GetInt32())).ToList();
      var actual   = new DifflibMatcher(Strings(c.GetProperty("a")), Strings(c.GetProperty("b"))).Opcodes().Select(o => (o.Tag, o.A1, o.A2, o.B1, o.B2)).ToList();

      Assert.True(expected.SequenceEqual(actual), $"opcodes differ for a={c.GetProperty("a").GetArrayLength()} lines, b={c.GetProperty("b").GetArrayLength()} lines");
    }
  }

  [Fact]
  public void Replacer_MatchesPythonsReSub_ForTemplatesGroupsAndErrors()
  {
    foreach (var c in Data.GetProperty("replace").EnumerateArray())
    {
      var find = c.GetProperty("find").GetString()!;
      var repl = c.GetProperty("repl").GetString()!;
      var text = c.GetProperty("text").GetString()!;
      var what = $"/{find}/ -> {repl} on {text}";

      if (c.TryGetProperty("error", out _))
      {
        Assert.True(Throws(() => HtmlEdits.Replacer(find, repl, c.GetProperty("regex").GetBoolean(), c.GetProperty("ignore").GetBoolean())(text)), $"should fail: {what}");

        continue;
      }

      Assert.True(c.GetProperty("out").GetString() == HtmlEdits.Replacer(find, repl, c.GetProperty("regex").GetBoolean(), c.GetProperty("ignore").GetBoolean())(text), $"differs: {what}");
    }
  }

  private static bool Throws(Action action)
  {
    try
    {
      action();

      return false;
    }
    catch (ThemeException)
    {
      return true;
    }
  }

  [Fact]
  public void StyleStripper_MatchesReference()
  {
    foreach (var c in Data.GetProperty("strip").EnumerateArray())
      Assert.Equal(c.GetProperty("out").GetString(), HtmlEdits.StyleStripper(Strings(c.GetProperty("props")))(c.GetProperty("html").GetString()!));
  }

  [Fact]
  public void ReadingHtml_MatchesReference_AndKeepsTheEncodingItFound()
  {
    using var box = new TestSandbox();

    foreach (var (c, i) in Data.GetProperty("html").GetProperty("decode").EnumerateArray().Select((c, i) => (c, i)))
    {
      var path = box.Path_($"{i}.htm");

      File.WriteAllBytes(path, Convert.FromHexString(c.GetProperty("hex").GetString()!));

      var (text, encoding) = HtmlEdits.Read(path, Cp1252);

      Assert.Equal(c.GetProperty("text").GetString(), text);
      Assert.Equal(c.GetProperty("codec").GetString() == "utf-8" ? 65001 : 1252, encoding.CodePage);
    }
  }

  [Fact]
  public void WritingHtml_MatchesReference_UsingCharacterReferencesForWhatTheCodePageCannotHold()
  {
    foreach (var c in Data.GetProperty("html").GetProperty("encode").EnumerateArray())
    {
      var encoding = c.GetProperty("codec").GetString() == "utf-8" ? new UTF8Encoding(false, true) : Cp1252;

      Assert.Equal(c.GetProperty("hex").GetString(), Convert.ToHexStringLower(HtmlEdits.Write(c.GetProperty("text").GetString()!, encoding)));
    }
  }

  [Fact]
  public void ByteRanges_MatchReference()
  {
    foreach (var c in Data.GetProperty("ranges").EnumerateArray())
    {
      var expected = c.GetProperty("ranges").EnumerateArray().Select(r => (r[0].GetInt32(), r[1].GetInt32())).ToList();
      var actual   = CardBackups.Ranges(Convert.FromHexString(c.GetProperty("a").GetString()!), Convert.FromHexString(c.GetProperty("b").GetString()!));

      Assert.True(expected.SequenceEqual(actual), $"ranges differ for {c.GetProperty("a").GetString()}");
    }
  }

  [Fact]
  public void PreviewWindows_MatchReference()
  {
    foreach (var c in Data.GetProperty("windows").EnumerateArray())
    {
      var (before, after) = PreviewDiff.Window(c.GetProperty("a").GetString()!, c.GetProperty("b").GetString()!);

      Assert.Equal(c.GetProperty("out")[0].GetString(), before);
      Assert.Equal(c.GetProperty("out")[1].GetString(), after);
    }
  }

  [Fact]
  public void SplitLines_MatchesPythonsSplitlines()
  {
    foreach (var c in Data.GetProperty("splitlines").EnumerateArray())
      Assert.Equal(Strings(c.GetProperty("out")), PreviewDiff.SplitLines(c.GetProperty("text").GetString()!));
  }

  [Fact]
  public void SlotsMapToTheSameFilesAsTheReference()
  {
    foreach (var c in Data.GetProperty("slots").EnumerateArray())
      Assert.Equal(c.GetProperty("path").GetString(), ElementFiles.For("C:/nope/coll", c.GetProperty("slot").GetInt64()).Replace('\\', '/'));
  }

  [Fact]
  public void ColorsReadAndPrintLikeTheReference()
  {
    foreach (var c in Data.GetProperty("color_text").EnumerateArray())
      Assert.Equal(c.GetProperty("out").GetString(), CardColors.ToText(c.GetProperty("color").ValueKind == JsonValueKind.Null ? null : c.GetProperty("color").GetUInt32()));

    foreach (var c in Data.GetProperty("parse_color").EnumerateArray())
    {
      var text = c.GetProperty("text").GetString()!;

      if (c.TryGetProperty("error", out _))
        Assert.Throws<ThemeException>(() => CardColors.Parse(text));
      else
        Assert.Equal(c.GetProperty("out").GetUInt32(), CardColors.Parse(text));
    }
  }

  [Fact]
  public void ElementNumbers_ParseLikeTheReference()
  {
    foreach (var c in Data.GetProperty("parse_numbers").EnumerateArray())
    {
      var spec = c.GetProperty("spec").GetString()!;

      if (c.TryGetProperty("error", out _))
        Assert.Throws<ThemeException>(() => CardSelection.ParseNumbers(spec));
      else
        Assert.Equal(c.GetProperty("out").EnumerateArray().Select(x => x.GetInt32()), CardSelection.ParseNumbers(spec).Order());
    }
  }
}
