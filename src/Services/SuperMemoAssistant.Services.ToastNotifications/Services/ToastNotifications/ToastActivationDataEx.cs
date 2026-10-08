using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Services.ToastNotifications
{
  using Interop.SMA.Notifications;

  /// <summary>
  /// Extends <see cref="ToastActivationData"/>.
  /// </summary>
  public static class ToastActivationDataEx
  {
    /// <summary>
    /// Checks whether this activation data belongs to a notification sent by this plugin.
    /// </summary>
    /// <param name="toastActivationData"></param>
    /// <returns></returns>
    public static bool WasTriggeredByThisPlugin(this ToastActivationData toastActivationData)
    {
      dynamic plugin = Svc.Plugin;

      return toastActivationData.PluginPackageName == plugin.AssemblyName;
    }
  }
}
