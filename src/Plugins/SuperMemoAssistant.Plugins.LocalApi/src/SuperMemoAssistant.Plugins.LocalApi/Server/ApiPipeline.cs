namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Threading;
using System.Threading.Tasks;
using Anotar.Serilog;

/// <summary>
///   Turns one <see cref="ApiRequest" /> into one <see cref="ApiResponse" />. The checks run in this order: loopback
///   address, Host header, Origin header, CORS preflight, token, route. The pipeline never throws.
/// </summary>
public sealed class ApiPipeline
{
  private readonly ApiOptions   _options;
  private readonly RequestGuard _guard;
  private readonly ApiEndpoints _endpoints;

  /// <summary>Creates a pipeline.</summary>
  public ApiPipeline(ApiOptions options, ISuperMemoGateway gateway)
  {
    _options   = options;
    _guard     = new RequestGuard(options.Port, options.AllowedOrigins);
    _endpoints = new ApiEndpoints(options, gateway);
  }

  /// <summary>Handles one request.</summary>
  public async Task<ApiResponse> HandleAsync(ApiRequest request, CancellationToken ct)
  {
    if (request.FromLoopback == false)
      return ApiResponse.Error(403, "Only programs on this computer can use the local API.");

    if (_guard.IsAllowedHost(request.Host) == false)
      return ApiResponse.Error(403, $"The Host header must be 127.0.0.1:{_options.Port} or localhost:{_options.Port}.");

    var origin = RequestGuard.NormalizeOrigin(request.Origin);
    if (request.Origin != null && (origin == null || _guard.IsAllowedOrigin(origin) == false))
      return ApiResponse.Error(403, "This web origin cannot use the local API. Browser extensions can; other origins must be in the allowed origins of the Local API settings.");

    var response = await RespondAsync(request, origin != null, ct).ConfigureAwait(false);

    if (origin != null)
    {
      response.Headers["Access-Control-Allow-Origin"] = origin;
      response.Headers["Vary"]                        = "Origin";
    }

    return response;
  }

  private async Task<ApiResponse> RespondAsync(ApiRequest request, bool hasOrigin, CancellationToken ct)
  {
    if (hasOrigin && IsPreflight(request))
      return Preflight(request);

    if (TokenGenerator.IsValidBearer(request.Authorization, _options.Token) == false)
    {
      var denied = ApiResponse.Error(401, "Send the access token from the Local API settings in the header \"Authorization: Bearer <token>\".");
      denied.Headers["WWW-Authenticate"] = "Bearer";
      return denied;
    }

    try
    {
      return await _endpoints.DispatchAsync(request, ct).ConfigureAwait(false);
    }
    catch (ApiException ex)
    {
      return ApiResponse.Error(ex.StatusCode, ex.Message);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
      return ApiResponse.Error(503, "The local API is stopping.");
    }
    catch (Exception ex) when (ex is not OutOfMemoryException)
    {
      LogTo.Error(ex, "Local API: {Method} {Path} failed", request.Method, request.Path);
      return ApiResponse.Error(500, "An unexpected error occurred. The SMA log has the details.");
    }
  }

  private static bool IsPreflight(ApiRequest request) =>
    request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.AccessControlRequestMethod) == false;

  private static ApiResponse Preflight(ApiRequest request)
  {
    var response = ApiResponse.NoContent();
    response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
    response.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type";
    response.Headers["Access-Control-Max-Age"]       = "600";

    if (request.RequestsPrivateNetwork)
      response.Headers["Access-Control-Allow-Private-Network"] = "true";

    return response;
  }
}
