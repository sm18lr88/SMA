// Routes Windows toast activations to the plugin that showed the toast, or to SMA's own toast actions.
namespace SuperMemoAssistant
{
  using System;
  using System.Linq;
  using System.Windows;
  using Anotar.Serilog;
  using Extensions;
  using Interop.SMA.Notifications;
  using Microsoft.QueryStringDotNET;
  using Microsoft.Toolkit.Uwp.Notifications;
  using Plugins;
  using SMA;

  internal static class ToastActivationHandler
  {
    public static void Handle(ToastNotificationActivatedEventArgsCompat e)
    {
      if (string.IsNullOrEmpty(e.Argument))
        return;

      var args = QueryString.Parse(e.Argument);
      var userInput = e.UserInput.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString());

      Application.Current?.Dispatcher.Invoke(() =>
      {
        if (args.Contains(NotificationManager.PluginSessionGuidArgName))
          ActivatePluginToast(args, userInput);
        else
          ActivateSmaToast(args, e.Argument);
      });
    }

    private static void ActivatePluginToast(QueryString args, System.Collections.Generic.Dictionary<string, string> userInput)
    {
      var sessionGuidStr = args[NotificationManager.PluginSessionGuidArgName];
      if (!Guid.TryParse(sessionGuidStr, out var sessionGuid))
      {
        LogTo.Error("A notification was activated for an invalid GUID.\r\nGUID: {Guid}\r\nArgs: {Args}", sessionGuidStr, args);
        return;
      }

      var plugin = SMAPluginManager.Instance[sessionGuid];
      if (plugin == null)
      {
        LogTo.Debug("A notification was activated for an expired plugin.\r\n{Args}", args);
        return;
      }

      var activationData = new ToastActivationData(plugin.Package.Id, plugin.Package.Version,
                                                   args.ToDictionary(k => k.Name, v => v.Value), userInput);
      SMA.Core.NotificationMgr.RaiseToastActivated(activationData);
    }

    private static void ActivateSmaToast(QueryString args, string rawArguments)
    {
      if (!args.Contains("action"))
      {
        LogTo.Warning("Received a Toast activation without an action argument: '{Args}'", args);
        return;
      }

      switch (args["action"])
      {
        case SMAPluginManager.ToastActionRestartAfterCrash when args.Contains(SMAPluginManager.ToastActionParameterPluginId):
          SMAPluginManager.Instance.StartPluginAsync(args[SMAPluginManager.ToastActionParameterPluginId]).RunAsync();
          break;

        default:
          LogTo.Warning("Unknown or incomplete notification action: '{Arguments}'", rawArguments);
          break;
      }
    }
  }
}
