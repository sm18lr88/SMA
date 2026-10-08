namespace SuperMemoAssistant.Tests.UiAgent;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Automation;

/// <summary>
///   Lists the visible top-level windows of the agent's desktop. Win32 treats owned dialogs (such as the Open dialog) as
///   top-level windows, while the UI Automation tree may hang them under an owner that it does not list.
/// </summary>
internal static class DesktopWindows
{
  private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

  public static List<AutomationElement> Visible()
  {
    var handles = new List<IntPtr>();
    EnumWindows((window, _) =>
    {
      if (IsWindowVisible(window))
        handles.Add(window);
      return true;
    }, IntPtr.Zero);

    var windows = new List<AutomationElement>();
    foreach (var handle in handles)
      if (TryFromHandle(handle) is { } window)
        windows.Add(window);

    return windows;
  }

  /// <summary>Returns null for a window that closed between the enumeration and the lookup.</summary>
  private static AutomationElement? TryFromHandle(IntPtr handle)
  {
    try
    {
      return AutomationElement.FromHandle(handle);
    }
    catch (ElementNotAvailableException)
    {
      return null;
    }
  }

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool IsWindowVisible(IntPtr window);
}
