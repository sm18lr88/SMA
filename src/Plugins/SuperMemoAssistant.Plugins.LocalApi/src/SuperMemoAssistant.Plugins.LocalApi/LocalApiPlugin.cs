namespace SuperMemoAssistant.Plugins.LocalApi;

using System.Threading.Tasks;
using Anotar.Serilog;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.Plugins;
using SuperMemoAssistant.Plugins.LocalApi.Server;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.IO.HotKeys;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.Services.ToastNotifications;
using SuperMemoAssistant.Services.UI.Configuration;

/// <summary>
///   Runs a token-protected HTTP server on the loopback interface, so that browser extensions, user scripts, and
///   command-line tools can add elements to SuperMemo and navigate. The server is off until the user turns it on.
/// </summary>
public class LocalApiPlugin : SMAPluginBase<LocalApiPlugin>
{
  private readonly object _serverLock = new();
  private LocalApiServer? _server;
  private bool            _shuttingDown;

  /// <summary>The plugin settings.</summary>
  public LocalApiCfg Config { get; private set; } = new();

  /// <inheritdoc />
  public override string Name => "Local API";

  /// <inheritdoc />
  public override bool HasSettings => true;

  /// <inheritdoc />
  protected override void OnPluginInitialized()
  {
    Config = Svc.Configuration.Load<LocalApiCfg>() ?? new LocalApiCfg();
    RestartServerInBackground();

    RegisterPaletteCommand("CopyToken", "Copy the Local API token", () => Svc.App.Dispatcher.Invoke(Config.CopyToken),
                           HotKeyScopes.Global);

    base.OnPluginInitialized();
  }

  /// <inheritdoc />
  public override void ShowSettings()
  {
    var window = ConfigurationWindow.ShowAndActivate("Local API Settings", (HotKeyManager?)null, Config);

    if (window != null)
      window.SaveMethod = _ =>
      {
        Svc.Configuration.SaveAsync(Config).RunAsync(ex => LogTo.Error(ex, "Local API: the settings could not be saved"));
        RestartServerInBackground();
      };
  }

  /// <inheritdoc />
  protected override void Dispose(bool disposing)
  {
    if (disposing)
      lock (_serverLock)
      {
        _shuttingDown = true;
        StopServer();
      }

    base.Dispose(disposing);
  }

  /// <summary>Stopping waits for requests in progress, so it does not run on the UI thread.</summary>
  private void RestartServerInBackground() =>
    Task.Run(RestartServer).RunAsync(ex => LogTo.Error(ex, "Local API: the server could not be restarted"));

  private void RestartServer()
  {
    lock (_serverLock)
    {
      if (_shuttingDown)
        return;

      StopServer();

      if (Config.Enabled == false)
        return;

      if (string.IsNullOrEmpty(Config.Token))
      {
        Config.Token = TokenGenerator.Create();
        Svc.Configuration.Save(Config);
      }

      var options = new ApiOptions(Config.Port,
                                   Config.Token,
                                   Config.AllowedOriginList(),
                                   Config.DefaultParent == ParentChoice.CurrentElement,
                                   Config.DefaultPriority);

      if (LocalApiServer.TryStart(options, new SuperMemoGateway(), out var server, out var error))
      {
        _server = server;
        LogTo.Information("Local API: listening on {Prefixes}", string.Join(", ", server.Prefixes));
        return;
      }

      LogTo.Warning("Local API: the server did not start: {Error}", error);
      $"Local API: {error}".ShowDesktopNotification();
    }
  }

  private void StopServer()
  {
    if (_server == null)
      return;

    _server.Dispose();
    _server = null;
    LogTo.Information("Local API: the server stopped");
  }
}
