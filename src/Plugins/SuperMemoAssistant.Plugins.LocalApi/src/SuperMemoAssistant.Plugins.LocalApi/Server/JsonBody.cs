namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Reads and writes the JSON of the API.</summary>
public static class JsonBody
{
  /// <summary>camelCase names; unknown properties are errors, so that a misspelled field is not ignored.</summary>
  public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
  {
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
  };

  /// <summary>Reads a JSON request body of at most <paramref name="maxBytes" /> bytes.</summary>
  /// <exception cref="ApiException">415, 413, or 400 when the body is not acceptable.</exception>
  public static async Task<T> ReadAsync<T>(ApiRequest request, long maxBytes, CancellationToken ct)
    where T : class
  {
    if (IsJson(request.ContentType) == false)
      throw new ApiException(415, "The request body must be JSON. Send the header \"Content-Type: application/json\".");

    if (request.ContentLength > maxBytes)
      throw TooLarge(maxBytes);

    var bytes = await ReadLimitedAsync(request.Body, maxBytes, ct).ConfigureAwait(false);

    if (bytes.Length == 0)
      throw new ApiException(400, "The request body is empty. Send a JSON object.");

    try
    {
      return JsonSerializer.Deserialize<T>(bytes, Options) ?? throw new ApiException(400, "The request body must be a JSON object.");
    }
    catch (JsonException ex)
    {
      throw new ApiException(400, $"The request body is not valid: {ex.Message}");
    }
  }

  /// <summary>Serializes a response object.</summary>
  public static byte[] Serialize(object value) => JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), Options);

  private static bool IsJson(string? contentType)
  {
    if (MediaTypeHeaderValue.TryParse(contentType, out var mediaType) == false
      || string.Equals(mediaType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) == false)
      return false;

    return mediaType.CharSet == null || mediaType.CharSet.Trim('"').Equals("utf-8", StringComparison.OrdinalIgnoreCase);
  }

  private static async Task<byte[]> ReadLimitedAsync(Stream body, long maxBytes, CancellationToken ct)
  {
    using var buffer = new MemoryStream();
    var       chunk  = new byte[81920];

    while (true)
    {
      var read = await body.ReadAsync(chunk, ct).ConfigureAwait(false);
      if (read == 0)
        return buffer.ToArray();

      if (buffer.Length + read > maxBytes)
        throw TooLarge(maxBytes);

      buffer.Write(chunk, 0, read);
    }
  }

  private static ApiException TooLarge(long maxBytes) =>
    new(413, $"The request body is larger than {maxBytes / (1024 * 1024)} MB.");
}
