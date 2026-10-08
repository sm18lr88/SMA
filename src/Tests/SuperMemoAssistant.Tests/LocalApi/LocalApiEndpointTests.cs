// The Local API endpoints over real HTTP with a fake gateway: status, element reads, creation, navigation, and errors.
namespace SuperMemoAssistant.Tests.LocalApi;

using System.Net;
using System.Net.Http;
using System.Text;
using SuperMemoAssistant.Plugins.LocalApi.Server;
using Xunit;

public sealed class LocalApiEndpointTests : IAsyncDisposable
{
  private readonly LocalApiHarness _api = LocalApiHarness.Create();

  public ValueTask DisposeAsync() => _api.DisposeAsync();

  [Fact]
  public async Task Status_ReportsVersionsCollectionAndCurrentElement()
  {
    using var response = await _api.SendAsync(HttpMethod.Get, "/api/v1/status");
    var json = await LocalApiHarness.JsonAsync(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("3.1.0-test", json.GetProperty("smaVersion").GetString());
    Assert.Equal(1, json.GetProperty("apiVersion").GetInt32());
    Assert.Equal("Test collection", json.GetProperty("collection").GetString());
    Assert.True(json.GetProperty("superMemoRunning").GetBoolean());
    Assert.Equal(5, json.GetProperty("currentElement").GetProperty("id").GetInt32());
    Assert.Equal("topic", json.GetProperty("currentElement").GetProperty("type").GetString());
  }

  [Fact]
  public async Task CurrentElement_And_ElementById()
  {
    using var current = await _api.SendAsync(HttpMethod.Get, "/api/v1/elements/current");
    using var byId    = await _api.SendAsync(HttpMethod.Get, "/api/v1/elements/1");
    using var missing = await _api.SendAsync(HttpMethod.Get, "/api/v1/elements/999");

    Assert.Equal(1, (await LocalApiHarness.JsonAsync(current)).GetProperty("parentId").GetInt32());
    Assert.Equal(1, (await LocalApiHarness.JsonAsync(byId)).GetProperty("childCount").GetInt32());
    Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    Assert.Contains("999", await LocalApiHarness.ErrorAsync(missing));
  }

  [Fact]
  public async Task CurrentElement_Is404_WhenNoElementIsShown()
  {
    _api.Gateway.CurrentId = null;

    using var response = await _api.SendAsync(HttpMethod.Get, "/api/v1/elements/current");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task CreateTopic_MapsTitleReferencesAndDefaults_AndSanitizesTheHtml()
  {
    using var response = await _api.SendAsync(HttpMethod.Post, "/api/v1/elements", new
    {
      type       = "topic",
      title      = "A web page",
      html       = "<p>Hello <b>world</b></p><script>alert(1)</script>",
      references = new { title = "Page", author = "Ann", link = "https://example.org/a", source = "Example", date = "2026", comment = "c", email = "a@example.org" },
    });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Equal(100, (await LocalApiHarness.JsonAsync(response)).GetProperty("id").GetInt32());

    var created = Assert.Single(_api.Gateway.Created);
    Assert.Equal(ElementKind.Topic, created.Kind);
    Assert.Equal("A web page", created.Title);
    Assert.Equal("<p>Hello <b>world</b></p>", created.Html);
    Assert.Equal(5, created.ParentId);
    Assert.Equal(LocalApiHarness.DefaultPriority, created.Priority);
    Assert.Equal(new ElementReferences("Page", "Ann", "https://example.org/a", "Example", "2026", "c", "a@example.org"), created.References);
  }

  [Fact]
  public async Task CreateItem_MapsQuestionAnswerParentAndPriority()
  {
    using var response = await _api.SendAsync(HttpMethod.Post, "/api/v1/elements", new
    {
      type = "item", question = "What is <i>2+2</i>?", answer = "4", parentId = 1, priority = 12.5,
    });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var created = Assert.Single(_api.Gateway.Created);
    Assert.Equal(ElementKind.Item, created.Kind);
    Assert.Equal("What is <i>2+2</i>?", created.Question);
    Assert.Equal("4", created.Answer);
    Assert.Null(created.Html);
    Assert.Null(created.Title);
    Assert.Null(created.References);
    Assert.Equal(1, created.ParentId);
    Assert.Equal(12.5, created.Priority);
  }

  [Fact]
  public async Task Create_UsesTheRoot_WhenNoElementIsShown()
  {
    _api.Gateway.CurrentId = null;

    using var response = await _api.SendAsync(HttpMethod.Post, "/api/v1/elements", new { type = "topic", html = "<p>x</p>" });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Equal(1, Assert.Single(_api.Gateway.Created).ParentId);
  }

  [Fact]
  public async Task Create_UsesTheRoot_WhenTheSettingSaysSo()
  {
    await using var api = LocalApiHarness.Create(configure: o => o with { ParentIsCurrentElement = false });

    using var response = await api.SendAsync(HttpMethod.Post, "/api/v1/elements", new { type = "topic", html = "<p>x</p>" });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Equal(1, Assert.Single(api.Gateway.Created).ParentId);
  }

  [Theory]
  [InlineData("{\"type\":\"topic\"}", 400, "html")]
  [InlineData("{\"type\":\"item\",\"question\":\"Q\"}", 400, "answer")]
  [InlineData("{\"type\":\"task\",\"html\":\"x\"}", 400, "type")]
  [InlineData("{\"type\":\"topic\",\"html\":\"x\",\"priority\":150}", 400, "priority")]
  [InlineData("{\"type\":\"topic\",\"html\":\"x\",\"priority\":-1}", 400, "priority")]
  [InlineData("{\"type\":\"topic\",\"html\":\"x\",\"parentId\":999}", 404, "999")]
  [InlineData("{\"type\":\"topic\",\"html\":\"x\",\"parentId\":0}", 400, "parentId")]
  [InlineData("{\"type\":\"topic\",\"html\":\"x\",\"colour\":\"red\"}", 400, "colour")]
  [InlineData("{\"type\":\"topic\",\"html\":\"x\",\"references\":{\"link\":\"javascript:alert(1)\"}}", 400, "link")]
  [InlineData("{\"type\":\"topic\",", 400, "not valid")]
  [InlineData("", 400, "empty")]
  public async Task Create_InvalidBody_IsRejected_AndCreatesNothing(string body, int status, string messagePart)
  {
    using var response = await PostRawAsync("/api/v1/elements", body, "application/json");

    Assert.Equal(status, (int)response.StatusCode);
    Assert.Contains(messagePart, await LocalApiHarness.ErrorAsync(response), StringComparison.OrdinalIgnoreCase);
    Assert.Empty(_api.Gateway.Created);
  }

  [Fact]
  public async Task Navigate_ShowsTheElement_Or404()
  {
    using var shown   = await _api.SendAsync(HttpMethod.Post, "/api/v1/navigate", new { id = 5 });
    using var missing = await _api.SendAsync(HttpMethod.Post, "/api/v1/navigate", new { id = 999 });

    Assert.Equal(HttpStatusCode.NoContent, shown.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    Assert.Equal([5], _api.Gateway.Navigated);
  }

  [Fact]
  public async Task BodyTooLarge_Is413()
  {
    var html = new string('a', 2 * 1024 * 1024);

    using var response = await PostRawAsync("/api/v1/elements", $"{{\"type\":\"topic\",\"html\":\"{html}\"}}", "application/json");

    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    Assert.Empty(_api.Gateway.Created);
  }

  [Theory]
  [InlineData("text/plain")]
  [InlineData("application/x-www-form-urlencoded")]
  public async Task WrongContentType_Is415(string contentType)
  {
    using var response = await PostRawAsync("/api/v1/elements", "{\"type\":\"topic\",\"html\":\"x\"}", contentType);

    Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
  }

  [Theory]
  [InlineData("/api/v1/nothing-here")]
  [InlineData("/api/v2/status")]
  [InlineData("/api/v1/elements/abc")]
  [InlineData("/")]
  public async Task UnknownRoute_Is404(string path)
  {
    using var response = await _api.SendAsync(HttpMethod.Get, path);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    Assert.False(string.IsNullOrEmpty(await LocalApiHarness.ErrorAsync(response)));
  }

  [Theory]
  [InlineData("DELETE", "/api/v1/status", "GET")]
  [InlineData("GET", "/api/v1/elements", "POST")]
  [InlineData("GET", "/api/v1/navigate", "POST")]
  public async Task WrongMethod_Is405_WithAllow(string method, string path, string allowed)
  {
    using var response = await _api.SendAsync(new HttpMethod(method), path);

    Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    Assert.Equal(allowed, Assert.Single(response.Content.Headers.Allow));
  }

  [Fact]
  public async Task Creates_RunOneAtATime_AndABusySuperMemoGives503()
  {
    await using var api = LocalApiHarness.Create(configure: o => o with { SuperMemoTimeout = TimeSpan.FromMilliseconds(300) });
    api.Gateway.CreateDelay = TimeSpan.FromMilliseconds(600);

    var first  = api.SendAsync(HttpMethod.Post, "/api/v1/elements", new { type = "topic", html = "<p>1</p>" });
    var second = api.SendAsync(HttpMethod.Post, "/api/v1/elements", new { type = "topic", html = "<p>2</p>" });
    var results = await Task.WhenAll(first, second);

    Assert.All(results, r => Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode));
    Assert.Contains("busy", await LocalApiHarness.ErrorAsync(results[0]));
    Assert.Equal(1, api.Gateway.MaxConcurrentCreates);

    foreach (var r in results)
      r.Dispose();
  }

  private Task<HttpResponseMessage> PostRawAsync(string path, string body, string contentType)
  {
    var request = LocalApiHarness.Request(HttpMethod.Post, path);
    request.Content = new StringContent(body, Encoding.UTF8);
    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
    return _api.SendAsync(request);
  }
}
