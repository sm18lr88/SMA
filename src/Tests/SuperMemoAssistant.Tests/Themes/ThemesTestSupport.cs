// Test support for the Themes plugin: an in-memory settings store, and a sandbox SuperMemo install around a copy of the original exe.
namespace SuperMemoAssistant.Tests.Themes;

using System.Text;
using Newtonsoft.Json;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.Plugins.Themes;
using global::SuperMemoAssistant.Themes;

/// <summary>Keeps the settings as JSON, like the real configuration service, so a setting that does not serialize fails here.</summary>
internal sealed class InMemoryStore : ISettingsStore
{
  private string _json = JsonConvert.SerializeObject(new ThemesCfg());

  public int Saves { get; private set; }

  public ThemesCfg Load() => JsonConvert.DeserializeObject<ThemesCfg>(_json)!;

  public void Save(ThemesCfg config)
  {
    Saves++;
    _json = JsonConvert.SerializeObject(config);
  }

  public void Update(Action<ThemesCfg> change)
  {
    var config = Load();

    change(config);
    _json = JsonConvert.SerializeObject(config);
  }
}

internal static class OriginalExe
{
  public static string? Path => SuperMemoLocation.OriginalExe;
}

internal sealed class ThemeSandbox : IDisposable
{
  public ThemeSandbox(bool withExe)
  {
    Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sma-themes-plugin-" + Guid.NewGuid().ToString("N")[..10]);
    Directory.CreateDirectory(Root);

    if (withExe)
      File.Copy(OriginalExe.Path!, ExePath);

    Write(@"bin\DarkMode.css", "BODY {color: #ddd; background-color: #111}\r\n");
    Write(@"bin\LightMode.css", "BODY {color: #222}\r\n");
    Write(@"bin\supermemo.css", "BODY {color: #ddd}\r\n");
    Write(@"bin\supermemo.ini", "[SuperMemo]\r\nDark mode=0\r\n");
    Write(@"systems\coll\collection.ini", "[Defaults]\r\nTheme=Windows\r\n");
  }

  public string Root { get; }

  public string ExePath => System.IO.Path.Combine(Root, "sm20.exe");

  public SMLaunchInfo Info => new(ExePath, System.IO.Path.Combine(Root, @"systems\coll"));

  public string Read(string relative) => Encoding.ASCII.GetString(File.ReadAllBytes(System.IO.Path.Combine(Root, relative)));

  public void Write(string relative, string text)
  {
    var path = System.IO.Path.Combine(Root, relative);

    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
    File.WriteAllBytes(path, Encoding.ASCII.GetBytes(text));
  }

  public void Dispose() => Directory.Delete(Root, true);
}
