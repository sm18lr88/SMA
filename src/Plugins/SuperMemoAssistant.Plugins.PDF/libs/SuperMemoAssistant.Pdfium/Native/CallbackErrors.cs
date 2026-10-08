namespace SuperMemoAssistant.Pdfium.Native;

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

/// <summary>
///   An exception must not unwind through PDFium's native frames. Callbacks catch it and rethrow it on the current
///   synchronization context, so the application's usual unhandled-exception handling still sees it.
/// </summary>
internal static class CallbackErrors
{
  public static void Report(Exception exception)
  {
    var captured = ExceptionDispatchInfo.Capture(exception);

    if (SynchronizationContext.Current is { } context)
      context.Post(static state => ((ExceptionDispatchInfo)state!).Throw(), captured);
    else
      Environment.FailFast("An exception escaped a PDFium callback.", exception);
  }
}
