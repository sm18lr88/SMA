namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Checks the Host and Origin headers, which protect the API from web pages in a browser.</summary>
public sealed class RequestGuard
{
  private static readonly string[] ExtensionSchemes = ["chrome-extension://", "moz-extension://", "safari-web-extension://"];

  private readonly HashSet<string> _hosts;
  private readonly HashSet<string> _allowedOrigins;

  /// <summary>Creates a guard for a port and a list of extra allowed origins.</summary>
  public RequestGuard(int port, IEnumerable<string> allowedOrigins)
  {
    _hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      string.Create(CultureInfo.InvariantCulture, $"127.0.0.1:{port}"),
      string.Create(CultureInfo.InvariantCulture, $"localhost:{port}"),
    };

    _allowedOrigins = new HashSet<string>(allowedOrigins.Select(NormalizeOrigin).OfType<string>(), StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  ///   Whether the Host header names this server. A DNS rebinding attack sends the name of the attacker's site, so any
  ///   other name is refused.
  /// </summary>
  public bool IsAllowedHost(string? host) => host != null && _hosts.Contains(host.Trim());

  /// <summary>Whether a browser origin may call the API: a browser extension, or an origin in the allow list.</summary>
  public bool IsAllowedOrigin(string origin)
  {
    var normalized = NormalizeOrigin(origin);

    if (normalized == null)
      return false;

    if (_allowedOrigins.Contains(normalized))
      return true;

    var scheme = ExtensionSchemes.FirstOrDefault(s => normalized.StartsWith(s, StringComparison.OrdinalIgnoreCase));
    if (scheme == null)
      return false;

    var id = normalized[scheme.Length..];
    return id.Length > 0 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
  }

  /// <summary>Returns an origin without white space and a final slash, or <see langword="null" /> when it is empty or "null".</summary>
  public static string? NormalizeOrigin(string? origin)
  {
    var value = origin?.Trim().TrimEnd('/');

    return string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : value;
  }
}
