// Pipe connection to SMA: ordered asynchronous sends (hook threads never block on I/O) and a blocking receive.
namespace SuperMemoAssistant.Hooks.Agent;

using System.Collections.Concurrent;
using System.IO.Pipes;

internal sealed class AgentLink : IDisposable
{
  private readonly NamedPipeClientStream            _pipe;
  private readonly BlockingCollection<AgentMessage> _outbox = new();
  private readonly Thread                           _sender;

  public AgentLink(string pipeName)
  {
    // Overlapped handle: a synchronous one serialises a blocked Read with Write, which would stall every reply.
    _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    _pipe.Connect(10_000);

    _sender = new Thread(SendLoop) { IsBackground = true, Name = "SMA agent sender" };
    _sender.Start();
  }

  public void Post(AgentMessage message)
  {
    if (!_outbox.IsAddingCompleted)
      _outbox.TryAdd(message);
  }

  public AgentMessage? Receive() => AgentProtocol.Read(_pipe);

  public void Log(AgentLogLevel level, string message) => Post(new AgentLog(level, message));

  public void Dispose()
  {
    _outbox.CompleteAdding();
    _sender.Join(2_000);
    _pipe.Dispose();
  }

  private void SendLoop()
  {
    try
    {
      foreach (var message in _outbox.GetConsumingEnumerable())
      {
        var frame = AgentProtocol.Encode(message);
        _pipe.Write(frame, 0, frame.Length);
        _pipe.Flush();
      }
    }
    catch (IOException)
    {
      _outbox.CompleteAdding(); // SMA went away; Receive() will end the agent.
    }
  }
}
