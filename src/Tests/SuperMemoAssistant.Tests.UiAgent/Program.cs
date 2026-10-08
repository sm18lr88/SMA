// UI Automation sees only the desktop of its own process. The UI tests therefore run this agent on their hidden desktop
// and send it one JSON command per line over a named pipe. Each command gets one JSON reply line.
namespace SuperMemoAssistant.Tests.UiAgent;

using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;

internal static class Program
{
  [STAThread]
  private static int Main(string[] args)
  {
    if (args.Length != 1)
      return 2;

    using var pipe = new NamedPipeClientStream(".", args[0], PipeDirection.InOut);
    pipe.Connect(TimeSpan.FromSeconds(30));

    using var reader = new StreamReader(pipe);
    using var writer = new StreamWriter(pipe) { AutoFlush = true };
    var commands = new Commands();

    while (reader.ReadLine() is { } line)
    {
      string reply;
      try
      {
        using var request = JsonDocument.Parse(line);
        reply = JsonSerializer.Serialize(new { ok = true, result = commands.Execute(request.RootElement) });
      }
      catch (Exception ex)
      {
        reply = JsonSerializer.Serialize(new { ok = false, error = ex.ToString() });
      }

      writer.WriteLine(reply);
    }

    return 0;
  }
}
