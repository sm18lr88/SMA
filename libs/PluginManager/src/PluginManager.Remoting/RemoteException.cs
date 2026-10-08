// Exceptions surfaced by the RPC layer.
namespace PluginManager.Remoting;

/// <summary>An exception thrown by the remote side of a call. Carries the original type name and stack trace.</summary>
public sealed class RemoteException(string remoteType, string message, string? remoteStackTrace)
  : Exception($"{remoteType}: {message}")
{
  public string RemoteType { get; } = remoteType;

  public string? RemoteStackTrace { get; } = remoteStackTrace;

  public override string? StackTrace =>
    RemoteStackTrace is null ? base.StackTrace : RemoteStackTrace + Environment.NewLine + "--- remote boundary ---" + Environment.NewLine + base.StackTrace;
}

/// <summary>The connection to the remote endpoint is closed or broken.</summary>
public sealed class RemotingException(string message, Exception? inner = null) : Exception(message, inner);
