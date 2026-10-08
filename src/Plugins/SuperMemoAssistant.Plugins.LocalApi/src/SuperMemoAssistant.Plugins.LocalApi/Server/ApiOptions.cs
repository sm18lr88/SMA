namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Collections.Generic;

/// <summary>The settings of one server run.</summary>
/// <param name="Port">The loopback TCP port.</param>
/// <param name="Token">The bearer token that every request must send.</param>
/// <param name="AllowedOrigins">Web origins that may call the API, in addition to browser extensions.</param>
/// <param name="ParentIsCurrentElement">Whether a new element without a parent goes under the current element.</param>
/// <param name="DefaultPriority">The priority of a new element without a priority.</param>
public sealed record ApiOptions(
  int                         Port,
  string                      Token,
  IReadOnlyCollection<string> AllowedOrigins,
  bool                        ParentIsCurrentElement,
  double                      DefaultPriority)
{
  /// <summary>The default port.</summary>
  public const int DefaultPort = 47321;

  /// <summary>The largest accepted request body, in bytes.</summary>
  public long MaxBodyBytes { get; init; } = 2 * 1024 * 1024;

  /// <summary>How long a request waits for SuperMemo before it fails with 503.</summary>
  public TimeSpan SuperMemoTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
