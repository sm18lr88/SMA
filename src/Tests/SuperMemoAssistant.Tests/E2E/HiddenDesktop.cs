// A never-shown desktop plus a kill-on-close job object: SuperMemo runs invisibly and its whole process tree dies with the test.
namespace SuperMemoAssistant.Tests.E2E;

using System.ComponentModel;
using System.Runtime.InteropServices;

internal sealed partial class HiddenDesktop : IDisposable
{
  private const uint GENERIC_ALL                        = 0x10000000;
  private const int  JobObjectExtendedLimitInformation  = 9;
  private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

  private readonly nint _desktop;
  private readonly nint _job;

  public HiddenDesktop()
  {
    Name     = $"sma-e2e-{Environment.ProcessId}";
    _desktop = CreateDesktopW(Name, 0, 0, 0, GENERIC_ALL, 0);
    if (_desktop == 0)
      throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateDesktop");

    _job = CreateJobObjectW(0, null);
    var limits = new byte[144]; // JOBOBJECT_EXTENDED_LIMIT_INFORMATION (x64); LimitFlags at offset 16
    BitConverter.TryWriteBytes(limits.AsSpan(16), JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE);
    if (!SetInformationJobObject(_job, JobObjectExtendedLimitInformation, limits, limits.Length))
      throw new Win32Exception(Marshal.GetLastWin32Error(), "SetInformationJobObject");
  }

  public string Name { get; }

  /// <summary>Value for STARTUPINFO.lpDesktop.</summary>
  public string StartupDesktop => $"WinSta0\\{Name}";

  public nint Handle => _desktop;

  public void Track(nint processHandle)
  {
    if (!AssignProcessToJobObject(_job, processHandle))
      throw new Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject");
  }

  /// <summary>Kills every process started on this desktop (the whole job), so their files can be deleted.</summary>
  public void TerminateProcesses() => TerminateJobObject(_job, 1);

  public void Dispose()
  {
    TerminateJobObject(_job, 1);
    CloseHandle(_job);
    CloseDesktop(_desktop);
  }

  [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
  private static partial nint CreateDesktopW(string name, nint device, nint devmode, uint flags, uint access, nint attributes);

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool CloseDesktop(nint desktop);

  [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
  private static partial nint CreateJobObjectW(nint attributes, string? name);

  [LibraryImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool SetInformationJobObject(nint job, int infoClass, byte[] info, int length);

  [LibraryImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool AssignProcessToJobObject(nint job, nint process);

  [LibraryImport("kernel32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool TerminateJobObject(nint job, uint exitCode);

  [LibraryImport("kernel32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool CloseHandle(nint handle);
}
