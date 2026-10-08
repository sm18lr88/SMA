namespace SuperMemoAssistant.Plugins.LocalApi.Server;

/// <summary>
///   The SuperMemo operations that the API uses. Methods are synchronous and can block. When SuperMemo is not running,
///   they throw an <see cref="ApiException" /> with status 503; when no collection is open, with status 409.
/// </summary>
public interface ISuperMemoGateway
{
  /// <summary>Gets the state of SMA and SuperMemo. This method does not throw when SuperMemo is not running.</summary>
  ApiStatus GetStatus();

  /// <summary>Gets the element in the element window, or <see langword="null" /> when no element is shown.</summary>
  ElementInfo? GetCurrentElement();

  /// <summary>Gets an element, or <see langword="null" /> when it does not exist or is deleted.</summary>
  ElementInfo? GetElement(int id);

  /// <summary>Gets the number of the collection root element.</summary>
  int GetRootElementId();

  /// <summary>Creates one element and returns its number.</summary>
  int CreateElement(NewElement element);

  /// <summary>Shows an element in the element window. Returns whether SuperMemo showed it.</summary>
  bool Navigate(int id);
}
