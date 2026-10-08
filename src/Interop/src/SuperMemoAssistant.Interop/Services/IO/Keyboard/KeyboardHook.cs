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




// ReSharper disable InconsistentNaming

namespace SuperMemoAssistant.Services.IO.Keyboard
{
  using System;
  using System.Collections.Concurrent;
  using System.ComponentModel;
  using System.Diagnostics;
  using System.Runtime.InteropServices;
  using System.Threading;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Extensions;
  using Interop.SMA;
  using Sys.IO.Devices;
  using Sys.Remoting;

  /// <summary>Facilitates handling hotkeys in Windows using the Windows Hook API</summary>
  /// <remarks>
  ///   https://stackoverflow.com/questions/604410/global-keyboard-capture-in-c-sharp-application Based on
  ///   https://gist.github.com/Stasonix
  /// </remarks>
  public sealed class KeyboardHookService : IDisposable, IKeyboardHookService
  {
    #region Constants & Statics

    /// <summary>The singleton</summary>
    public static KeyboardHookService Instance { get; } = new KeyboardHookService();

    #endregion




    #region Properties & Fields - Non-Public


    private Native.KeyboardHookHandler _hookProc;
    private bool                       _isDisposed;
    private IntPtr                     _windowsHookHandle;

    private ConcurrentDictionary<HotKey, RegisteredHotKey> HotKeys { get; } =
      new ConcurrentDictionary<HotKey, RegisteredHotKey>();
    private ConcurrentQueue<Action> TriggeredCallbacks { get; } = new ConcurrentQueue<Action>();
    private AutoResetEvent          TriggeredEvent     { get; } = new AutoResetEvent(false);
    private CancellationTokenSource DisposeCts         { get; } = new CancellationTokenSource();

    #endregion




    #region Constructors

    private KeyboardHookService()
    {
      // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.
      _hookProc          = LowLevelKeyboardProc;
      _windowsHookHandle = IntPtr.Zero;

      var _ = Task.Factory.StartNew(
        ExecuteCallbacks,
        DisposeCts.Token,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);

      using (Process curProcess = Process.GetCurrentProcess())
      using (ProcessModule curModule = curProcess.MainModule)
        _windowsHookHandle = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL,
                                                     _hookProc,
                                                     Native.GetModuleHandle(curModule.ModuleName),
                                                     0);

      if (_windowsHookHandle == IntPtr.Zero)
      {
        int errorCode = Marshal.GetLastWin32Error();
        throw new Win32Exception(errorCode,
                                 $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
      }

      //Svc.OnSMAAvailable += OnSMAAvailable;
    }

