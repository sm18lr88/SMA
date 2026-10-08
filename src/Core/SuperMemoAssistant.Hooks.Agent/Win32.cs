// Win32 entry points the agent calls directly (its own imports, never routed through sm20.exe's hooked IAT).
namespace SuperMemoAssistant.Hooks.Agent;

using System.Runtime.InteropServices;

internal static partial class Win32
{
  public const uint PM_REMOVE              = 0x0001;
  public const int  SW_SHOWNORMAL          = 1;
  public const uint PAGE_READWRITE         = 0x04;
  public const int  IMAGE_DIRECTORY_IMPORT = 1;

  [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
  public static partial nint GetModuleHandle(string? moduleName);

  [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8)]
  public static partial nint GetProcAddress(nint module, string procName);

  [LibraryImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static partial bool VirtualProtect(nint address, nuint size, uint newProtect, out uint oldProtect);

  [LibraryImport("kernel32.dll")]
  public static partial uint GetCurrentThreadId();

  [LibraryImport("user32.dll", EntryPoint = "PostThreadMessageW", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static partial bool PostThreadMessage(uint threadId, uint msg, nint wParam, nint lParam);

  [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
  public static partial uint RegisterWindowMessage(string name);
}
