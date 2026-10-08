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
// Created On:   2020/03/29 00:21
// Modified On:  2020/04/07 05:29
// Modified By:  Alexis

#endregion




// ReSharper disable InconsistentNaming

// ReSharper disable IdentifierTypo
namespace SuperMemoAssistant.Sys.IO.Devices
{
  using System;
  using System.Runtime.InteropServices;
  using System.Windows.Input;

  /// <summary>Class for messaging and key presses</summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
                                                   Justification = "Windows naming")]
  public static class Native
  {
    #region Constants & Statics

    /// <summary>The windows LowLevel keyboard hook key</summary>
    internal const int WH_KEYBOARD_LL = 13;

    /// <summary>Maps a virtual key to a key code with specified keyboard.</summary>
    internal const uint MAPVK_VK_TO_VSC_EX = 0x04;

    /// <summary>Code for if the key is pressed.</summary>
    internal const ushort KEY_PRESSED = 0xF000;

    #endregion




    #region Methods

    [DllImport("user32.dll")]
    internal static extern ushort GetKeyState(int nVirtKey);

    //[return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern int SendMessage(IntPtr hWnd,
                                           int    wMsg,
                                           uint   wParam,
                                           uint   lParam);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool PostMessage(IntPtr hWnd,
                                            int    Msg,
                                            uint   wParam,
                                            uint   lParam);

    [DllImport("user32.dll")]
    internal static extern uint MapVirtualKey(uint uCode,
                                              uint uMapType);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr hWnd,
                                               int    id,
                                               uint   fsModifiers,
                                               uint   vk);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd,
                                                 int    id);

    /// <summary>
    ///   Retrieves a handle to the foreground window (the window with which the user is currently working). The system assigns
    ///   a slightly higher priority to the thread that creates the foreground window than it does to other threads.
    /// </summary>
    /// <returns>
    ///   The return value is a handle to the foreground window. The foreground window can be NULL in certain circumstances,
    ///   such as when a window is losing activation.
    /// </returns>
    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    /// <summary>
    ///   Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the
    ///   process that created the window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="lpdwProcessId">
    ///   [Out] A pointer to a variable that receives the process identifier. If this parameter is not <c>NULL</c>,
    ///   <see cref="GetWindowThreadProcessId" /> copies the identifier of the process to the variable; otherwise, it does not.
    /// </param>
    /// <returns>The return value is the identifier of the thread that created the window.</returns>
    [DllImport("user32.dll")]
    internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    /// <summary>Returns the handle for the given process's module name</summary>
    /// <param name="lpModuleName"></param>
    /// <returns></returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPTStr)][In] string lpModuleName);

    /// <summary>
    ///   The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain. You would install a
    ///   hook procedure to monitor the system for certain types of events. These events are associated either with a specific
    ///   thread or with all threads in the same desktop as the calling thread.
    /// </summary>
    /// <param name="idHook">hook type</param>
    /// <param name="lpfn">hook procedure</param>
    /// <param name="hMod">handle to application instance</param>
    /// <param name="dwThreadId">thread identifier</param>
    /// <returns>If the function succeeds, the return value is the handle to the hook procedure.</returns>
    [DllImport("user32.dll",
               CharSet      = CharSet.Auto,
               SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int                 idHook,
                                                   KeyboardHookHandler lpfn,
                                                   IntPtr              hMod,
                                                   int                 dwThreadId);

    /// <summary>
    ///   The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
    /// </summary>
    /// <param name="hHook">handle to hook procedure</param>
    /// <returns>If the function succeeds, the return value is true.</returns>
    [DllImport("user32.dll",
               CharSet      = CharSet.Auto,
               SetLastError = true)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hHook);

    /// <summary>
    ///   The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain. A hook
    ///   procedure can call this function either before or after processing the hook information.
    /// </summary>
    /// <param name="hHook">handle to current hook</param>
    /// <param name="code">hook code passed to hook procedure</param>
    /// <param name="wParam">value passed to hook procedure</param>
    /// <param name="lParam">value passed to hook procedure</param>
    /// <returns>If the function succeeds, the return value is true.</returns>
    [DllImport("user32.dll",
               CharSet      = CharSet.Auto,
               SetLastError = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hHook,
                                                 int    code,
                                                 IntPtr wParam,
                                                 IntPtr lParam);

    [DllImport("user32.dll",
               CharSet = CharSet.Auto)]
    internal static extern short GetKeyState(System.Windows.Forms.Keys nVirtKey);

    public static bool GetCapslock()
    {
      return Convert.ToBoolean(GetKeyState(System.Windows.Forms.Keys.CapsLock)) & true;
    }

    public static bool GetNumlock()
    {
      return Convert.ToBoolean(GetKeyState(System.Windows.Forms.Keys.NumLock)) & true;
    }

    public static bool GetScrollLock()
    {
      return Convert.ToBoolean(GetKeyState(System.Windows.Forms.Keys.Scroll)) & true;
    }

    public static bool GetCtrlPressed()
    {
      int state = GetKeyState(System.Windows.Forms.Keys.ControlKey);
      if (state > 1 || state < -1) return true;

      return false;
    }

    public static bool GetAltPressed()
    {
      int state = GetKeyState(System.Windows.Forms.Keys.Menu);
      if (state > 1 || state < -1) return true;

      return false;
    }

    public static bool GetShiftPressed()
    {
      int state = GetKeyState(System.Windows.Forms.Keys.ShiftKey);
      if (state > 1 || state < -1) return true;

      return false;
    }

    public static bool GetMetaPressed()
    {
      int state = GetKeyState(System.Windows.Forms.Keys.LWin);
      if (state > 1 || state < -1) return true;

      state = GetKeyState(System.Windows.Forms.Keys.RWin);
      if (state > 1 || state < -1) return true;

      return false;
    }

    public static uint GetScanCode(VKey vkey)
    {
      //uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_CHAR);
      return MapVirtualKey((uint)vkey, MAPVK_VK_TO_VSC_EX);
    }

    public static uint GetLParam(int x,
                                 int y)
    {
      return (uint)((y << 16) | (x & 0xFFFF));
    }

    public static uint GetLParam(Int16 repeatCount,
                                 VKey  vkey,
                                 byte  extended,
                                 byte  contextCode,
                                 byte  previousState,
                                 byte  transitionState)
    {
      var  lParam   = (uint)repeatCount;
      uint scanCode = GetScanCode(vkey);

      lParam += scanCode * 0x10000;
      lParam += (uint)(extended            * 0x1000000);
      lParam += (uint)(contextCode * 2     * 0x10000000);
      lParam += (uint)(previousState * 4   * 0x10000000);
      lParam += (uint)(transitionState * 8 * 0x10000000);

      return lParam;
    }

    #endregion



    /// <summary>
    /// Windows keyboard hook callback delegate
    /// </summary>
    /// <param name="nCode"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    public delegate IntPtr KeyboardHookHandler(int    nCode,
                                               IntPtr wParam,
                                               IntPtr lParam);
  }

  /// <summary>Windows LL keyboard input structure</summary>
  [StructLayout(LayoutKind.Sequential)]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
                                                   Justification = "Don't use")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>")]
  public struct LowLevelKeyboardInputEvent
  {
    /// <summary>A virtual-key code. The code must be a value in the range 1 to 254.</summary>
    public readonly int VirtualCode;

    /// <summary>A hardware scan code for the key.</summary>
    public readonly int HardwareScanCode;

    /// <summary>
    ///   The extended-key flag, event-injected Flags, context code, and transition-state flag. This member is specified as
    ///   follows. An application can use the following values to test the keystroke Flags. Testing LLKHF_INJECTED (bit 4) will
    ///   tell you whether the event was injected. If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you
    ///   whether or not the event was injected from a process running at lower integrity level.
    /// </summary>
    public readonly int Flags;

    /// <summary>
    ///   The time stamp stamp for this message, equivalent to what GetMessageTime would return for this message.
    /// </summary>
    public readonly int TimeStamp;

    /// <summary>Additional information associated with the message.</summary>
    public readonly IntPtr AdditionalInformation;

    /// <summary>Converts C/C++ Virtual Key code to C# Windows key</summary>
    public Key Key => KeyInterop.KeyFromVirtualKey(VirtualCode);
  }

  /// <summary>The keyboard button state</summary>
  public enum KeyboardState
  {
    /// <summary>Key is down</summary>
    KeyDown = 0x0100,

    /// <summary>Key is up</summary>
    KeyUp = 0x0101,

    /// <summary>Key is down with alt</summary>
    SysKeyDown = 0x0104,

    /// <summary>Key is up with alt</summary>
    SysKeyUp = 0x0105
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
                                                   Justification = "MS naming convention")]
  internal enum Message
  {
    KEY_DOWN      = 0x100, //Key down
    KEY_UP        = 0x101, //Key Up
    CHAR          = 0x102, //The character being pressed
    SYSKEY_DOWN   = 0x104, //An Alt/ctrl/shift + key down message
    SYSKEY_UP     = 0x105, //An Alt/Ctrl/Shift + Key up Message
    SYSCHAR       = 0x106, //An Alt/Ctrl/Shift + Key character Message
    MOUSEMOVE     = 0x200,
    LBUTTONDOWN   = 0x201, //Left mousebutton down 
    LBUTTONUP     = 0x202, //Left mousebutton up 
    LBUTTONDBLCLK = 0x203, //Left mousebutton doubleclick 
    RBUTTONDOWN   = 0x204, //Right mousebutton down 
    RBUTTONUP     = 0x205, //Right mousebutton up 
    RBUTTONDBLCLK = 0x206, //Right mousebutton doubleclick

    /// <summary>Middle mouse button down</summary>
    MBUTTONDOWN = 0x207,

    /// <summary>Middle mouse button up</summary>
    MBUTTONUP = 0x208,

    HOTKEY = 0x312,
  }
}
