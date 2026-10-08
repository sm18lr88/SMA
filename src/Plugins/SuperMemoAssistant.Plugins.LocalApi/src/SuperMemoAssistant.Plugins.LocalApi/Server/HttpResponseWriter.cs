namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System.Net;
using System.Threading.Tasks;

/// <summary>Writes an <see cref="ApiResponse" /> to an <see cref="HttpListenerResponse" />.</summary>
internal static class HttpResponseWriter
{
  public static async Task WriteAsync(HttpListenerResponse target, ApiResponse response)
  {
    using (target)
    {
      target.StatusCode = response.StatusCode;
      target.Headers["Cache-Control"]          = "no-store";
      target.Headers["X-Content-Type-Options"] = "nosniff";

      foreach (var (name, value) in response.Headers)
        target.Headers[name] = value;

      if (response.Json == null)
      {
        target.ContentLength64 = 0;
        return;
      }

      var bytes = JsonBody.Serialize(response.Json);
      target.ContentType     = "application/json; charset=utf-8";
      target.ContentLength64 = bytes.Length;
      await target.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
    }
  }
}
