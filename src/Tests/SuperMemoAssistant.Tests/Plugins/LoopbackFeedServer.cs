// Serves a folder over loopback HTTP the way GitHub Pages does: static files only, query strings ignored.
namespace SuperMemoAssistant.Tests.Plugins;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

public sealed class LoopbackFeedServer : IAsyncDisposable
{
  private readonly HttpListener            _listener;
  private readonly CancellationTokenSource _deadline;
  private readonly Task                    _loop;
  private readonly string                  _root;

  private LoopbackFeedServer(HttpListener listener, string root, TimeSpan lifetime, Uri baseUrl)
  {
    _listener = listener;
    _root     = Path.GetFullPath(root);
    BaseUrl   = baseUrl;

    // Stops serving even if a test hangs and never disposes the server.
    _deadline = new CancellationTokenSource(lifetime);
    _deadline.Token.Register(listener.Stop);
    _loop = Task.Run(ServeAsync);
  }

  public Uri BaseUrl { get; }

  /// <summary>Absolute paths of all requests, without query strings.</summary>
  public ConcurrentQueue<string> RequestedPaths { get; } = new();

  public static LoopbackFeedServer Create(string root, TimeSpan lifetime)
  {
    for (var attempt = 0; ; attempt++)
    {
      var probe = new TcpListener(IPAddress.Loopback, 0);
      probe.Start();
      var port = ((IPEndPoint)probe.LocalEndpoint).Port;
      probe.Stop();

      // http.sys accepts a "localhost" prefix without an administrator URL reservation.
      var baseUrl  = new Uri($"http://localhost:{port}/");
      var listener = new HttpListener();
      listener.Prefixes.Add(baseUrl.AbsoluteUri);

      try
      {
        listener.Start();
        return new LoopbackFeedServer(listener, root, lifetime, baseUrl);
      }
      catch (HttpListenerException) when (attempt < 5)
      {
        // Another process took the probed port between Stop and Start.
        listener.Close();
      }
    }
  }

  public async ValueTask DisposeAsync()
  {
    await _deadline.CancelAsync().ConfigureAwait(false);
    _listener.Close();
    await _loop.ConfigureAwait(false);
    _deadline.Dispose();
  }

  private async Task ServeAsync()
  {
    while (_listener.IsListening)
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

      using var response = context.Response;
      var       path     = Uri.UnescapeDataString(context.Request.Url!.AbsolutePath);
      RequestedPaths.Enqueue(path);

      var file = Path.GetFullPath(Path.Combine(_root, path.TrimStart('/')));
      if (!file.StartsWith(_root, StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
      {
        response.StatusCode = 404;
        continue;
      }

      response.ContentType = file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "application/octet-stream";
      var bytes = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
      response.ContentLength64 = bytes.Length;

      try
      {
        await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
      }
      catch (HttpListenerException)
      {
        // The client closed the connection; NuGet does this when it cancels a request.
      }
    }
  }
}
