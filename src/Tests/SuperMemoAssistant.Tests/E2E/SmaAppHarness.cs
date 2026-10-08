// Runs the built SuperMemoAssistant.exe on a hidden desktop with a throwaway profile, seeded config and development plugins.
namespace SuperMemoAssistant.Tests.E2E;

using System.Text.Json;
using SuperMemoAssistant.SuperMemo.Hooks;

internal sealed class SmaAppHarness : IDisposable
{
  private readonly HiddenDesktop               _desktop;
  private readonly Dictionary<string, string> _environment;

  public IReadOnlyDictionary<string, string> Environment => _environment;
  private NativeProcess.StartedProcess        _process;

  /// <param name="smExePath">The SuperMemo executable, or null for a first run: no configuration, so SMA opens its setup wizard.</param>
  public SmaAppHarness(HiddenDesktop desktop, string? smExePath)
  {
    _desktop = desktop;
    Profile  = Path.Combine(Path.GetTempPath(), $"sma-e2e-profile-{Guid.NewGuid():N}");
    _environment = new Dictionary<string, string>
    {
      ["SMA_APPDATA_DIR"] = Profile,
      ["USERPROFILE"]     = Profile,
      ["LOCALAPPDATA"]    = Path.Combine(Profile, "AppData", "Local"),
      ["APPDATA"]         = Path.Combine(Profile, "AppData", "Roaming"),
      ["TEMP"]            = Path.Combine(Profile, "Temp"),
      ["TMP"]             = Path.Combine(Profile, "Temp"),
    };
    Directory.CreateDirectory(_environment["TEMP"]);

    // Shell folders expand %USERPROFILE%; without them, file dialogs report that their default location is missing.
    foreach (var folder in new[] { "Desktop", "Documents", "Downloads", "Music", "Pictures", "Videos" })
      Directory.CreateDirectory(Path.Combine(Profile, folder));

    if (smExePath is null)
      return;

    var coreCfgDir = Path.Combine(DataDir, "Configs", "Core");
    Directory.CreateDirectory(coreCfgDir);
    File.WriteAllText(Path.Combine(coreCfgDir, "CoreCfg.json"), JsonSerializer.Serialize(new
    {
      SuperMemo                 = new { SMBinPath = smExePath },
      Updates                   = new { EnableCoreUpdates = false, EnablePluginsUpdates = false, ChangeLogLastCrc32 = ChangeLogCrc32() },
      HasAgreedToTermsOfLicense = true,
      HasImportedCollections    = true,
    }));
  }

  public static string RepoRoot
  {
    get
    {
      var dir = new DirectoryInfo(AppContext.BaseDirectory);
      while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
        dir = dir.Parent;
      return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
  }

  public static string AppDir => Path.Combine(RepoRoot, "artifacts", "app-dev");

  public static string AppExe => Path.Combine(AppDir, "SuperMemoAssistant.exe");

  public string Profile { get; }

  public string DataDir => Path.Combine(Profile, "SuperMemoAssistant");

  public int ProcessId => _process.ProcessId;

  /// <summary>Copies a built plugin (bin\x64\Debug) into Plugins\Development\{name}. Throws when the plugin is not built.</summary>
  public void SeedPlugin(string name)
  {
    var dll = Directory.EnumerateFiles(Path.Combine(RepoRoot, "src", "Plugins"), name + ".dll", SearchOption.AllDirectories)
                       .Where(p => p.Contains(@"\bin\x64\Debug\", StringComparison.OrdinalIgnoreCase) && !p.Contains(@"\obj\"))
                       .OrderByDescending(File.GetLastWriteTimeUtc)
                       .FirstOrDefault()
              ?? throw new FileNotFoundException($"Plugin {name} is not built (expected bin\\x64\\Debug\\{name}.dll).");

    var source = Path.GetDirectoryName(dll)!;
    var target = Path.Combine(DataDir, "Plugins", "Development", name);
    foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
    {
      var destination = Path.Combine(target, Path.GetRelativePath(source, file));
      Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
      File.Copy(file, destination, overwrite: true);
    }
  }

  /// <summary>Writes a plugin's saved settings, where its Svc.Configuration.Load finds them: Configs\{plugin}\{type name}.json.</summary>
  public void SeedPluginConfig<T>(string pluginAssemblyName, T config)
  {
    var folder = Path.Combine(DataDir, "Configs", pluginAssemblyName);

    Directory.CreateDirectory(folder);
    File.WriteAllText(Path.Combine(folder, typeof(T).Name + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(config));
  }

  public void Launch(string arguments) => Launch(AppExe, arguments);

  public void Launch(string exePath, string arguments)
  {
    WaitForEarlierInstances(exePath);

    _process = NativeProcess.StartSuspended(exePath, arguments, Path.GetDirectoryName(exePath), _desktop.StartupDesktop, _environment);
    _desktop.Track(_process.ProcessHandle);
    NativeProcess.Resume(_process.ThreadHandle);
  }

  /// <summary>
  ///   An SMA of an earlier test can still be exiting. It holds the single-instance lock of its executable, and an SMA
  ///   started now would hand over to it and quit.
  /// </summary>
  private static void WaitForEarlierInstances(string exePath)
  {
    foreach (var running in System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath)))
      using (running)
      {
        try
        {
          if (string.Equals(running.MainModule?.FileName, Path.GetFullPath(exePath), StringComparison.OrdinalIgnoreCase))
            running.WaitForExit(TimeSpan.FromSeconds(15));
        }
        catch (System.ComponentModel.Win32Exception) { } // a process of another session or that already exited
        catch (InvalidOperationException) { }
      }
  }

  public bool HasExited
  {
    get
    {
      try
      {
        using var process = System.Diagnostics.Process.GetProcessById(ProcessId);
        return process.HasExited;
      }
      catch (ArgumentException)
      {
        return true; // no longer running
      }
    }
  }

  public string ReadLog()
  {
    var logDir = Path.Combine(DataDir, "Logs");
    if (!Directory.Exists(logDir))
      return "";

    return string.Concat(Directory.GetFiles(logDir, "*.log").Order().Select(ReadShared));
  }

  public void Dispose()
  {
    _desktop.TerminateProcesses();
    NativeProcess.Close(_process.ThreadHandle);
    NativeProcess.Close(_process.ProcessHandle);

    for (var attempt = 0; attempt < 20; attempt++)
      try
      {
        if (Directory.Exists(Profile))
          Directory.Delete(Profile, recursive: true);
        return;
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        Thread.Sleep(500); // processes killed with the desktop job can hold handles for a moment
      }
  }

  /// <summary>Same text and checksum as ChangeLogWindow.ShowIfUpdated, so the first-run changelog dialog stays closed.</summary>
  private static string ChangeLogCrc32()
  {
    var text = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(Path.Combine(RepoRoot, "ChangeLogs")), "^[^\\[]*", string.Empty);
    return SuperMemoAssistant.Extensions.StringEx.GetCrc32("Change logs\n\n\n" + text);
  }

  private static string ReadShared(string path)
  {
    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}
