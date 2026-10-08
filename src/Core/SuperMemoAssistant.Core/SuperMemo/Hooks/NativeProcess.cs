// Win32 process primitives for starting sm20.exe suspended and loading the agent into it.
namespace SuperMemoAssistant.SuperMemo.Hooks
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Runtime.InteropServices;
  using System.Text;

  internal static partial class NativeProcess
  {
    private const uint CREATE_SUSPENDED       = 0x00000004;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint MEM_COMMIT_RESERVE     = 0x00003000;
    private const uint MEM_RELEASE            = 0x00008000;
    private const uint PAGE_READWRITE         = 0x04;

    public readonly record struct StartedProcess(int ProcessId, int MainThreadId, IntPtr ProcessHandle, IntPtr ThreadHandle);

    /// <summary>
    ///   Starts <paramref name="exePath" /> with its main thread suspended, optionally on another desktop (the end-to-end
    ///   tests use a hidden one).
    /// </summary>
    public static unsafe StartedProcess StartSuspended(string                              exePath,
                                                       string                              arguments,
                                                       string                              workingDirectory,
                                                       string                              desktop             = null,
                                                       IReadOnlyDictionary<string, string> environmentOverrides = null)
    {
      var commandLine = new StringBuilder($"\"{exePath}\" {arguments}");
      var desktopName = desktop is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(desktop);
      var environment = environmentOverrides is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(EnvironmentBlock(environmentOverrides));
      var startup     = new STARTUPINFOW { cb = sizeof(STARTUPINFOW), lpDesktop = desktopName };

      try
      {
        if (!CreateProcessW(exePath, commandLine, IntPtr.Zero, IntPtr.Zero, false, CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT,
                            environment, workingDirectory, ref startup, out var info))
          throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not launch {exePath}");

        return new StartedProcess(info.dwProcessId, info.dwThreadId, info.hProcess, info.hThread);
      }
      finally
      {
        Marshal.FreeHGlobal(desktopName);
        Marshal.FreeHGlobal(environment);
      }
    }

    private static string EnvironmentBlock(IReadOnlyDictionary<string, string> overrides)
    {
      var merged = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
        merged[(string)e.Key] = (string)e.Value;
      foreach (var (key, value) in overrides)
        merged[key] = value;

      return string.Concat(merged.Select(kv => $"{kv.Key}={kv.Value}\0")) + "\0";
    }

    public static void Resume(IntPtr threadHandle)
    {
      if (ResumeThread(threadHandle) == uint.MaxValue)
        throw new Win32Exception(Marshal.GetLastWin32Error(), "ResumeThread failed");
    }

    /// <summary>Copies <paramref name="text" /> (UTF-16, null-terminated) into the target and returns its address.</summary>
    public static IntPtr WriteString(IntPtr process, string text)
    {
      var bytes  = Encoding.Unicode.GetBytes(text + '\0');
      var remote = VirtualAllocEx(process, IntPtr.Zero, (UIntPtr)bytes.Length, MEM_COMMIT_RESERVE, PAGE_READWRITE);
      if (remote == IntPtr.Zero || !WriteProcessMemory(process, remote, bytes, (UIntPtr)bytes.Length, out _))
        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not write to the SuperMemo process");

      return remote;
    }

    public static void Free(IntPtr process, IntPtr address) => VirtualFreeEx(process, address, UIntPtr.Zero, MEM_RELEASE);

    /// <summary>Runs <paramref name="function" />(<paramref name="argument" />) on a new remote thread and returns its exit code.</summary>
    public static uint RunRemote(IntPtr process, IntPtr function, IntPtr argument, TimeSpan timeout)
    {
      var thread = CreateRemoteThread(process, IntPtr.Zero, UIntPtr.Zero, function, argument, 0, out _);
      if (thread == IntPtr.Zero)
        throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateRemoteThread failed");

      try
      {
        if (WaitForSingleObject(thread, (uint)timeout.TotalMilliseconds) != 0)
          throw new TimeoutException("The remote thread in SuperMemo did not finish in time.");

        GetExitCodeThread(thread, out var exitCode);
        return exitCode;
      }
      finally
      {
        CloseHandle(thread);
      }
    }

    public static IntPtr Kernel32Export(string name) => GetProcAddress(GetModuleHandleW("kernel32.dll"), name);

    public static void Close(IntPtr handle)
    {
      if (handle != IntPtr.Zero)
        CloseHandle(handle);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFOW
    {
      public int    cb;
      public IntPtr lpReserved, lpDesktop, lpTitle;
      public int    dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
      public short  wShowWindow, cbReserved2;
      public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
      public IntPtr hProcess, hThread;
      public int    dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessW(string app, StringBuilder cmd, IntPtr procAttr, IntPtr threadAttr, bool inherit, uint flags,
                                              IntPtr env, string cwd, ref STARTUPINFOW startup, out PROCESS_INFORMATION info);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint ResumeThread(IntPtr thread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr VirtualAllocEx(IntPtr process, IntPtr address, UIntPtr size, uint type, uint protect);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFreeEx(IntPtr process, IntPtr address, UIntPtr size, uint type);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] buffer, UIntPtr size, out UIntPtr written);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr CreateRemoteThread(IntPtr process, IntPtr attributes, UIntPtr stack, IntPtr start, IntPtr parameter,
                                                     uint flags, out uint threadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetExitCodeThread(IntPtr thread, out uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr handle);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandleW(string name);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr GetProcAddress(IntPtr module, string name);
  }
}
