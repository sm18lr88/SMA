namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Routes authenticated requests to the endpoints of version 1, and calls SuperMemo with a time limit.</summary>
public sealed class ApiEndpoints
{
  /// <summary>The version of the API in the URL and in the status.</summary>
  public const int ApiVersion = 1;

  private const string Prefix = "/api/v1";

  private readonly ApiOptions        _options;
  private readonly ISuperMemoGateway _gateway;
  private readonly SemaphoreSlim     _changeLock = new(1, 1);

  /// <summary>Creates the endpoints.</summary>
  public ApiEndpoints(ApiOptions options, ISuperMemoGateway gateway)
  {
    _options = options;
    _gateway = gateway;
  }

  /// <summary>Runs the endpoint for <paramref name="request" />.</summary>
  /// <exception cref="ApiException">The request fails with a status for the client.</exception>
  public Task<ApiResponse> DispatchAsync(ApiRequest request, CancellationToken ct)
  {
    var path = request.Path.Length > 1 ? request.Path.TrimEnd('/') : request.Path;

    if (path.StartsWith(Prefix + "/", StringComparison.Ordinal) == false)
      throw NotFound(request.Path);

    var route  = path[Prefix.Length..];
    var method = request.Method.ToUpperInvariant();

    return route switch
    {
      "/status"           => Only("GET", method, () => GetStatusAsync(ct)),
      "/elements/current" => Only("GET", method, () => GetCurrentAsync(ct)),
      "/elements"         => Only("POST", method, () => CreateAsync(request, ct)),
      "/navigate"         => Only("POST", method, () => NavigateAsync(request, ct)),
      _ when TryElementId(route, out var id) => Only("GET", method, () => GetElementAsync(id, ct)),
      _                   => throw NotFound(request.Path),
    };
  }

  private static Task<ApiResponse> Only(string allowed, string method, Func<Task<ApiResponse>> handler)
  {
    if (method == allowed)
      return handler();

    var response = ApiResponse.Error(405, $"This endpoint accepts only {allowed} requests.");
    response.Headers["Allow"] = allowed;
    return Task.FromResult(response);
  }

  private async Task<ApiResponse> GetStatusAsync(CancellationToken ct)
  {
    var status = await CallAsync(_gateway.GetStatus, false, ct).ConfigureAwait(false);
    var current = status.CurrentElement is { } e ? new { id = e.Id, title = e.Title, type = e.Type } : null;

    return new ApiResponse(200, new
    {
      smaVersion       = status.SmaVersion,
      apiVersion       = ApiVersion,
      collection       = status.Collection,
      superMemoRunning = status.SuperMemoRunning,
      currentElement   = current,
    });
  }

  private async Task<ApiResponse> GetCurrentAsync(CancellationToken ct)
  {
    var e = await CallAsync(_gateway.GetCurrentElement, false, ct).ConfigureAwait(false)
      ?? throw new ApiException(404, "No element is shown in the element window.");

    return new ApiResponse(200, new { id = e.Id, title = e.Title, type = e.Type, parentId = e.ParentId });
  }

  private async Task<ApiResponse> GetElementAsync(int id, CancellationToken ct)
  {
    var e = await CallAsync(() => _gateway.GetElement(id), false, ct).ConfigureAwait(false)
      ?? throw ElementMissing(id);

    return new ApiResponse(200, new { id = e.Id, title = e.Title, type = e.Type, parentId = e.ParentId, childCount = e.ChildCount });
  }

  private async Task<ApiResponse> CreateAsync(ApiRequest request, CancellationToken ct)
  {
    var body                = await JsonBody.ReadAsync<CreateElementBody>(request, _options.MaxBodyBytes, ct).ConfigureAwait(false);
    var (element, parentId) = ElementRequestValidator.Validate(body, _options.DefaultPriority);

    var id = await CallAsync(() =>
    {
      var parent = parentId ?? DefaultParentId();

      if (_gateway.GetElement(parent) == null)
        throw new ApiException(404, string.Create(CultureInfo.InvariantCulture, $"The parent element {parent} does not exist."));

      return _gateway.CreateElement(element with { ParentId = parent });
    }, true, ct).ConfigureAwait(false);

    return new ApiResponse(201, new { id });
  }

  private async Task<ApiResponse> NavigateAsync(ApiRequest request, CancellationToken ct)
  {
    var body = await JsonBody.ReadAsync<NavigateBody>(request, _options.MaxBodyBytes, ct).ConfigureAwait(false);
    var id   = ElementRequestValidator.ValidateNavigate(body);

    await CallAsync(() =>
    {
      if (_gateway.GetElement(id) == null)
        throw ElementMissing(id);

      return _gateway.Navigate(id)
        ? true
        : throw new ApiException(500, string.Create(CultureInfo.InvariantCulture, $"SuperMemo did not show element {id}."));
    }, true, ct).ConfigureAwait(false);

    return ApiResponse.NoContent();
  }

  private int DefaultParentId() =>
    _options.ParentIsCurrentElement && _gateway.GetCurrentElement() is { } current ? current.Id : _gateway.GetRootElementId();

  /// <summary>
  ///   Runs a blocking SuperMemo call on the thread pool. A call that changes SuperMemo holds the change lock until it
  ///   really ends, also after a time-out, so that two changes never run at the same time.
  /// </summary>
  private async Task<T> CallAsync<T>(Func<T> call, bool changesSuperMemo, CancellationToken ct)
  {
    if (changesSuperMemo && await _changeLock.WaitAsync(_options.SuperMemoTimeout, ct).ConfigureAwait(false) == false)
      throw Busy();

    var task = Task.Run(call, CancellationToken.None);

    if (changesSuperMemo)
      _ = task.ContinueWith(t =>
      {
        _ = t.Exception;
        _changeLock.Release();
      }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    try
    {
      return await task.WaitAsync(_options.SuperMemoTimeout, ct).ConfigureAwait(false);
    }
    catch (TimeoutException)
    {
      throw Busy();
    }
  }

  private static bool TryElementId(string route, out int id)
  {
    id = 0;
    return route.StartsWith("/elements/", StringComparison.Ordinal)
      && int.TryParse(route.AsSpan("/elements/".Length), NumberStyles.None, CultureInfo.InvariantCulture, out id)
      && id > 0;
  }

  private static ApiException Busy() =>
    new(503, "SuperMemo is busy and did not answer in time. Close any open SuperMemo dialog and try again.");

  private static ApiException ElementMissing(int id) =>
    new(404, string.Create(CultureInfo.InvariantCulture, $"Element {id} does not exist."));

  private static ApiException NotFound(string path) => new(404, $"There is no endpoint at {path}.");
}
