// Cancellation that crosses process boundaries: a by-value token handle backed by a by-reference registration source.
namespace SuperMemoAssistant.Sys.Remoting
{
  using System;
  using System.Threading;
  using PluginManager.Interop.Sys;

  /// <summary>Cancellation notifications from another process. Implemented by SMA; do not implement.</summary>
  public interface IRemoteCancellationSource
  {
    /// <summary>Invokes <paramref name="callback" /> once when cancelled (immediately if already cancelled).</summary>
    void Register(Action callback);
  }

  /// <summary>A <see cref="CancellationToken" /> that can be passed to remote calls. Use <see cref="RemoteCancellationTokenEx.Token" />.</summary>
  [Serializable]
  public sealed class RemoteCancellationToken
  {
    private readonly IRemoteCancellationSource _source;

    [NonSerialized]
    private readonly CancellationToken? _local;

    public RemoteCancellationToken(CancellationToken token)
    {
      _local  = token;
      _source = new Source(token);
    }

    public void Register(Action cancelledCallback) => _source.Register(cancelledCallback);

    internal CancellationToken? Local => _local;

    private sealed class Source(CancellationToken token) : PerpetualMarshalByRefObject, IRemoteCancellationSource
    {
      public void Register(Action callback) => token.Register(callback);
    }
  }

  public static class RemoteCancellationTokenEx
  {
    /// <summary>Returns a token for this process that is cancelled when the remote token is.</summary>
    public static CancellationToken Token(this RemoteCancellationToken remoteToken)
    {
      if (remoteToken.Local is { } local)
        return local;

      var tokenSrc = new CancellationTokenSource();
      remoteToken.Register(tokenSrc.Cancel);
      return tokenSrc.Token;
    }
  }

  /// <summary>Creates <see cref="RemoteCancellationToken" />s; mirrors <see cref="CancellationTokenSource" />.</summary>
  public sealed class RemoteCancellationTokenSource : IDisposable
  {
    private readonly CancellationTokenSource _tokenSrc = new();

    public RemoteCancellationTokenSource() => Token = new RemoteCancellationToken(_tokenSrc.Token);

    public bool IsCancellationRequested => _tokenSrc.IsCancellationRequested;

    public RemoteCancellationToken Token { get; }

    public void Cancel() => _tokenSrc.Cancel();

    public void CancelAfter(int millisecondDelay) => _tokenSrc.CancelAfter(millisecondDelay);

    public void CancelAfter(TimeSpan delay) => _tokenSrc.CancelAfter(delay);

    public void Dispose() => _tokenSrc.Dispose();

    public static implicit operator CancellationTokenSource(RemoteCancellationTokenSource remoteTokenSrc) => remoteTokenSrc._tokenSrc;
  }
}
