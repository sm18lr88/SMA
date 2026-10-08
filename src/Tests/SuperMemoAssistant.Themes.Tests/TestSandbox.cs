// A temporary folder that is removed after the test, with helpers that lay out a fake SuperMemo install.
using System.Text;

namespace SuperMemoAssistant.Themes.Tests;

internal sealed class TestSandbox : IDisposable
{
  public TestSandbox()
  {
    Root = Path.Combine(Path.GetTempPath(), "sma-themes-test-" + Guid.NewGuid().ToString("N")[..10]);
    Directory.CreateDirectory(Root);
  }

  public string Root { get; }

  public string Path_(string relative) => Path.Combine(Root, relative);

  public void Dispose() => Directory.Delete(Root, true);

  public void Write(string relative, string text, Encoding? encoding = null)
  {
    var path = Path_(relative);

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllBytes(path, (encoding ?? Encoding.ASCII).GetBytes(text));
  }

  public string Read(string relative) => Encoding.ASCII.GetString(File.ReadAllBytes(Path_(relative)));

  /// <summary>An install whose files are text only: bin\*.css, bin\supermemo.ini, and a collection folder with collection.ini. No exe.</summary>
  public SuperMemoInstall TextOnlyInstall(Dictionary<string, string> files)
  {
    foreach (var (relative, text) in files)
      Write(relative, text);

    return new SuperMemoInstall(Path_("sm20.exe"), Path_(@"systems\coll"));
  }

  /// <summary>A fake install around a copy of the real original exe, with the collection files the engine reads and writes.</summary>
  public SuperMemoInstall InstallWithExe(string originalExe, Dictionary<string, string>? extraFiles = null)
  {
    File.Copy(originalExe, Path_("sm20.exe"));

    var files = new Dictionary<string, string>
    {
      [@"bin\DarkMode.css"]            = "BODY {color: #ddd; background-color: #111}\r\n",
      [@"bin\LightMode.css"]           = "BODY {color: #222}\r\n",
      [@"bin\supermemo.css"]           = "BODY {color: #ddd}\r\n",
      [@"bin\supermemo.ini"]           = "[SuperMemo]\r\nDark mode=0\r\n",
      [@"systems\coll\collection.ini"] = "[Defaults]\r\nTheme=Windows\r\n",
    };

    foreach (var (k, v) in extraFiles ?? [])
      files[k] = v;

    foreach (var (relative, text) in files)
      Write(relative, text);

    return new SuperMemoInstall(Path_("sm20.exe"), Path_(@"systems\coll"));
  }
}