    /// <summary>Destructor</summary>
    ~KeyboardHookService()
    {
      Dispose(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>Dispose logic</summary>
    /// <param name="isDisposing"></param>
    public void Dispose(bool isDisposing)
    {
      if (_isDisposed)
        return;

      if (isDisposing)
        TriggeredEvent.Set();

      DisposeCts.CancelAfter(1500);

      if (_windowsHookHandle != IntPtr.Zero)
      {
        if (!Native.UnhookWindowsHookEx(_windowsHookHandle))
        {
          int errorCode = Marshal.GetLastWin32Error();
          LogTo.Warning(
            "Failed to remove keyboard hooks for '{ProcessName}'. Error {ErrorCode}: {Message}.",
            Process.GetCurrentProcess().ProcessName,
            errorCode,
            Marshal.GetLastWin32Error());
        }

        _windowsHookHandle = IntPtr.Zero;

        // ReSharper disable once DelegateSubtraction
        _hookProc -= LowLevelKeyboardProc;
      }

      _isDisposed = true;
    }

    #endregion




    #region Properties Impl - Public

    /// <summary>Optional global callback on each key press</summary>
    public Action<HotKey> MainCallback { get; set; }

    #endregion




    #region Methods Impl

    /// <summary>Removes <paramref name="hotkey" /> from the list of registered hotkeys</summary>
    /// <param name="hotkey"></param>
    /// <returns></returns>
    public bool UnregisterHotKey(HotKey hotkey)
    {
      return HotKeys.TryRemove(hotkey, out _);
    }

    /// <summary>
    ///   Registers <paramref name="callback" /> to be called when <paramref name="hotkey" /> is pressed in the given scope
    /// </summary>
    /// <param name="hotkey">The trigger hotkey</param>
    /// <param name="callback">The callback to call when the hotkey is pressed</param>
    /// <param name="scopes">Which scope does this hotkey applies to</param>
    public void RegisterHotKey(HotKey       hotkey,
                               Action       callback,
                               HotKeyScopes scopes = HotKeyScopes.SM)
    {
      HotKeys[hotkey] = new RegisteredHotKey(callback, scopes);
    }

    #endregion




    #region Methods

    private void ExecuteCallbacks()
    {
      while (_isDisposed == false)
        try
        {
          TriggeredEvent.WaitOne();

          while (TriggeredCallbacks.TryDequeue(out var callback))
            callback();
        }
        catch (Exception ex)
        {
          LogTo.Error(ex, "An exception was thrown while executing Keyboard HotKey callback");
        }
    }

    [LogToErrorOnException]
    private IntPtr LowLevelKeyboardProc(int    nCode,
                                        IntPtr wParam,
                                        IntPtr lParam)
    {
      if (nCode < 0)
        return Native.CallNextHookEx(_windowsHookHandle,
                                     nCode,
                                     wParam,
                                     lParam);

      var wparamTyped = wParam.ToInt32();

      if (Enum.IsDefined(typeof(KeyboardState),
                         wparamTyped))
      {
        KeyboardState kbState = (KeyboardState)wparamTyped;
        LowLevelKeyboardInputEvent kbEvent = (LowLevelKeyboardInputEvent)Marshal.PtrToStructure(lParam,
                                                                                                typeof(LowLevelKeyboardInputEvent));

        var keyboardPressed = KeyboardPressed;
        if (keyboardPressed != null)
        {
          var args = new KeyboardHookEventArgs(kbEvent, kbState,
                                               Native.GetCtrlPressed(), Native.GetAltPressed(),
                                               Native.GetShiftPressed(), Native.GetMetaPressed());
          keyboardPressed(this, args);

          if (args.Handled)
            return (IntPtr)1;
        }

        if (kbState == KeyboardState.KeyDown || kbState == KeyboardState.SysKeyDown)
        {
          var hk = new HotKey(
            kbEvent.Key,
            Native.GetCtrlPressed(), Native.GetAltPressed(),
            Native.GetShiftPressed(), Native.GetMetaPressed());
          var hkReg = HotKeys.SafeGet(hk);

          if (MainCallback != null && hk.Modifiers != KeyModifiers.None)
            MainCallback(hk);

          if (hkReg == null)
            return Native.CallNextHookEx(_windowsHookHandle,
                                         nCode,
                                         wParam,
                                         lParam);

          bool scopeMatches = true;

          if (hkReg.Scopes != HotKeyScopes.Global)
          {
            var foregroundWdwHandle = Native.GetForegroundWindow();

            if (foregroundWdwHandle == IntPtr.Zero)
            {
              scopeMatches = false;
            }

            else if (hkReg.Scopes == HotKeyScopes.SMBrowser && foregroundWdwHandle != Svc.SM.UI.ElementWdw.Handle)
            {
              scopeMatches = false;
            }

            else if (hkReg.Scopes == HotKeyScopes.SM)
            {
              _ = Native.GetWindowThreadProcessId(foregroundWdwHandle, out var foregroundProcId);

              if (foregroundProcId != Svc.SM.ProcessId)
                scopeMatches = false;
            }
          }

          if (scopeMatches)
          {
            TriggeredCallbacks.Enqueue(hkReg.Callback);
            TriggeredEvent.Set();

            return (IntPtr)1;
          }
        }
      }

      return Native.CallNextHookEx(_windowsHookHandle,
                                   nCode,
                                   wParam,
                                   lParam);
    }


    #endregion




    #region Events

    /// <inheritdoc />
    public event EventHandler<KeyboardHookEventArgs> KeyboardPressed;

    #endregion




    private class RegisteredHotKey
    {
      #region Constructors

      public RegisteredHotKey(Action callback, HotKeyScopes scopes)
      {
        Callback = callback;
        Scopes   = scopes;
      }

      #endregion




      #region Properties & Fields - Public

      public Action       Callback { get; }
      public HotKeyScopes Scopes   { get; }

      #endregion
    }
  }

  /// <summary>The scope to apply for hotkeys</summary>
  [Flags]
  public enum HotKeyScopes
  {
    /// <summary>Restrict hotkey to SM element window</summary>
    SMBrowser = 1,

    /// <summary>Restrict hotkey to the SuperMemo app</summary>
    SM = 0xFFFF,

    /// <summary>USE WITH CARE. Hotkey will be available from anywhere</summary>
    Global = 0xFFFFFFF
  }
}
