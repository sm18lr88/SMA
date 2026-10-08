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
// 
// 
// Created On:   2020/03/29 06:07
// Modified On:  2020/04/10 14:17
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.UI
{
  using System.Windows;
  using System.Windows.Input;
  using DataTemplates;
  using Interop.SMA;
  using SMA.Commands;
  using Services.IO.Keyboard;
  using Settings;
  using Sys.IO.Devices;

  // ReSharper disable once InconsistentNaming
  public static class SMAUI
  {
    #region Methods

    public static void Initialize()
    {
      var commands = SMA.Core.SMA.Commands;
      SMA.Core.HotKeyManager.PublishCommands(
        hk => commands.Register(PaletteCommand.FromHotKey(hk, PaletteCommand.SmaOwner, "SMA"), hk.Callback),
        hk => commands.Unregister(PaletteCommand.SmaOwner, hk.Id));

      SMA.Core.HotKeyManager.RegisterGlobal(
        "Settings",
        "Show settings window",
        HotKeyScopes.Global,
        new HotKey(Key.O, KeyModifiers.CtrlAltShift),
        ShowGlobalSettings
      );

      SMA.Core.HotKeyManager.RegisterGlobal(
        CommandPalette.HotKeyId,
        "Show the command palette",
        HotKeyScopes.Global,
        new HotKey(Key.P, KeyModifiers.CtrlAltShift),
        () => Application.Current.Dispatcher.Invoke(CommandPalette.ShowOrActivate)
      );

      // Set default data templates
      Application.Current.Dispatcher.Invoke(() =>
      {
        Application.Current.Resources.MergedDictionaries.Add(new OnlinePluginPackageDataTemplate().Resources);
        Application.Current.Resources.MergedDictionaries.Add(new LocalPluginPackageDataTemplate().Resources);
      });
    }

    private static void ShowGlobalSettings()
    {
      Application.Current.Dispatcher.Invoke(SettingsWindow.ShowOrActivate);
    }

    #endregion
  }
}
