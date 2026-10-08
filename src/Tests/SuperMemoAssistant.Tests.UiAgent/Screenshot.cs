namespace SuperMemoAssistant.Tests.UiAgent;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>Captures a window through DWM, which also works on a desktop that no monitor shows.</summary>
internal static partial class Screenshot
{
  private const uint PW_RENDERFULLCONTENT = 2;

  public static string Save(IntPtr window, string path)
  {
    if (!GetWindowRect(window, out var rect))
      throw new InvalidOperationException("The window no longer exists.");

    using var bitmap = new Bitmap(Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top), PixelFormat.Format32bppArgb);
    using (var graphics = Graphics.FromImage(bitmap))
    {
      var hdc = graphics.GetHdc();
      try
      {
        if (!PrintWindow(window, hdc, PW_RENDERFULLCONTENT))
          throw new InvalidOperationException("PrintWindow failed.");
      }
      finally
      {
        graphics.ReleaseHdc(hdc);
      }
    }

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    bitmap.Save(path, ImageFormat.Png);
    return path;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct Rect
  {
    public int Left, Top, Right, Bottom;
  }

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool GetWindowRect(IntPtr window, out Rect rect);

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool PrintWindow(IntPtr window, IntPtr hdc, uint flags);
}
