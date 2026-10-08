// Creates SMA's process-wide services (logging, configuration, hotkeys, SMA core, plugin manager) before the UI starts.
namespace SuperMemoAssistant
{
  using System;
  using System.IO;
  using Anotar.Serilog;
  using Exceptions;
  using Extensions;
  using Interop;
  using Plugins;
  using Services.Configuration;
  using Services.IO.Diagnostics;
  using Services.IO.HotKeys;
  using Services.IO.Keyboard;
  using SMA;
  using SuperMemo.Common.Content.Layout;

  public static class AppBootstrap
  {
    public static void Initialize()
    {
      try
      {
        // Required for logging
        SMA.Core.SharedConfiguration = new ConfigurationService(SMAFileSystem.SharedConfigDir);

        SMA.Core.Logger = LoggerFactory.Create(SMAConst.Name, SMA.Core.SharedConfiguration);

        Logger.ReloadAnotarLogger(typeof(AppBootstrap));

        SMA.Core.SMAVersion = typeof(App).GetAssemblyVersion();
        LogTo.Information("SuperMemo Assistant version {SMAVersion} starting.", SMA.Core.SMAVersion);

        SMA.Core.Configuration   = new ConfigurationService(SMAFileSystem.ConfigDir.Combine("Core"));
        SMA.Core.KeyboardHotKey  = KeyboardHookService.Instance;
        SMA.Core.HotKeyManager   = HotKeyManager.Instance.Initialize(SMA.Core.Configuration, SMA.Core.KeyboardHotKey);
        SMA.Core.NotificationMgr = NotificationManager.Instance;
        SMA.Core.SMA             = new SMA.SMA();

        _ = LayoutManager.Instance;
        _ = SMAPluginManager.Instance;
      }
      catch (SMAException ex)
      {
        LogTo.Warning(ex, "Error during SuperMemoAssistant initialization.");
        File.WriteAllText(SMAFileSystem.TempErrorLog.FullPath, $"Error during SuperMemoAssistant initialization: {ex}");
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception thrown during SuperMemoAssistant initialization.");
        File.WriteAllText(SMAFileSystem.TempErrorLog.FullPath, $"Exception thrown during SuperMemoAssistant initialization: {ex}");
      }
    }
  }
}
