// A pipe that goes away under a connection that still counts as open must look like a closed connection, never escape as another exception.
namespace SuperMemoAssistant.Tests.Remoting;

using System.IO.Pipes;
using PluginManager.Remoting;
using Xunit;

public sealed class RpcClosedPipeTests
{
  public interface IPing
  {
    int Ping();
  }

  [Fact]
  public void ACallOnADisposedPipe_FailsWithRemotingException_AndClosesTheConnection()
  {
    using var pipe       = new DisposedOnWritePipe();
    using var connection = new RpcConnection(pipe, root: null, "closed-pipe-test");

    var proxy = connection.GetRoot<IPing>();
    var error = Record.Exception(() => proxy.Ping());

    Assert.IsType<RemotingException>(error);
    Assert.False(connection.IsConnected);
  }

  /// <summary>Reads block until disposal; writes fail as on a pipe that another thread has just disposed.</summary>
  private sealed class DisposedOnWritePipe() : PipeStream(PipeDirection.InOut, 4096)
  {
    private readonly ManualResetEventSlim _disposed = new();

    public override int Read(byte[] buffer, int offset, int count)
    {
      _disposed.Wait();
      return 0;
    }

    public override void Write(ReadOnlySpan<byte> buffer) => throw new ObjectDisposedException(nameof(PipeStream));

    public override void Write(byte[] buffer, int offset, int count) => throw new ObjectDisposedException(nameof(PipeStream));

    public override void Flush() { }

    protected override void Dispose(bool disposing)
    {
      _disposed.Set();
      base.Dispose(disposing);
    }
  }
}
