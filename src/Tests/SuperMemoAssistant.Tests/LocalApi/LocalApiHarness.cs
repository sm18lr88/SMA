// Starts a real Local API server on a free loopback port with a fake gateway, and an HttpClient that talks to it.
namespace SuperMemoAssistant.Tests.LocalApi;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SuperMemoAssistant.Plugins.LocalApi.Server;
using Xunit;

internal sealed class LocalApiHarness : IAsyncDisposable
{
  public const string Token          = "test-token-for-the-local-api";
  public const string AllowedOrigin  = "https://allowed.example";
  public const double DefaultPriority = 42;

  private LocalApiHarness(LocalApiServer server, ApiOptions options, FakeGateway gateway)
  {
    Server  = server;
    Options = options;
    Gateway = gateway;
    BaseUrl = new Uri(server.Prefixes[0]);
    Client  = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = BaseUrl };
  }

  public LocalApiServer Server  { get; }
  public ApiOptions     Options { get; }
  public FakeGateway    Gateway { get; }
  public Uri            BaseUrl { get; }
  public HttpClient     Client  { get; }
  public int            Port    => Options.Port;

  public static LocalApiHarness Create(FakeGateway? gateway = null, Func<ApiOptions, ApiOptions>? configure = null)
  {
    gateway ??= new FakeGateway();

    for (var attempt = 0; ; attempt++)
    {
      var options = new ApiOptions(FreePort(), Token, [AllowedOrigin], true, DefaultPriority);
      options = configure?.Invoke(options) ?? options;

      if (LocalApiServer.TryStart(options, gateway, out var server, out var error))
        return new LocalApiHarness(server, options, gateway);

      if (attempt >= 5)
        throw new InvalidOperationException(error);
    }
  }

  public static int FreePort()
  {
    var probe = new TcpListener(IPAddress.Loopback, 0);
    probe.Start();
    var port = ((IPEndPoint)probe.LocalEndpoint).Port;
    probe.Stop();
    return port;
  }

  public static HttpRequestMessage Request(HttpMethod method, string path, object? json = null, string? token = Token, string? origin = null)
  {
    var request = new HttpRequestMessage(method, path);

    if (token != null)
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    if (origin != null)
      request.Headers.TryAddWithoutValidation("Origin", origin);

    if (json != null)
      request.Content = new StringContent(JsonSerializer.Serialize(json), Encoding.UTF8, "application/json");

    return request;
  }

  public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
    Client.SendAsync(request, TestContext.Current.CancellationToken);

  public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? json = null) =>
    SendAsync(Request(method, path, json));

  public static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
  {
    var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    return JsonDocument.Parse(text).RootElement.Clone();
  }

  public static async Task<string> ErrorAsync(HttpResponseMessage response) =>
    (await JsonAsync(response)).GetProperty("error").GetString() ?? string.Empty;

  public async ValueTask DisposeAsync()
  {
    Client.Dispose();
    await Server.DisposeAsync();
  }
}
