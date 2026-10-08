namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

/// <summary>Creates and checks API tokens.</summary>
public static class TokenGenerator
{
  private const string BearerPrefix = "Bearer ";

  /// <summary>Creates a token from 32 random bytes, encoded as base64url without padding (43 characters).</summary>
  public static string Create() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

  /// <summary>Checks an Authorization header against the token in constant time.</summary>
  /// <param name="authorizationHeader">The value of the Authorization header, or <see langword="null" />.</param>
  /// <param name="token">The expected token.</param>
  public static bool IsValidBearer(string? authorizationHeader, string token)
  {
    if (string.IsNullOrEmpty(token)
      || authorizationHeader == null
      || authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase) == false)
      return false;

    var presented = authorizationHeader[BearerPrefix.Length..].Trim();

    // Hashing both values first makes the comparison independent of the length of the presented value.
    var expectedHash  = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));

    return CryptographicOperations.FixedTimeEquals(expectedHash, presentedHash);
  }
}
