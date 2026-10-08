// Replays the scripted session of the Python smcards CLI with the C# command line and compares every step: output, exit code, files after.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace SuperMemoAssistant.Themes.Tests.Editing;

public sealed partial class CardSessionGoldenTests : IDisposable
{
  private static readonly JsonElement Data = GoldenData.Load("cardcli.json");

  private static readonly Encoding Cp1252 = Ansi.ForCodePage(1252);

  /// <summary>
  ///   The recording was made with every backup in the same second (names "-2", "-2-3"), and backup lists sort by name. A
  ///   real clock can cross a second between two steps and change that order, so the session runs at one fixed instant.
  /// </summary>
  private static readonly DateTime SessionClock = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Local);

  private readonly string _base = Path.Combine(Path.GetTempPath(), "sma-cardcli-" + Guid.NewGuid().ToString("N")[..8]);

  public void Dispose()
  {
    if (Directory.Exists(_base))
      Directory.Delete(_base, true);
  }

  private string Root => Path.Combine(_base, "SuperMemo");

  private string Collection => Path.Combine(Root, "systems", "coll");

  [GeneratedRegex(@"\d{8}-\d{6}")]
  private static partial Regex Timestamp();

  [GeneratedRegex(@"(?<=<ts>-[A-Za-z-]*[A-Za-z])(-\d+)+(?=\s|$)", RegexOptions.Multiline)]
  private static partial Regex CollisionSuffix();

  [GeneratedRegex(@"(== bin\\supermemo\.ini \()\d+ -> \d+( bytes\))")]
  private static partial Regex IniSizes();

  /// <summary>
  ///   Backup folder names get "-2", "-2-3" when two backups are made in the same second, which depends on timing. The size
  ///   of supermemo.ini depends on the length of the temp folder it names. The comparison ignores both.
  /// </summary>
  private string Normalize(string text) => NormalizeGolden(Timestamp().Replace(text.Replace(Root, "<root>"), "<ts>"));

  private static string NormalizeGolden(string text) => IniSizes().Replace(CollisionSuffix().Replace(text, ""), "$1<n> -> <n>$2");

  private void Materialize(JsonElement files)
  {
    foreach (var file in files.EnumerateObject())
    {
      var path = Path.Combine(Root, file.Name.Replace('/', '\\'));
      var data = Convert.FromHexString(file.Value.GetString()!);

      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllBytes(path, Encoding.ASCII.GetBytes(Encoding.ASCII.GetString(data).Replace("@ROOT@", Root)) is var replaced && data.AsSpan().IndexOf("@ROOT@"u8) >= 0 ? replaced : data);
    }
  }

  private List<string> BackupNames()
  {
    var folder = Path.Combine(Root, "smcards-backups", "coll");

    return Directory.Exists(folder)
      ? Directory.GetDirectories(folder).Where(d => File.Exists(Path.Combine(d, "manifest.json"))).Select(d => Path.GetFileName(d)!).Order(StringComparer.Ordinal).ToList()
      : [];
  }

  private string Expand(string arg)
  {
    arg = arg.Replace("{root}", Root).Replace("{coll}", Collection);

    var m = Regex.Match(arg, @"^\{backup:(.+?)#(\d+)\}$");

    return m.Success
      ? BackupNames().Where(b => Regex.IsMatch(b, $"-{Regex.Escape(m.Groups[1].Value)}(-\\d+)?$"))
                     .ElementAt(int.Parse(m.Groups[2].Value) - 1)
      : arg;
  }

  private Dictionary<string, string> Watched()
  {
    string Sha(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    var result = new Dictionary<string, string>();

    foreach (var rel in new[] { "info/compon.dat", "info/other.dat" })
      result[rel] = Sha(Path.Combine(Collection, rel.Replace('/', '\\')));

    foreach (var rel in new[] { "bin/supermemo.css", "bin/LightMode.css", "bin/DarkMode.css" })
      result[rel] = Sha(Path.Combine(Root, rel.Replace('/', '\\')));

    foreach (var file in Directory.EnumerateFiles(Path.Combine(Collection, "elements"), "*.HTM", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
      result[Path.GetRelativePath(Collection, file).Replace('\\', '/')] = Sha(file);

    return result;
  }

  private (string Out, string Err, int Code) Run(IReadOnlyList<string> args)
  {
    var output = new StringWriter { NewLine = "\n" };
    var error  = new StringWriter { NewLine = "\n" };
    var code   = CardCommandLine.Run(args, output, error, () => [], Cp1252, () => SessionClock);

    return (output.ToString(), error.ToString(), code);
  }

  [Fact]
  public void EveryStepOfTheSession_BehavesLikeThePythonCli()
  {
    var session = Data.GetProperty("session");

    Materialize(session.GetProperty("initial"));

    var failures = new List<string>();
    var index    = 0;

    foreach (var step in session.GetProperty("steps").EnumerateArray())
    {
      var args = step.GetProperty("args").EnumerateArray().Select(a => a.GetString()!).ToList();
      var argv = new List<string> { "--sm-root", Root };

      if (!args.Contains("--collection"))
        argv.AddRange(["--collection", "coll"]);

      argv.AddRange(args.Select(Expand));

      var (output, error, code) = Run(argv);
      var label                 = $"step {index++}: {string.Join(' ', args)}";

      if (NormalizeGolden(step.GetProperty("stdout").GetString()!) != Normalize(output))
        failures.Add($"{label}\n--- expected stdout\n{NormalizeGolden(step.GetProperty("stdout").GetString()!)}\n--- actual stdout\n{Normalize(output)}");

      if (step.GetProperty("code").GetInt32() != code)
        failures.Add($"{label}: exit code {code}, expected {step.GetProperty("code").GetInt32()} ({error})");

      if (step.GetProperty("compare_stderr").GetBoolean() && NormalizeGolden(step.GetProperty("stderr").GetString()!) != Normalize(error))
        failures.Add($"{label}\n--- expected stderr\n{step.GetProperty("stderr").GetString()}\n--- actual stderr\n{Normalize(error)}");

      var expectedFiles = step.GetProperty("files").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
      var actualFiles   = Watched();

      foreach (var (name, hash) in expectedFiles)
      {
        if (!actualFiles.TryGetValue(name, out var actual) || actual != hash)
          failures.Add($"{label}: file {name} differs after this step");
      }

      if (failures.Count > 0)
        break;
    }

    Assert.True(failures.Count == 0, string.Join("\n\n", failures));
  }

  [Fact]
  public void SchedulingDataIsNeverTouched_ByAnyStepOfTheSession()
  {
    var session = Data.GetProperty("session");

    Materialize(session.GetProperty("initial"));

    var before = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(Collection, "info", "other.dat"))));

    foreach (var step in session.GetProperty("steps").EnumerateArray())
    {
      var args = step.GetProperty("args").EnumerateArray().Select(a => a.GetString()!).ToList();
      var argv = new List<string> { "--sm-root", Root };

      if (!args.Contains("--collection"))
        argv.AddRange(["--collection", "coll"]);

      argv.AddRange(args.Select(Expand));
      Run(argv);
    }

    Assert.Equal(before, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(Collection, "info", "other.dat")))));
  }

  [Fact]
  public void WhileSuperMemoRuns_ApplyIsRefused_AndNothingChanges()
  {
    Materialize(Data.GetProperty("session").GetProperty("initial"));

    var before = Watched();
    var output = new StringWriter { NewLine = "\n" };
    var error  = new StringWriter { NewLine = "\n" };
    var code   = CardCommandLine.Run(["--sm-root", Root, "--collection", "coll", "element-color", "#112233", "--apply"], output, error, () => ["sm20.exe"], Cp1252);

    Assert.Equal(1, code);
    Assert.Contains("SuperMemo is running (sm20.exe). Close it first", error.ToString());
    Assert.Equal(before, Watched());
    Assert.False(Directory.Exists(Path.Combine(Root, "smcards-backups")));
  }

  [Fact]
  public void ADryRun_WorksWhileSuperMemoRuns()
  {
    Materialize(Data.GetProperty("session").GetProperty("initial"));

    var output = new StringWriter { NewLine = "\n" };
    var code   = CardCommandLine.Run(["--sm-root", Root, "--collection", "coll", "element-color", "#112233"], output, new StringWriter(), () => ["sm20.exe"], Cp1252);

    Assert.Equal(0, code);
    Assert.Contains("DRY RUN", output.ToString());
  }

  [Fact]
  public void Help_ListsTheCommands_AndNoCommandIsAUsageError()
  {
    var output = new StringWriter();
    var error  = new StringWriter();

    Assert.Equal(0, CardCommandLine.Run(["--help"], output, error));
    Assert.StartsWith("Usage: sma-cards", output.ToString());

    foreach (var command in new[] { "status", "collections", "list", "show", "element-color", "css set", "css unset", "html replace", "html strip-style", "backups", "restore", "snapshot", "diff" })
      Assert.Contains(command, output.ToString());

    var none = new StringWriter();

    Assert.Equal(2, CardCommandLine.Run([], new StringWriter(), none));
    Assert.StartsWith("Usage:", none.ToString());
  }

  [Theory]
  [InlineData("nonsense")]
  [InlineData("css")]
  [InlineData("show")]
  [InlineData("list --kind banana")]
  [InlineData("list --bogus")]
  [InlineData("css set BODY color")]
  public void ABadCommandLine_ExitsWithTwo_AndSaysWhy(string line)
  {
    Materialize(Data.GetProperty("session").GetProperty("initial"));

    var error = new StringWriter();
    var code  = CardCommandLine.Run(["--sm-root", Root, ..line.Split(' ')], new StringWriter(), error, () => [], Cp1252);

    Assert.Equal(2, code);
    Assert.StartsWith("smcards:", error.ToString());
  }
}
