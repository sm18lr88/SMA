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
// Created On:   2019/02/25 22:02
// Modified On:  2019/03/02 04:11
// Modified By:  Alexis

#endregion




using System;
using System.Windows;
using SuperMemoAssistant.Interop.Plugins;
using SuperMemoAssistant.Interop.SuperMemo;
using SuperMemoAssistant.Services.Configuration;
using SuperMemoAssistant.Services.IO.HotKeys;
using SuperMemoAssistant.Services.IO.Keyboard;

// ReSharper disable StaticMemberInGenericType
// ReSharper disable UnusedTypeParameter

namespace SuperMemoAssistant.Services
{
  using Interop.SMA;
  using IO.Diagnostics;

  /// <summary>
  /// Convenience singleton with access to the most important services
  /// </summary>
  public static class Svc
  {
    #region Constants & Statics

    private static ISuperMemoAssistant _sma;

    /// <summary>
    /// The SMA service
    /// </summary>
    public static ISuperMemoAssistant SMA
    {
      get => _sma;
      set
      {
        _sma = value;
        OnSMAAvailable?.Invoke(value);
      }
    }

    /// <summary>
    /// The SM service
    /// </summary>
    public static ISuperMemo SM => SMA?.SM;

    /// <summary>
    /// The current plugin
    /// </summary>
    public static ISMAPlugin Plugin { get; set; }

    /// <summary>
    /// Legacy hotkey service
    /// </summary>
    [Obsolete("Use KeyboardHotkey")]
    public static IKeyboardHotKeyService KeyboardHotKeyLegacy { get; set; }

    /// <summary>
    /// The hotkey service. Registers hotkey directly with Windows. 
    /// </summary>
    public static IKeyboardHookService   KeyboardHotKey       { get; set; }
    
    /// <summary>
    /// Facilitates managing hotkeys. Offers additional features such as rebinding, config saving and loading, etc.
    /// </summary>
    public static HotKeyManager          HotKeyManager        { get; set; }

    /// <summary>
    /// The global logger
    /// </summary>
    public static Logger Logger { get; set; }
    
    /// <summary>
    /// The global configuration service (stored in My Documents\SuperMemoAssistant\Configs\[PluginName])
    /// </summary>
    public static ConfigurationServiceBase Configuration { get; set; }

    /// <summary>
    /// The shared global configuration service (stored in My Documents\SuperMemoAssistant\Configs\Shared)
    /// </summary>
    public static ConfigurationServiceBase SharedConfiguration { get; set; }

    /// <summary>
    /// The collection configuration service (stored in the sma\configs)
    /// </summary>
    public static ConfigurationServiceBase CollectionConfiguration { get; set; }

    /// <summary>
    /// The WPF app instance
    /// </summary>
    public static Application App { get; set; }

    #endregion




    #region Events

    /// <summary>
    /// Raised when the <see cref="SMA"/> service becomes available
    /// </summary>
    public static event Action<ISuperMemoAssistant> OnSMAAvailable;

    #endregion
  }

  /// <summary>
  /// Services bound to the hosting plugin type
  /// </summary>
  /// <typeparam name="T"></typeparam>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "<Pending>")]
  public static class Svc<T>
    where T : ISMAPlugin
  {
    #region Constants & Statics

    /// <summary>
    /// The current plugin instance if the <typeparamref name="T"/> is valid, or null
    /// </summary>
    public static T Plugin => (T)Svc.Plugin;

    #endregion
  }
}
