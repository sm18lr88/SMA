// Checks for SMA updates with Velopack, downloads them, and stages them to apply when SMA exits.
namespace SuperMemoAssistant.Installer
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Interop;
  using Microsoft.Toolkit.Uwp.Notifications;
  using Sys.Windows;
  using Sys.Windows.Net;
  using Velopack;
  using Velopack.Sources;

  public sealed class SMAUpdater
  {
    private readonly SemaphoreSlim _gate = new(1, 1);

    private SMAUpdater() { }

    public static SMAUpdater Instance { get; } = new();

    public SMAUpdateState State       { get; private set; } = SMAUpdateState.Idle;
    public int            ProgressPct { get; private set; }

    private static bool   UpdateEnabled => SMA.Core.CoreConfig?.Updates.EnableCoreUpdates ?? false;
    private static string UpdateUrl     => SMA.Core.CoreConfig?.Updates.CoreUpdateUrl;
    private static bool   Prerelease    => SMA.Core.CoreConfig?.Updates.CoreUpdateChannelIsPrerelease ?? false;

    /// <summary>
    ///   SMA channels (Stable, Beta, Nightly) select stable or prerelease GitHub releases. Velopack's own channel stays at
    ///   its default ("win"), the channel that build\pack.ps1 packs.
    /// </summary>
    internal static IUpdateSource CreateSource(string url, bool prerelease) =>
      Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        ? new GithubSource(url, null, prerelease)
        : new SimpleWebSource(url);

    /// <summary>Checks for and downloads an update; it is applied when SMA exits. No-op for development builds or when disabled.</summary>
    public async Task UpdateAsync()
    {
      if (!UpdateEnabled || string.IsNullOrWhiteSpace(UpdateUrl) || !Wininet.HasNetworking())
        return;

      if (!await _gate.WaitAsync(0).ConfigureAwait(false))
        return; // an update is already running

      string targetVersion = null;
      try
      {
        var manager = new UpdateManager(CreateSource(UpdateUrl, Prerelease));

        if (!manager.IsInstalled)
          return;

        State = SMAUpdateState.Fetching;
        var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (update == null)
        {
          State = SMAUpdateState.UpToDate;
          LogTo.Information("SMA is up to date");
          return;
        }

        targetVersion = update.TargetFullRelease.Version.ToString();
        LogTo.Information("Downloading the SMA {Version} update", targetVersion);

        State = SMAUpdateState.Downloading;
        await manager.DownloadUpdatesAsync(update, progress => ProgressPct = progress).ConfigureAwait(false);

        manager.WaitExitThenApplyUpdates(update.TargetFullRelease, silent: true, restart: false);
        State = SMAUpdateState.Updated;
        LogTo.Information("SMA {Version} is downloaded and installs when SMA closes", targetVersion);
      }
      catch (Exception ex)
      {
        LogTo.Warning(ex, "Update failed while {State}", State);
        State = SMAUpdateState.Error;
      }
      finally
      {
        _gate.Release();

        if (targetVersion != null)
          NotifyUpdateResult(targetVersion);
      }
    }

    /// <summary>Blocks until a running update finishes, so SMA does not exit mid-download.</summary>
    public void WaitForIdle()
    {
      _gate.Wait();
      _gate.Release();
    }

    private void NotifyUpdateResult(string version)
    {
      var msg = State == SMAUpdateState.Updated
        ? $"SMA {version} has been downloaded and will be installed when SMA closes."
        : $"An error occurred while updating SMA to version {version}. Check the logs for more information.";

      msg.ShowDesktopNotification(
        new ToastButton("Open the logs folder", SMAFileSystem.LogDir.FullPathWin)
        {
          ActivationType = ToastActivationType.Protocol
        });
    }
  }

  public enum SMAUpdateState
  {
    Idle,
    Error,
    UpToDate,
    Fetching,
    Downloading,
    Updated,
  }
}
