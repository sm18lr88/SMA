// Answers SuperMemo's start-up Yes/No prompt ("rename the other SuperMemo install?") with No, on the hidden desktop.
namespace SuperMemoAssistant.Tests.E2E;

using System.Runtime.InteropServices;

internal static class StartupPromptAnswerer
{
  private const int WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, VK_ESCAPE = 0x1B;
  private const int MaxAttemptsPerDialog = 10;

  private delegate bool EnumProc(nint hwnd, nint lParam);

  /// <summary>
  ///   Polls on a thread bound to <paramref name="desktop" /> until <paramref name="stop" />. Watches dialogs of
  ///   <paramref name="processId" />, or of every process on the desktop when it is 0.
  /// </summary>
  public static Thread Start(nint desktop, int processId, CancellationToken stop, List<string> events)
  {
    var thread = new Thread(() =>
    {
      SetThreadDesktop(desktop);
      var attempts = new Dictionary<nint, int>();
      try
      {
        while (!stop.IsCancellationRequested)
        {
          foreach (var dialog in Windows(desktop, processId).Where(h => ClassName(h) == "TMsgDialog"))
          {
            var attempt = attempts[dialog] = attempts.GetValueOrDefault(dialog) + 1;
            if (attempt > MaxAttemptsPerDialog)
              continue;

            // SuperMemo's toolbar buttons ignore posted clicks; the dialog's keyboard accelerator for "No" works.
            var key = attempt % 3 == 0 ? VK_ESCAPE : 'N';
            PostMessageW(dialog, WM_KEYDOWN, key, 0);
            PostMessageW(dialog, WM_KEYUP, key, 0);

            lock (events)
              events.Add($"{DateTime.Now:HH:mm:ss.fff} answered a SuperMemo prompt (attempt {attempt})");
          }

          stop.WaitHandle.WaitOne(1000);
        }
      }
      catch (ObjectDisposedException)
      {
        // The test finished and disposed its token while this thread was polling.
      }
    }) { IsBackground = true, Name = "SuperMemo prompt answerer" };

    thread.Start();
    return thread;
  }

  /// <summary>Visible top-level windows on <paramref name="desktop" />, of one process or (0) of all processes.</summary>
  public static List<nint> Windows(nint desktop, int processId)
  {
    var found = new List<nint>();
    EnumDesktopWindows(desktop, (h, _) =>
    {
      GetWindowThreadProcessId(h, out var owner);
      if ((processId == 0 || owner == processId) && IsWindowVisible(h))
        found.Add(h);
      return true;
    }, 0);
    return found;
  }

  /// <summary>Window class and owning process, e.g. "TElWind (sm20)".</summary>
  public static string Describe(nint hwnd)
  {
    GetWindowThreadProcessId(hwnd, out var owner);
    string process;
    try
    {
      using var p = System.Diagnostics.Process.GetProcessById(owner);
      process = p.ProcessName;
    }
    catch (ArgumentException)
    {
      process = "exited";
    }

    return $"{ClassName(hwnd)} ({process})";
  }

  public static string ClassName(nint hwnd)
  {
    var buffer = new char[256];
    var length = GetClassNameW(hwnd, buffer, buffer.Length);
    return new string(buffer, 0, length);
  }

  [DllImport("user32.dll")] private static extern bool EnumDesktopWindows(nint desktop, EnumProc proc, nint lParam);
  [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out int processId);
  [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassNameW(nint hwnd, char[] name, int max);
  [DllImport("user32.dll")] private static extern bool SetThreadDesktop(nint desktop);
  [DllImport("user32.dll")] private static extern bool PostMessageW(nint hwnd, int msg, nint wParam, nint lParam);
}
