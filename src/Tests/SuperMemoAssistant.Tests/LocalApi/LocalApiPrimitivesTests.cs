// The Local API building blocks: the HTML sanitizer, the token generator, and the origin rules.
namespace SuperMemoAssistant.Tests.LocalApi;

using SuperMemoAssistant.Plugins.LocalApi.Server;
using Xunit;

public sealed class LocalApiPrimitivesTests
{
  [Fact]
  public void Sanitizer_RemovesActiveContent_AndKeepsFormatting()
  {
    const string html =
      "<p style=\"color:red\" onclick=\"evil()\"><b>bold</b> <i>it</i> <a href=\"javascript:alert(1)\">bad</a> "
      + "<a href=\"https://ok.example/\">ok</a><script>alert(1)</script><iframe src=\"https://x.example\"></iframe>"
      + "<img src=\"  java&#09;script:alert(1)\" onerror=\"x()\" alt=\"pic\"><object data=\"x\"></object><embed src=\"x\">"
      + "<form action=\"https://x.example\"><input name=\"q\"></form><div style=\"width: expr/**/ession(alert(1))\">e</div>"
      + "<table><tr><td>cell</td></tr></table></p>";

    var clean = HtmlSanitizer.Sanitize(html);

    foreach (var forbidden in new[] { "<script", "<iframe", "<object", "<embed", "<form", "onclick", "onerror", "javascript", "ession(" })
      Assert.DoesNotContain(forbidden, clean, StringComparison.OrdinalIgnoreCase);

    foreach (var kept in new[] { "<b>bold</b>", "<i>it</i>", "style=\"color:red\"", "href=\"https://ok.example/\"", "alt=\"pic\"", "<td>cell</td>", ">bad</a>" })
      Assert.Contains(kept, clean);
  }

  [Fact]
  public void Sanitizer_LeavesSafeHtmlUnchanged()
  {
    const string html = "<h1>Title</h1><p>Text with <em>emphasis</em> and <a href=\"https://example.org\">a link</a>.</p><ul><li>one</li></ul>";

    Assert.Equal(html, HtmlSanitizer.Sanitize(html));
  }

  [Fact]
  public void Token_Is43Base64UrlCharacters_AndDiffersEachTime()
  {
    var tokens = Enumerable.Range(0, 20).Select(_ => TokenGenerator.Create()).ToList();

    Assert.All(tokens, t => Assert.Matches("^[A-Za-z0-9_-]{43}$", t));
    Assert.Equal(tokens.Count, tokens.Distinct(StringComparer.Ordinal).Count());
  }

  [Theory]
  [InlineData("Bearer secret", true)]
  [InlineData("bearer secret", true)]
  [InlineData("Bearer  secret ", true)]
  [InlineData("Bearer secret2", false)]
  [InlineData("Bearer ", false)]
  [InlineData("Basic secret", false)]
  [InlineData("secret", false)]
  [InlineData(null, false)]
  public void Token_IsCheckedAsABearerHeader(string? header, bool valid)
  {
    Assert.Equal(valid, TokenGenerator.IsValidBearer(header, "secret"));
  }

  [Fact]
  public void Token_EmptyExpectedToken_NeverMatches()
  {
    Assert.False(TokenGenerator.IsValidBearer("Bearer ", string.Empty));
  }

  [Theory]
  [InlineData("chrome-extension://abcdefghijklmnop", true)]
  [InlineData("moz-extension://0b2f6b1e-6b8a-4c43-9b8e-2d1b4f0d1a77", true)]
  [InlineData("safari-web-extension://ABC123", true)]
  [InlineData("http://localhost:3000", true)]
  [InlineData("http://localhost:3000/", true)]
  [InlineData("https://evil.example", false)]
  [InlineData("http://localhost:3001", false)]
  [InlineData("chrome-extension://", false)]
  [InlineData("chrome-extension://abc/../x", false)]
  [InlineData("null", false)]
  public void Guard_AllowsExtensionsAndTheAllowList_Only(string origin, bool allowed)
  {
    var guard = new RequestGuard(47321, ["http://localhost:3000"]);

    Assert.Equal(allowed, guard.IsAllowedOrigin(origin));
  }

  [Theory]
  [InlineData("127.0.0.1:47321", true)]
  [InlineData("localhost:47321", true)]
  [InlineData("LOCALHOST:47321", true)]
  [InlineData("localhost", false)]
  [InlineData("localhost:1", false)]
  [InlineData("attacker.example:47321", false)]
  [InlineData(null, false)]
  public void Guard_AcceptsOnlyLoopbackHostHeaders(string? host, bool allowed)
  {
    Assert.Equal(allowed, new RequestGuard(47321, []).IsAllowedHost(host));
  }
}
