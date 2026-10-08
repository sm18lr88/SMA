// Public entry points: serve a root object on a named pipe, or connect to one and obtain its proxy.
namespace PluginManager.Remoting;

using System.IO.Pipes;
using System.Security.Cryptography;

public static class RpcEndpoint
{
  /// <summary>Starts accepting connections on <paramref name="pipeName" />; every client sees <paramref name="root" /> as its root object.</summary>
  public static RpcServer Serve(string pipeName, object root) => new(pipeName, root);

  /// <summary>Connects to <paramref name="pipeName" /> on this machine and returns a proxy for the server's root object.</summary>
  public static T Connect<T>(string pipeName, TimeSpan? timeout = null) =>
    Open(pipeName, timeout).GetRoot<T>();

  /// <summary>Connects and returns the connection itself (for lifetime events), without creating the root proxy.</summary>
  public static RpcConnection Open(string pipeName, TimeSpan? timeout = null)
  {
    var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    try
    {
      pipe.Connect((int)(timeout ?? TimeSpan.FromSeconds(30)).TotalMilliseconds);
    }
    catch (TimeoutException ex)
    {
      pipe.Dispose();
      throw new RemotingException($"Could not connect to '{pipeName}'.", ex);
    }

    return new RpcConnection(pipe, root: null, pipeName);
  }

  /// <summary>Random pipe name (letters and digits) that is hard to guess.</summary>
  public static string NewPipeName() =>
    "sma-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}

/// <summary>Listens on a named pipe and serves one root object to each connecting client.</summary>
public sealed class RpcServer : IDisposable
{
  private readonly CancellationTokenSource _stop = new();
  private readonly List<RpcConnection>     _connections = new();
  private readonly object                  _root;

  internal RpcServer(string pipeName, object root)
  {
    PipeName = pipeName;
    _root    = root;
    _ = AcceptLoopAsync();
  }

  public string PipeName { get; }

  public event EventHandler<RpcConnection>? ClientConnected;

  public void Dispose()
  {
    List<RpcConnection> open;
    lock (_connections)
    {
      _stop.Cancel();
      open = _connections.ToList();
    }

    foreach (var c in open)
      c.Dispose();
  }

  private async Task AcceptLoopAsync()
  {
    while (!_stop.IsCancellationRequested)
    {
      var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                                           PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      try
      {
        await pipe.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        await pipe.DisposeAsync().ConfigureAwait(false);
        return;
      }

      var connection = new RpcConnection(pipe, _root, PipeName);
      lock (_connections)
      {
        // Dispose may have run while this client was being accepted: it must not outlive the server.
        if (_stop.IsCancellationRequested)
        {
          connection.Dispose();
          return;
        }

        _connections.Add(connection);
      }

      connection.Closed += (_, _) => { lock (_connections) _connections.Remove(connection); };
      ClientConnected?.Invoke(this, connection);
    }
  }
}
