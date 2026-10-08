namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;

/// <summary>An error that the API returns to the client as <c>{"error": message}</c> with an HTTP status.</summary>
public sealed class ApiException : Exception
{
  /// <summary>Creates an API error.</summary>
  /// <param name="statusCode">The HTTP status code.</param>
  /// <param name="message">A message for the user of the client.</param>
  public ApiException(int statusCode, string message)
    : base(message)
  {
    StatusCode = statusCode;
  }

  /// <summary>The HTTP status code.</summary>
  public int StatusCode { get; }
}
