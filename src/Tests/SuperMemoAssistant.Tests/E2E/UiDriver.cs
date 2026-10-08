// Drives windows on the hidden desktop through the UI agent (SuperMemoAssistant.Tests.UiAgent), which runs on that desktop.
namespace SuperMemoAssistant.Tests.E2E;

using System.IO.Pipes;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using SuperMemoAssistant.SuperMemo.Hooks;

internal sealed class UiDriver : IDisposable
{
  private const string AvalonKey = @"Software\Microsoft\Avalon.Graphics";
  private const string SoftwareRenderingValue = "DisableHWAcceleration";

  private readonly NamedPipeServerStream _pipe;
  private readonly StreamReader _reader;
  private readonly StreamWriter _writer;
  private readonly object? _previousRendering;
  private readonly Action<string> _log;
  private int _shots;
  private bool _failing;
  private readonly CancellationTokenRegistration _cancellation;

  /// <summary>
  ///   Starts the agent on <paramref name="desktop" />. Until disposal, WPF apps of this user render in software: DWM
  ///   captures hardware-rendered WPF windows on an unseen desktop as black.
  /// </summary>
  public UiDriver(HiddenDesktop desktop, string screenshotDir, Action<string> log, CancellationToken cancellation)
  {
    ScreenshotDir = screenshotDir;
    _log = log;
    Directory.CreateDirectory(screenshotDir);

    using (var avalon = Registry.CurrentUser.CreateSubKey(AvalonKey))
    {
      _previousRendering = avalon.GetValue(SoftwareRenderingValue);
      avalon.SetValue(SoftwareRenderingValue, 1, RegistryValueKind.DWord);
    }

    var pipeName = $"sma-ui-agent-{Guid.NewGuid():N}";
    _pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    var agent = NativeProcess.StartSuspended(AgentExe, pipeName, Path.GetDirectoryName(AgentExe)!, desktop.StartupDesktop);
    desktop.Track(agent.ProcessHandle);
    NativeProcess.Resume(agent.ThreadHandle);
    NativeProcess.Close(agent.ThreadHandle);
    NativeProcess.Close(agent.ProcessHandle);

    using var connected = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    _pipe.WaitForConnectionAsync(connected.Token).GetAwaiter().GetResult();
    _reader = new StreamReader(_pipe);
    _writer = new StreamWriter(_pipe) { AutoFlush = true };

    // Closing the pipe ends a blocked command, so a test timeout stops the walkthrough at once.
    _cancellation = cancellation.Register(_pipe.Dispose);
  }

  public static string AgentExe =>
    Path.Combine(SmaAppHarness.RepoRoot, "src", "Tests", "SuperMemoAssistant.Tests.UiAgent", "bin", "x64", "Debug",
                 "SuperMemoAssistant.Tests.UiAgent.exe");

  public string ScreenshotDir { get; }

  public void Dispose()
  {
    _cancellation.Dispose();
    _pipe.Dispose();

    using var avalon = Registry.CurrentUser.CreateSubKey(AvalonKey);
    if (_previousRendering is null)
      avalon.DeleteValue(SoftwareRenderingValue, throwOnMissingValue: false);
    else
      avalon.SetValue(SoftwareRenderingValue, _previousRendering);
  }

  /// <summary>Waits until a window whose title contains <paramref name="window" /> appears.</summary>
  public void WaitForWindow(string window, int timeoutMs = 60_000)
  {
    if (Send(new JsonObject { ["op"] = "wait-window", ["window"] = window, ["timeoutMs"] = timeoutMs }).GetValue<int>() == 0)
      Fail($"Window \"{window}\" did not appear within {timeoutMs / 1000} s.", null);
  }

  public bool WaitFor(string window, string name, int timeoutMs = 30_000, string? type = null)
  {
    var query = Query("wait", window, name, type);
    query["timeoutMs"] = timeoutMs;
    return Send(query).GetValue<bool>();
  }

  public bool Exists(string window, string name, string? type = null) => Send(Query("exists", window, name, type)).GetValue<bool>();

