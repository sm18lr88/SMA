// Awaitable results that cross process boundaries: a by-value handle plus a by-reference completion source.
namespace SuperMemoAssistant.Sys.Remoting
{
  using System;
  using System.Threading.Tasks;
  using PluginManager.Interop.Sys;

  /// <summary>Completion notifications for a task living in another process. Implemented by SMA; do not implement.</summary>
  public interface IRemoteTaskSource
  {
    void OnCompleted(Action<Exception> callback);
  }

  /// <inheritdoc cref="IRemoteTaskSource" />
  public interface IRemoteTaskSource<T>
  {
    void OnCompleted(Action<T, Exception> callback);
  }

  /// <summary>
  ///   A <see cref="Task" /> that can be returned from a remote call. Await it directly (<c>await remoteTask</c>) or call
  ///   <see cref="AsTask" />. Implicitly converts from <see cref="Task" />.
  /// </summary>
  [Serializable]
  public sealed class RemoteTask
  {
    private readonly IRemoteTaskSource _source;

    [NonSerialized]
    private readonly Task _local;

    public RemoteTask(Task task, Action<Exception> onExceptionHandler = null)
    {
      _local  = task;
      _source = new Source(task, onExceptionHandler);
    }

    /// <summary>The task in this process: the original when local, otherwise one completed by the remote notification.</summary>
    public Task AsTask()
    {
      if (_local is not null)
        return _local;

      var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      _source.OnCompleted(ex =>
      {
        if (ex is null) tcs.TrySetResult();
        else tcs.TrySetException(ex);
      });
      return tcs.Task;
    }

    public static implicit operator RemoteTask(Task task) => new(task);

    private sealed class Source(Task task, Action<Exception> onException) : PerpetualMarshalByRefObject, IRemoteTaskSource
    {
      public void OnCompleted(Action<Exception> callback) =>
        _ = task.ContinueWith(t => callback(RemoteTaskEx.Unwrap(t, onException)), TaskScheduler.Default);
    }
  }

  /// <inheritdoc cref="RemoteTask" />
  [Serializable]
  public sealed class RemoteTask<T>
  {
    private readonly IRemoteTaskSource<T> _source;

    [NonSerialized]
    private readonly Task<T> _local;

    public RemoteTask(Task<T> task, Action<Exception> onExceptionHandler = null)
    {
      _local  = task;
      _source = new Source(task, onExceptionHandler);
    }

    /// <inheritdoc cref="RemoteTask.AsTask" />
    public Task<T> AsTask()
    {
      if (_local is not null)
        return _local;

      var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      _source.OnCompleted((result, ex) =>
      {
        if (ex is null) tcs.TrySetResult(result);
        else tcs.TrySetException(ex);
      });
      return tcs.Task;
    }

    public static implicit operator RemoteTask<T>(Task<T> task) => new(task);

    private sealed class Source(Task<T> task, Action<Exception> onException) : PerpetualMarshalByRefObject, IRemoteTaskSource<T>
    {
      public void OnCompleted(Action<T, Exception> callback) =>
        _ = task.ContinueWith(t =>
        {
          var ex = RemoteTaskEx.Unwrap(t, onException);
          callback(ex is null ? t.Result : default, ex);
        }, TaskScheduler.Default);
    }
  }
}
