// Agent lifecycle inside sm20.exe: handshake with SMA, hook installation, request loop, teardown.
namespace SuperMemoAssistant.Hooks.Agent;

using System.Runtime.InteropServices;
public static unsafe class AgentHost
{
  private static int _started;

  /// <summary>
  ///   Entry point SMA invokes through CreateRemoteThread while SuperMemo's main thread is still suspended. Performs the
  ///   handshake synchronously (so hooks are live before SuperMemo runs), then serves requests on a background thread.
  ///   Returns 0 on success, otherwise a non-zero failure code.
  /// </summary>
  [UnmanagedCallersOnly(EntryPoint = "SmaAgentStart")]
  public static int Start(char* pipeName)
  {
    if (Interlocked.Exchange(ref _started, 1) == 1)
      return 1;

    AgentLink? link = null;
    try
    {
      link = new AgentLink(new string(pipeName));

      var module = Win32.GetModuleHandle(null);
      link.Post(new AgentReady(Environment.ProcessId, module));

      if (link.Receive() is not ConfigureAgent config)
        return 2;

      var dispatcher = new MainThreadDispatcher((uint)config.MainThreadId, new NativeCaller(module, config), link);
      Hooks.Install(module, link, dispatcher, config.WatchedFilePaths);
      link.Post(new AgentConfigured(true, null));

      new Thread(() => Serve(link, dispatcher)) { IsBackground = true, Name = "SMA agent" }.Start();
      return 0;
    }
    catch (Exception ex)
    {
      link?.Post(new AgentConfigured(false, ex.ToString()));
      link?.Dispose();
      return 3;
    }
  }

  private static void Serve(AgentLink link, MainThreadDispatcher dispatcher)
  {
    try
    {
      while (link.Receive() is { } message)
      {
        if (message is ExecuteNative request)
          dispatcher.Enqueue(request);
        else if (message is ShutdownAgent)
          break;
      }
    }
    catch (IOException)
    {
      // SMA closed the pipe: SuperMemo keeps running without the assistant.
    }
    finally
    {
      Hooks.Uninstall();
      link.Dispose();
    }
  }
}
