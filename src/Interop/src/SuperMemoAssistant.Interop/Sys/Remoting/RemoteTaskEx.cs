// Await/convert helpers for RemoteTask; kept as extension methods for source compatibility with existing plugins.
namespace SuperMemoAssistant.Sys.Remoting
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Threading.Tasks;
  using Anotar.Serilog;

  public static class RemoteTaskEx
  {
    public static Task GetTask(this RemoteTask remoteTask) => remoteTask.AsTask();

    public static Task<T> GetTask<T>(this RemoteTask<T> remoteTask) => remoteTask.AsTask();

    public static TaskAwaiter GetAwaiter(this RemoteTask remoteTask) => remoteTask.AsTask().GetAwaiter();

    public static TaskAwaiter<T> GetAwaiter<T>(this RemoteTask<T> remoteTask) => remoteTask.AsTask().GetAwaiter();

    /// <summary>Blocks until the remote task completes. Prefer <c>await</c>.</summary>
    public static T GetResult<T>(this RemoteTask<T> remoteTask) => remoteTask.AsTask().GetAwaiter().GetResult();

    public static RemoteTask ConfigureRemoteTask(this Task task, Action<Exception> onExceptionHandler) => new(task, onExceptionHandler);

    public static RemoteTask<T> ConfigureRemoteTask<T>(this Task<T> task, Action<Exception> onExceptionHandler) => new(task, onExceptionHandler);

    public static Task WhenAll(this IEnumerable<RemoteTask> remoteTasks) => Task.WhenAll(remoteTasks.Select(rt => rt.AsTask()));

    public static Task<T[]> WhenAll<T>(this IEnumerable<RemoteTask<T>> remoteTasks) => Task.WhenAll(remoteTasks.Select(rt => rt.AsTask()));

    /// <summary>Returns the task's failure (reporting it to <paramref name="onException" /> or the log), or null on success.</summary>
    internal static Exception Unwrap(Task task, Action<Exception> onException)
    {
      Exception ex = task.IsCanceled ? new TaskCanceledException(task) : task.Exception?.InnerExceptions.Count == 1 ? task.Exception.InnerException : task.Exception;
      if (ex is null)
        return null;

      if (onException is not null) onException(ex);
      else LogTo.Warning(ex, "Remote task failed");

      return ex;
    }
  }
}
