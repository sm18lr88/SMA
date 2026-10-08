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




namespace SuperMemoAssistant.Interop.SMA.Notifications
{
  using System;
  using PluginManager.Interop.Plugins;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

  /// <summary>Enables displaying and interacting with Windows Toast Notifications.</summary>
  public interface INotificationManager
  {
    /// <summary>Shows a Windows Toast notification on the user's desktop. Use with caution.</summary>
    /// <remarks>
    ///   Do not use that API directly. Use <see cref="SuperMemoAssistant.Services.ToastNotifications.INotificationManagerEx" />
    ///   from the SuperMemoAssistant.Services.ToastNotifications NuGet package instead.
    /// </remarks>
    /// <param name="toastXml">Windows Toast XML content.</param>
    /// <param name="pluginSessionGuid">
    ///   The Plugin Session GUID, see
    ///   <see cref="PluginBase{TPlugin,IPlugin,ICore}.SessionGuid" />.
    /// </param>
    /// <returns>Whether the toast was displayed successfully or not.</returns>
    bool ShowDesktopNotification(string toastXml, Guid pluginSessionGuid);

    /// <summary>
    ///   Triggered when a user interact with a Toast Notification (e.g. pressing a button, or clicking on the notification).
    /// </summary>
    event Action<ToastActivationData> OnToastActivated;
  }
}
