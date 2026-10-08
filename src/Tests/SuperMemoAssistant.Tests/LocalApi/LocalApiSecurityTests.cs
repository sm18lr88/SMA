// The Local API security model over real HTTP: token, Host header, Origin header, CORS, and the loopback-only binding.
namespace SuperMemoAssistant.Tests.LocalApi;

using System.Net;
using System.Net.Http;
using SuperMemoAssistant.Plugins.LocalApi.Server;
using Xunit;

public sealed class LocalApiSecurityTests : IAsyncDisposable
{
  private const string ExtensionOrigin = "chrome-extension://abcdefghijklmnopabcdefghijklmnop";

  private readonly LocalApiHarness _api = LocalApiHarness.Create();

  public ValueTask DisposeAsync() => _api.DisposeAsync();

  [Fact]
  public void Server_ListensOnlyOnLoopbackPrefixes()
  {
    Assert.NotEmpty(_api.Server.Prefixes);
    Assert.All(_api.Server.Prefixes, p => Assert.Matches(@"^http://(127\.0\.0\.1|localhost):\d+/$", p));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("wrong-token")]
  [InlineData("")]
  public async Task MissingOrWrongToken_Is401(string? token)
  {
    using var response = await _api.SendAsync(LocalApiHarness.Request(HttpMethod.Get, "/api/v1/status", token: token));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Contains("Authorization: Bearer", await LocalApiHarness.ErrorAsync(response));
    Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
  }

  [Fact]
  public async Task WrongHostPort_Is403()
  {
    var request = LocalApiHarness.Request(HttpMethod.Get, "/api/v1/status");
    request.Headers.Host = "localhost:1";

    using var response = await _api.SendAsync(request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Contains("Host header", await LocalApiHarness.ErrorAsync(response));
  }

  [Fact]
  public async Task RebindingHostName_IsRefused()
  {
    // http.sys refuses an unknown host name with 400 before the pipeline sees it; the pipeline would answer 403.
    var request = LocalApiHarness.Request(HttpMethod.Get, "/api/v1/status");
    request.Headers.Host = $"attacker.example:{_api.Port}";

    using var response = await _api.SendAsync(request);

    Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Forbidden });
  }

  [Fact]
  public async Task WebOrigin_Is403_EvenWithAValidToken()
  {
    using var response = await _api.SendAsync(LocalApiHarness.Request(HttpMethod.Get, "/api/v1/status", origin: "https://evil.example"));

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
  }

  [Fact]
  public async Task WebOrigin_PreflightIs403()
  {
    using var response = await _api.SendAsync(Preflight("https://evil.example"));

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
  }

  [Fact]
  public async Task ExtensionOrigin_PreflightIsAllowed_WithTheExactOriginEchoed()
  {
    var request = Preflight(ExtensionOrigin);
    request.Headers.TryAddWithoutValidation("Access-Control-Request-Private-Network", "true");

    using var response = await _api.SendAsync(request);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal(ExtensionOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    Assert.Contains("Authorization", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Headers")));
    Assert.Contains("POST", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Methods")));
    Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Private-Network")));
  }

  [Theory]
  [InlineData(ExtensionOrigin)]
  [InlineData("moz-extension://0b2f6b1e-6b8a-4c43-9b8e-2d1b4f0d1a77")]
  [InlineData(LocalApiHarness.AllowedOrigin)]
  public async Task AllowedOrigin_GetsTheOriginEchoed_NeverAStar(string origin)
  {
    using var response = await _api.SendAsync(LocalApiHarness.Request(HttpMethod.Get, "/api/v1/status", origin: origin));

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
  }

  [Fact]
  public async Task AllowedOrigin_StillNeedsTheToken()
  {
    using var response = await _api.SendAsync(LocalApiHarness.Request(HttpMethod.Get, "/api/v1/status", token: null, origin: ExtensionOrigin));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal(ExtensionOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
  }

  [Fact]
  public async Task RequestWithoutOrigin_HasNoCorsHeader()
  {
    using var response = await _api.SendAsync(HttpMethod.Get, "/api/v1/status");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
  }

  [Fact]
  public async Task Pipeline_RefusesANonLoopbackConnection()
  {
    var pipeline = new ApiPipeline(_api.Options, new FakeGateway());
    var request = new ApiRequest
    {
      Method        = "GET",
      Path          = "/api/v1/status",
      FromLoopback  = false,
      Host          = $"localhost:{_api.Port}",
      Authorization = "Bearer " + LocalApiHarness.Token,
    };

    var response = await pipeline.HandleAsync(request, TestContext.Current.CancellationToken);

    Assert.Equal(403, response.StatusCode);
  }

  [Fact]
  public async Task Dispose_StopsTheServer_AndFreesThePort()
  {
    var harness = LocalApiHarness.Create();
    var options = harness.Options;
    await harness.DisposeAsync();

    Assert.True(LocalApiServer.TryStart(options, new FakeGateway(), out var again, out var error), error);
    await again.DisposeAsync();
  }

  [Fact]
  public void PortInUse_IsReportedWithAMessage_NotThrown()
  {
    Assert.False(LocalApiServer.TryStart(_api.Options, new FakeGateway(), out var second, out var error));
    Assert.Null(second);
    Assert.Contains("already in use", error);
  }

  private static HttpRequestMessage Preflight(string origin)
  {
    var request = LocalApiHarness.Request(HttpMethod.Options, "/api/v1/elements", token: null, origin: origin);
    request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
    request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "authorization,content-type");
    return request;
  }
}
