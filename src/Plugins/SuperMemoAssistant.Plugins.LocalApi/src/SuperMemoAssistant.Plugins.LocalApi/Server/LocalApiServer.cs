namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Anotar.Serilog;

/// <summary>
///   Serves the API with <see cref="HttpListener" /> on loopback prefixes only. Requests are handled on the thread
///   pool. Disposing stops the listener and waits for the requests in progress.
/// </summary>
public sealed class LocalApiServer : IAsyncDisposable, IDisposable
{
  private const int AccessDenied     = 5;
  private const int SharingViolation = 32;
  private const int AlreadyExists    = 183;

  private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(5);

  private readonly HttpListener                     _listener;
  private readonly ApiPipeline                      _pipeline;
  private readonly CancellationTokenSource          _stop     = new();
  private readonly ConcurrentDictionary<Task, byte> _inFlight = new();
  private readonly Task                             _loop;
  private          int                              _disposed;

  private LocalApiServer(HttpListener listener, ApiPipeline pipeline, IReadOnlyList<string> prefixes)
  {
    _listener = listener;
    _pipeline = pipeline;
    Prefixes  = prefixes;
    _loop     = Task.Run(AcceptLoopAsync);
  }

  /// <summary>The URL prefixes that the server listens on.</summary>
  public IReadOnlyList<string> Prefixes { get; }

  /// <summary>
  ///   Starts a server. Without administrator rights, Windows can refuse the 127.0.0.1 prefix (it needs a URL
  ///   reservation); the server then listens on localhost only.
  /// </summary>
  /// <param name="options">The settings.</param>
  /// <param name="gateway">The SuperMemo operations.</param>
  /// <param name="server">The running server, or <see langword="null" /> when it could not start.</param>
  /// <param name="error">A message for the user when the server could not start.</param>
  public static bool TryStart(ApiOptions options, ISuperMemoGateway gateway, [NotNullWhen(true)] out LocalApiServer? server, [NotNullWhen(false)] out string? error)
  {
    var both = new[] { Prefix("127.0.0.1", options.Port), Prefix("localhost", options.Port) };
    var listener = TryListen(both, out var errorCode, out var message);

    if (listener == null && errorCode == AccessDenied)
    {
      LogTo.Information("Local API: Windows refused the 127.0.0.1 prefix without a URL reservation; listening on localhost only");
      both     = [Prefix("localhost", options.Port)];
      listener = TryListen(both, out errorCode, out message);
    }

    if (listener == null)
    {
      server = null;
      error = errorCode is SharingViolation or AlreadyExists
        ? string.Create(CultureInfo.InvariantCulture, $"Port {options.Port} is already in use by another program. Choose another port in the Local API settings.")
        : string.Create(CultureInfo.InvariantCulture, $"The local API could not start on port {options.Port}: {message}");
      return false;
    }

    server = new LocalApiServer(listener, new ApiPipeline(options, gateway), both);
    error  = null;
    return true;
  }

  /// <inheritdoc />
  public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1)
      return;

    await _stop.CancelAsync().ConfigureAwait(false);
    _listener.Stop();
    await _loop.ConfigureAwait(false);

    try
    {
      await Task.WhenAll(_inFlight.Keys).WaitAsync(DrainTimeout).ConfigureAwait(false);
    }
    catch (TimeoutException)
    {
      LogTo.Warning("Local API: {Count} requests did not end within {Seconds} s of the stop", _inFlight.Count, DrainTimeout.TotalSeconds);
    }

    _listener.Close();
    _stop.Dispose();
  }

  private static string Prefix(string host, int port) => string.Create(CultureInfo.InvariantCulture, $"http://{host}:{port}/");

  private static HttpListener? TryListen(IEnumerable<string> prefixes, out int errorCode, out string message)
  {
    var listener = new HttpListener { IgnoreWriteExceptions = true };
    foreach (var prefix in prefixes)
      listener.Prefixes.Add(prefix);

    try
    {
      listener.Start();
      errorCode = 0;
      message   = string.Empty;
      return listener;
    }
    catch (HttpListenerException ex)
    {
      listener.Close();
      errorCode = ex.ErrorCode;
      message   = ex.Message;
      return null;
    }
  }

  private async Task AcceptLoopAsync()
  {
    while (_stop.IsCancellationRequested == false)
    {
      HttpListenerContext context;
      try
      {
        context = await _listener.GetContextAsync().ConfigureAwait(false);
      }
      catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
      {
        return;
      }

      var task = HandleAsync(context);
      _inFlight[task] = 0;
      _ = task.ContinueWith(t => _inFlight.TryRemove(t, out _), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
  }

  private async Task HandleAsync(HttpListenerContext context)
  {
    var watch    = Stopwatch.StartNew();
    var request  = context.Request;
    var response = await _pipeline.HandleAsync(ToApiRequest(request), _stop.Token).ConfigureAwait(false);

    try
    {
      await HttpResponseWriter.WriteAsync(context.Response, response).ConfigureAwait(false);
    }
    catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
    {
      LogTo.Debug("Local API: the client closed the connection before the response was sent: {Message}", ex.Message);
    }

    LogTo.Debug("Local API: {Method} {Path} -> {Status} in {Elapsed} ms",
                request.HttpMethod, request.Url?.AbsolutePath, response.StatusCode, watch.ElapsedMilliseconds);
  }

  private static ApiRequest ToApiRequest(HttpListenerRequest request) => new()
  {
    Method                     = request.HttpMethod,
    Path                       = request.Url?.AbsolutePath ?? "/",
    FromLoopback               = request.RemoteEndPoint is { } remote && IPAddress.IsLoopback(remote.Address),
    Host                       = request.Headers["Host"],
    Origin                     = request.Headers["Origin"],
    Authorization              = request.Headers["Authorization"],
    ContentType                = request.ContentType,
    ContentLength              = request.ContentLength64,
    AccessControlRequestMethod = request.Headers["Access-Control-Request-Method"],
    RequestsPrivateNetwork     = string.Equals(request.Headers["Access-Control-Request-Private-Network"], "true", StringComparison.OrdinalIgnoreCase),
    Body                       = request.InputStream,
  };
}