  /// <summary>Reads an element, or returns null when it is missing or is being replaced (for example, during a re-template).</summary>
  public JsonNode? Read(string window, string name, string? type = null) => SendOrNull(Query("read", window, name, type));

  public void Click(string window, string name, string? type = null, int index = 0, string? within = null)
  {
    var query = Query("invoke", window, name, type);
    query["index"] = index;
    if (within is not null)
      query["within"] = within;
    Send(query);
  }

  public void Select(string window, string name, string? type = null) => Send(Query("select", window, name, type));

  public void Expand(string window, string name, string? type = null) => Send(Query("expand", window, name, type));

  public void SetValueById(string window, string automationId, string text) =>
    Send(new JsonObject { ["op"] = "set-value", ["window"] = window, ["id"] = automationId, ["text"] = text });

  public void CloseWindow(string window) => Send(new JsonObject { ["op"] = "close", ["window"] = window });

  public string Describe(string window) => Send(new JsonObject { ["op"] = "describe", ["window"] = window }).GetValue<string>();

  public List<string> WindowTitles() =>
    Send(new JsonObject { ["op"] = "windows" }).AsArray().Select(w => w!["name"]!.GetValue<string>()).ToList();

  /// <summary>Saves a numbered screenshot of <paramref name="window" /> and returns its path.</summary>
  public string Screenshot(string window, string step)
  {
    var path = Path.Combine(ScreenshotDir, $"{++_shots:00}-{step}.png");
    Send(new JsonObject { ["op"] = "screenshot", ["window"] = window, ["path"] = path });
    _log("screenshot " + path);
    return path;
  }

  /// <summary>Captures a window by its handle, without UI Automation, so that a window whose UI thread is busy still shows.</summary>
  public string Screenshot(nint hwnd, string step)
  {
    var path = Path.Combine(ScreenshotDir, $"{++_shots:00}-{step}.png");
    Send(new JsonObject { ["op"] = "screenshot", ["hwnd"] = (long)hwnd, ["path"] = path });
    _log("screenshot " + path);
    return path;
  }

  private static JsonObject Query(string op, string window, string name, string? type)
  {
    var query = new JsonObject { ["op"] = op, ["window"] = window, ["name"] = name };
    if (type is not null)
      query["type"] = type;
    return query;
  }

  private JsonNode Send(JsonObject command) => SendOrNull(command) ?? JsonValue.Create(0);

  private JsonNode? SendOrNull(JsonObject command)
  {
    _writer.WriteLine(command.ToJsonString());
    var reply = JsonNode.Parse(_reader.ReadLine() ?? throw new IOException("The UI agent closed the connection."))!;
    if (reply["ok"]!.GetValue<bool>())
      return reply["result"];

    Fail($"UI command {command.ToJsonString()} failed: {reply["error"]}", command["window"]?.GetValue<string>());
    return null!;
  }

  /// <summary>Logs the element list and a screenshot of the window, then fails the test.</summary>
  private void Fail(string message, string? window)
  {
    if (_failing)
      Xunit.Assert.Fail(message);

    _failing = true;
    if (window is not null)
      try
      {
        _log("elements of " + window + ":" + Environment.NewLine + Describe(window));
        Screenshot(window, "failure");
      }
      catch (Exception ex) when (ex is IOException or Xunit.Sdk.XunitException)
      {
        _log("could not capture the failing window: " + ex.Message);
      }

    try
    {
      var titles = WindowTitles();
      _log("open windows: " + string.Join(" | ", titles));
      foreach (var title in titles.Where(t => t.Length > 0 && t != window))
      {
        _log("elements of " + title + ":" + Environment.NewLine + Describe(title));
        Screenshot(title, "open-window");
      }
    }
    catch (Exception ex) when (ex is IOException or Xunit.Sdk.XunitException)
    {
      _log("could not list the windows: " + ex.Message);
    }

    Xunit.Assert.Fail(message);
  }
}
