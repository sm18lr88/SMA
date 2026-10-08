#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion




// ReSharper disable once CheckNamespace
namespace SuperMemoAssistant.Services.ToastNotifications
{
  using Interop.SMA.Notifications;
  using Microsoft.Toolkit.Uwp.Notifications;

  /// <summary>Facilitates using Windows Toast Notifications through SMA.</summary>
  public static class INotificationManagerEx
  {
    #region Methods

    /// <summary>Shows a toast using Windows Desktop notifications.</summary>
    /// <remarks>
    ///   See <see cref="INotificationManager.OnToastActivated" /> to be notified when a user either activates your toast or
    ///   clicks on one of the buttons (when applicable).
    /// </remarks>
    /// <param name="notificationText">The main text contained in the notification</param>
    /// <param name="buttons">Optional buttons enabling the user to react</param>
    /// <returns>Whether the notification was shown.</returns>
    public static bool ShowDesktopNotification(this string notificationText, params IToastButton[] buttons)
    {
      var toastContent = new ToastContent
      {
        Visual = new ToastVisual
        {
          BindingGeneric = new ToastBindingGeneric
          {
            Children =
            {
              new AdaptiveText
              {
                Text = notificationText
              }
            }
          }
        }
      };

      if (buttons?.Length > 0)
      {
        var toastActions = new ToastActionsCustom();

        foreach (var toastButton in buttons)
          toastActions.Buttons.Add(toastButton);

        toastContent.Actions = toastActions;
      }

      return toastContent.Show();
    }

    /// <summary>Shows <paramref name="toastContent" /> using Windows Desktop notifications.</summary>
    /// <remarks>
    ///   See <see cref="INotificationManager.OnToastActivated" /> to be notified when a user either activates your toast or
    ///   clicks on one of the buttons (when applicable).
    /// </remarks>
    /// <param name="toastContent">The toast definition</param>
    /// <returns>Whether the notification was shown.</returns>
    public static bool Show(this ToastContent toastContent)
    {
      dynamic plugin = Svc.Plugin;

      /*toastContent.Launch = toastContent.Launch.EnsureParametersContainPluginSessionId(plugin);

      if (toastContent.Actions is ToastActionsCustom actionsCustom)
      {
        foreach (var button in actionsCustom.Buttons)
        {
          action.
        }
      }*/

      return Svc.SMA.NotificationMgr.ShowDesktopNotification(toastContent.GetContent(), plugin.SessionGuid);
    }

    #endregion




    /*
    public static void EnsureParametersContainPluginSessionId<TPlugin>(
    this IToastButton button,
                TPlugin plugin)
    where TPlugin : SMAPluginBase<TPlugin>
    {
      switch (button)
      {
        case ToastButton toastButton:
          toastButton.Arguments = toastButton.Arguments.EnsureParametersContainPluginSessionId(plugin);
          break;

        case ToastButtonSnooze toastButtonSnooze:
          new ToastButtonSnooze()

          break;
      }
    }

    private static string EnsureParametersContainPluginSessionId<TPlugin>(
      this string parameters,
      TPlugin plugin)
      where TPlugin : SMAPluginBase<TPlugin>
    {
      QueryString args = QueryString.Parse(parameters);

      if (args.Contains(SMAConst.API.ToastNotificationPluginParameterName) == false)
        args.Set(SMAConst.API.ToastNotificationPluginParameterName, plugin.SessionGuid.ToString());
    }
    */
  }
}
