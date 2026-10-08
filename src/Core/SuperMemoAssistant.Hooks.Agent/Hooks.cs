// Detours installed in sm20.exe's IAT: collection file I/O forwarding and the main-thread dispatch point.
namespace SuperMemoAssistant.Hooks.Agent;

using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

internal static unsafe class Hooks
{
  private static AgentLink?                         _link;
  private static MainThreadDispatcher?              _dispatcher;
  private static HashSet<string>                    _watchedPaths = new(StringComparer.OrdinalIgnoreCase);
  private static readonly ConcurrentDictionary<nint, byte> WatchedHandles = new();
  private static readonly List<(nint Replacement, nint Original)> Installed = new();
  private static nint _module;

  private static delegate* unmanaged<char*, uint, uint, nint, uint, uint, nint, nint> _createFileW;
  private static delegate* unmanaged<nint, int, int*, uint, uint>                    _setFilePointer;
  private static delegate* unmanaged<nint, byte*, uint, uint*, nint, int>            _writeFile;
  private static delegate* unmanaged<nint, int>                                      _closeHandle;
  private static delegate* unmanaged<void*, nint, uint, uint, uint, int>             _peekMessageW;
  private static delegate* unmanaged<nint, int, int>                                 _showWindow;
  private static int _firstShowNormalSuppressed;

  public static void Install(nint module, AgentLink link, MainThreadDispatcher dispatcher, IEnumerable<string> watchedPaths)
  {
    _module       = module;
    _link         = link;
    _dispatcher   = dispatcher;
    _watchedPaths = new HashSet<string>(watchedPaths, StringComparer.OrdinalIgnoreCase);

    _createFileW    = (delegate* unmanaged<char*, uint, uint, nint, uint, uint, nint, nint>)Patch("kernel32.dll", "CreateFileW", (nint)(delegate* unmanaged<char*, uint, uint, nint, uint, uint, nint, nint>)&CreateFileW);
    _setFilePointer = (delegate* unmanaged<nint, int, int*, uint, uint>)Patch("kernel32.dll", "SetFilePointer", (nint)(delegate* unmanaged<nint, int, int*, uint, uint>)&SetFilePointer);
    _writeFile      = (delegate* unmanaged<nint, byte*, uint, uint*, nint, int>)Patch("kernel32.dll", "WriteFile", (nint)(delegate* unmanaged<nint, byte*, uint, uint*, nint, int>)&WriteFile);
    _closeHandle    = (delegate* unmanaged<nint, int>)Patch("kernel32.dll", "CloseHandle", (nint)(delegate* unmanaged<nint, int>)&CloseHandle);
    _peekMessageW   = (delegate* unmanaged<void*, nint, uint, uint, uint, int>)Patch("user32.dll", "PeekMessageW", (nint)(delegate* unmanaged<void*, nint, uint, uint, uint, int>)&PeekMessageW);
    _showWindow     = (delegate* unmanaged<nint, int, int>)Patch("user32.dll", "ShowWindow", (nint)(delegate* unmanaged<nint, int, int>)&ShowWindow);
  }

  public static void Uninstall()
  {
    lock (Installed)
    {
      foreach (var (replacement, original) in Installed)
        IatPatcher.Restore(_module, replacement, original);
      Installed.Clear();
    }
  }

  private static nint Patch(string dll, string function, nint replacement)
  {
    var original = IatPatcher.Patch(_module, dll, function, replacement);
    if (original == 0)
      throw new InvalidOperationException($"sm20.exe does not import {dll}!{function}.");

    lock (Installed)
      Installed.Add((replacement, original));
    return original;
  }

  [UnmanagedCallersOnly]
  private static nint CreateFileW(char* name, uint access, uint share, nint security, uint disposition, uint flags, nint template)
  {
    var handle = _createFileW(name, access, share, security, disposition, flags, template);
    Guard(() =>
    {
      var path = new string(name);
      if (handle != -1 && _watchedPaths.Contains(path))
      {
        WatchedHandles[handle] = 0;
        _link!.Post(new FileCreated(path, handle));
      }
    });
    return handle;
  }

  [UnmanagedCallersOnly]
  private static uint SetFilePointer(nint handle, int distance, int* distanceHigh, uint method)
  {
    var position = _setFilePointer(handle, distance, distanceHigh, method);
    if (WatchedHandles.ContainsKey(handle))
      Guard(() => _link!.Post(new FileSeeked(handle, position)));
    return position;
  }

  [UnmanagedCallersOnly]
  private static int WriteFile(nint handle, byte* buffer, uint count, uint* written, nint overlapped)
  {
    if (WatchedHandles.ContainsKey(handle))
      Guard(() => _link!.Post(new FileWritten(handle, new ReadOnlySpan<byte>(buffer, (int)count).ToArray())));
    return _writeFile(handle, buffer, count, written, overlapped);
  }

  [UnmanagedCallersOnly]
  private static int CloseHandle(nint handle)
  {
    if (WatchedHandles.TryRemove(handle, out _))
      Guard(() => _link!.Post(new FileClosed(handle)));
    return _closeHandle(handle);
  }

  [UnmanagedCallersOnly]
  private static int PeekMessageW(void* msg, nint hwnd, uint min, uint max, uint remove)
  {
    if ((remove & Win32.PM_REMOVE) != 0)
      _dispatcher?.RunPending();
    return _peekMessageW(msg, hwnd, min, max, remove);
  }

  /// <summary>Swallows SuperMemo's first SW_SHOWNORMAL (parity with the x86 injector), then forwards every call.</summary>
  [UnmanagedCallersOnly]
  private static int ShowWindow(nint hwnd, int command)
  {
    if (command == Win32.SW_SHOWNORMAL && Interlocked.Exchange(ref _firstShowNormalSuppressed, 1) == 0)
      return 1;

    return _showWindow(hwnd, command);
  }

  /// <summary>Exceptions must never unwind into Delphi frames: report them and continue with SuperMemo's original call.</summary>
  [MethodImpl(MethodImplOptions.NoInlining)]
  private static void Guard(Action action)
  {
    try { action(); }
    catch (Exception ex) { _link?.Log(AgentLogLevel.Error, ex.ToString()); }
  }
}
