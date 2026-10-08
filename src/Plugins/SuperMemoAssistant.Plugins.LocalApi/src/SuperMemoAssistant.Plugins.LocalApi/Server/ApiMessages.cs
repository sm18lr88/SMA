namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>The parts of an HTTP request that the pipeline reads. It does not depend on a particular HTTP server.</summary>
public sealed class ApiRequest
{
  /// <summary>The HTTP method, for example "GET".</summary>
  public required string Method { get; init; }

  /// <summary>The URL path without the query string, for example "/api/v1/status".</summary>
  public required string Path { get; init; }

  /// <summary>Whether the connection comes from a loopback address.</summary>
  public required bool FromLoopback { get; init; }

  /// <summary>The Host header.</summary>
  public string? Host { get; init; }

  /// <summary>The Origin header.</summary>
  public string? Origin { get; init; }

  /// <summary>The Authorization header.</summary>
  public string? Authorization { get; init; }

  /// <summary>The Content-Type header.</summary>
  public string? ContentType { get; init; }

  /// <summary>The Content-Length header, or -1 when the length is not known.</summary>
  public long ContentLength { get; init; } = -1;

  /// <summary>The Access-Control-Request-Method header of a CORS preflight.</summary>
  public string? AccessControlRequestMethod { get; init; }

  /// <summary>Whether a preflight asks for private network access (Access-Control-Request-Private-Network).</summary>
  public bool RequestsPrivateNetwork { get; init; }

  /// <summary>The request body.</summary>
  public Stream Body { get; init; } = Stream.Null;
}

/// <summary>The response that the pipeline makes.</summary>
/// <param name="StatusCode">The HTTP status code.</param>
/// <param name="Json">The object to serialize as the JSON body, or <see langword="null" /> for no body.</param>
public sealed record ApiResponse(int StatusCode, object? Json)
{
  /// <summary>Extra response headers.</summary>
  public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>Makes an error response with the body <c>{"error": message}</c>.</summary>
  public static ApiResponse Error(int statusCode, string message) => new(statusCode, new ErrorBody(message));

  /// <summary>Makes a response without a body.</summary>
  public static ApiResponse NoContent() => new(204, null);
}

/// <summary>The JSON body of an error.</summary>
/// <param name="Error">The message.</param>
public sealed record ErrorBody(string Error);
