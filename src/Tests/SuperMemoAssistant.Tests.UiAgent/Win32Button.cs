namespace SuperMemoAssistant.Tests.UiAgent;

using System;
using System.Runtime.InteropServices;

/// <summary>
///   UI Automation refuses to invoke Win32 buttons on a desktop that is not the input desktop. A button click reaches its
///   dialog as WM_COMMAND with BN_CLICKED, so the agent posts that message directly.
/// </summary>
internal static partial class Win32Button
{
  private const uint WM_COMMAND = 0x0111;
  private const int BN_CLICKED = 0;

  public static void Click(IntPtr button)
  {
    var dialog = GetParent(button);
    var id = GetDlgCtrlID(button);
    if (dialog == IntPtr.Zero || id == 0)
      throw new InvalidOperationException("The button has no parent dialog or no control id.");

    if (!PostMessageW(dialog, WM_COMMAND, (IntPtr)((BN_CLICKED << 16) | (id & 0xFFFF)), button))
      throw new InvalidOperationException("PostMessage failed.");
  }

  [LibraryImport("user32.dll")]
  private static partial IntPtr GetParent(IntPtr window);

  [LibraryImport("user32.dll")]
  private static partial int GetDlgCtrlID(IntPtr window);

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool PostMessageW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
