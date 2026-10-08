namespace SuperMemoAssistant.Plugins.LocalApi.Server;

/// <summary>The kinds of element that the API can create.</summary>
public enum ElementKind
{
  /// <summary>A topic with HTML content.</summary>
  Topic,

  /// <summary>An item with a question and an answer.</summary>
  Item,
}

/// <summary>An element as the API reports it.</summary>
/// <param name="Id">The element number.</param>
/// <param name="Title">The element title.</param>
/// <param name="Type">The lowercase element type, for example "topic" or "item".</param>
/// <param name="ParentId">The parent element number, or <see langword="null" /> for the root.</param>
/// <param name="ChildCount">The number of children.</param>
public sealed record ElementInfo(int Id, string Title, string Type, int? ParentId, int ChildCount);

/// <summary>The state of SMA and SuperMemo.</summary>
/// <param name="SmaVersion">The SMA version.</param>
/// <param name="Collection">The name of the open collection, or <see langword="null" />.</param>
/// <param name="SuperMemoRunning">Whether SuperMemo is running and connected.</param>
/// <param name="CurrentElement">The element in the element window, or <see langword="null" />.</param>
public sealed record ApiStatus(string SmaVersion, string? Collection, bool SuperMemoRunning, ElementInfo? CurrentElement);

/// <summary>The SuperMemo references of a new element. Every value is optional.</summary>
public sealed record ElementReferences(
  string? Title,
  string? Author,
  string? Link,
  string? Source,
  string? Date,
  string? Comment,
  string? Email);

/// <summary>A validated element that the gateway must create. The HTML is already sanitized.</summary>
/// <param name="Kind">Topic or item.</param>
/// <param name="Title">The title, or <see langword="null" /> to let SMA make one from the content.</param>
/// <param name="Html">The topic content.</param>
/// <param name="Question">The item question.</param>
/// <param name="Answer">The item answer.</param>
/// <param name="ParentId">The parent element number.</param>
/// <param name="Priority">The priority, from 0 to 100.</param>
/// <param name="References">The references, or <see langword="null" />.</param>
public sealed record NewElement(
  ElementKind        Kind,
  string?            Title,
  string?            Html,
  string?            Question,
  string?            Answer,
  int                ParentId,
  double             Priority,
  ElementReferences? References);
