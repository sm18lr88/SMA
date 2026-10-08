// Checks the Delphi form reader and the StyleElements editor against the Python reference, and against the real forms when sm20.exe is present.
using System.Security.Cryptography;
using System.Text.Json;
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Forms;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Forms;

public class DfmGoldenTests
{
  private static readonly JsonElement Data = GoldenData.Load("dfm.json");

  private static byte[] Form => Convert.FromHexString(Data.GetProperty("form_hex").GetString()!);

  private static List<(int Depth, string Class, string Name, string[]? Elements)> Flatten(DfmObject node, int depth = 0)
  {
    var rows = new List<(int, string, string, string[]?)> { (depth, node.Class, node.Name, DfmForm.StyleElementsOf(node)?.Order().ToArray()) };

    foreach (var kid in node.Kids)
      rows.AddRange(Flatten(kid, depth + 1));

    return rows;
  }

  [Fact]
  public void Parse_ReadsTheSameTreeAsTheReference()
  {
    var expected = Data.GetProperty("tree").EnumerateArray().Select(r => (
      r[0].GetInt32(), r[1].GetString()!, r[2].GetString()!,
      r[3].ValueKind == JsonValueKind.Null ? null : r[3].EnumerateArray().Select(x => x.GetString()!).ToArray())).ToList();

    var actual = Flatten(DfmForm.Parse(Form));

    Assert.Equal(expected.Select(e => (e.Item1, e.Item2, e.Item3)), actual.Select(a => (a.Depth, a.Class, a.Name)));
    Assert.Equal(expected.Select(e => e.Item4), actual.Select(a => a.Elements));
  }

  [Fact]
  public void SetStyleElements_MatchesReferenceBytes()
  {
    foreach (var edit in Data.GetProperty("edits").EnumerateArray())
    {
      var themed = edit.GetProperty("themed").EnumerateArray().Select(x => x.GetString()!).ToArray();
      var wanted = edit.GetProperty("names").EnumerateArray().ToDictionary(n => n.GetString()!, _ => themed);

      Assert.Equal(edit.GetProperty("out_hex").GetString(), Convert.ToHexStringLower(DfmForm.SetStyleElements(Form, wanted)));
    }
  }

  [Fact]
  public void SetStyleElements_InsertsTheProperty_WhenAbsent_AndReplacesItWhenPresent()
  {
    var themed = new[] { "seFont", "seClient", "seBorder" };
    var edited = DfmForm.Parse(DfmForm.SetStyleElements(Form, new Dictionary<string, string[]> { ["Plain"] = themed, ["Box"] = themed }));

    Assert.Equal(themed.Order(), DfmForm.StyleElementsOf(edited.Find("Plain")!)!.Order());
    Assert.Equal(themed.Order(), DfmForm.StyleElementsOf(edited.Find("Box")!)!.Order());
    Assert.Equal(["seFont"], DfmForm.StyleElementsOf(edited.Find("Lbl")!)!);
  }

  [Fact]
  public void SetStyleElements_NamesTheMissingObject()
  {
    var ex = Assert.Throws<KeyNotFoundException>(() => DfmForm.SetStyleElements(Form, new Dictionary<string, string[]> { ["Nope"] = ["seFont"] }));

    Assert.Contains("Nope", ex.Message);
  }

  [Fact]
  public void Parse_RejectsDataThatIsNotABinaryForm()
  {
    Assert.Throws<InvalidDataException>(() => DfmForm.Parse("object Form1: TForm"u8.ToArray()));
  }

  [Fact]
  public void RealForms_MatchTheReference_WhenTheOriginalExeIsAvailable()
  {
    var real = Data.GetProperty("real");

    Assert.SkipWhen(GoldenData.Sm20OriginalPath is null || real.ValueKind == JsonValueKind.Null, "the untouched sm20.exe is not available (set SMA_SM20_ORIGINAL).");
    Assert.SkipUnless(ExeBuilder.ProgramHash(File.ReadAllBytes(GoldenData.Sm20OriginalPath!)) == real.GetProperty("program_hash").GetString(), "the exe is another build than the one the golden hashes came from.");

    Assert.Equal(real.GetProperty("objects").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(x => x.GetString()!).ToArray()).OrderBy(k => k.Key).Select(k => (k.Key, k.Value)),
                 ExeBuilder.ElementObjects.OrderBy(k => k.Key).Select(k => (k.Key, k.Value)));

    foreach (var (formName, names) in ExeBuilder.ElementObjects)
    {
      var edited = DfmForm.SetStyleElements(ExeResources.ReadForm(GoldenData.Sm20OriginalPath!, formName), names.ToDictionary(n => n, _ => ExeBuilder.Themed));

      Assert.Equal(real.GetProperty("forms").GetProperty(formName).GetString(), Convert.ToHexStringLower(SHA256.HashData(edited)));
    }
  }
}
