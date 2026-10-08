namespace SuperMemoAssistant.Tests.UiAgent;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Automation;

/// <summary>
///   The agent's commands. The agent sees every window of the desktop that it runs on. A command names a top-level window ("window": a title fragment, or "handle") and, for element
///   commands, a descendant ("name", "id" = AutomationId, "type" = control type such as "Button", "index" = n-th match).
/// </summary>
internal sealed class Commands
{
  public object? Execute(JsonElement request)
  {
    var op = request.GetProperty("op").GetString();
    return op switch
    {
      "windows" => Windows().Select(w => new { handle = w.Current.NativeWindowHandle, name = w.Current.Name }).ToList(),
      "wait-window" => WaitFor(() => FindWindow(request), Timeout(request)) is { } w ? w.Current.NativeWindowHandle : 0,
      "describe" => Describe(Window(request)),
      "exists" => FindElement(request) is not null,
      "wait" => WaitFor(() => FindElement(request), Timeout(request)) is not null,
      "read" => TryProbe(() => FindElement(request) is { } element ? Read(element) : null),
      "invoke" => Act(Element(request), Invoke),
      "select" => Act(Element(request), Select),
      "expand" => Act(Element(request), e => ((ExpandCollapsePattern)e.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand()),
      "set-value" => Act(Element(request), e => ((ValuePattern)e.GetCurrentPattern(ValuePattern.Pattern)).SetValue(request.GetProperty("text").GetString())),
      "close" => Act(Window(request), e => ((WindowPattern)e.GetCurrentPattern(WindowPattern.Pattern)).Close()),
      "screenshot" => Screenshot.Save(request.TryGetProperty("hwnd", out var hwnd) ? new IntPtr(hwnd.GetInt64())
                                                                                    : new IntPtr(Window(request).Current.NativeWindowHandle),
                                      request.GetProperty("path").GetString()!),
      _ => throw new ArgumentException($"Unknown command \"{op}\"."),
    };
  }

  private static List<AutomationElement> Windows() => DesktopWindows.Visible();

  private AutomationElement? FindWindow(JsonElement request)
  {
    if (request.TryGetProperty("handle", out var handle))
      return Windows().FirstOrDefault(w => w.Current.NativeWindowHandle == handle.GetInt32());

    var title = request.GetProperty("window").GetString()!;
    return Windows().FirstOrDefault(w => w.Current.Name.Contains(title, StringComparison.OrdinalIgnoreCase));
  }

  private AutomationElement Window(JsonElement request) =>
    FindWindow(request) ?? throw new InvalidOperationException($"No window matches {request}.");

  private AutomationElement? FindElement(JsonElement request)
  {
    if (FindWindow(request) is not { } window)
      return null;

    string? Text(string key) => request.TryGetProperty(key, out var value) ? value.GetString() : null;
    var name = Text("name");
    var id = Text("id");
    var type = Text("type");
    var index = request.TryGetProperty("index", out var i) ? i.GetInt32() : 0;

    var scope = window;
    if (Text("within") is { } within)
    {
      var anchor = window.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                         .Cast<AutomationElement>()
                         .FirstOrDefault(e => e.Current.Name.Contains(within, StringComparison.OrdinalIgnoreCase));
      if (anchor is null || ListItemOf(anchor) is not { } item)
        return null;

      scope = item;
    }

    var matches = scope.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                        .Cast<AutomationElement>()
                        .Where(e => id is null || e.Current.AutomationId == id)
                        .Where(e => type is null || e.Current.ControlType.ProgrammaticName == "ControlType." + type)
                        .Where(e => name is null || e.Current.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                        .ToList();

    // An exact name match wins over a partial one, so "Open" does not pick "Open a collection".
    if (name is not null && matches.Any(e => e.Current.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
      matches = matches.Where(e => e.Current.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();

    return matches.ElementAtOrDefault(index);
  }

  /// <summary>The list or tree item that contains <paramref name="element" />, so that "within" scopes a row of a list.</summary>
  private static AutomationElement? ListItemOf(AutomationElement element)
  {
    for (var current = element; current is not null; current = TreeWalker.ControlViewWalker.GetParent(current))
      if (current.Current.ControlType == ControlType.ListItem || current.Current.ControlType == ControlType.TreeItem
                                                               || current.Current.ControlType == ControlType.DataItem)
        return current;

    return null;
  }

  private AutomationElement Element(JsonElement request) =>
    FindElement(request) ?? throw new InvalidOperationException($"No element matches {request}.");

  private static TimeSpan Timeout(JsonElement request) =>
    TimeSpan.FromMilliseconds(request.TryGetProperty("timeoutMs", out var t) ? t.GetInt32() : 30_000);

  private static T? WaitFor<T>(Func<T?> probe, TimeSpan timeout) where T : class
  {
    var deadline = DateTime.UtcNow + timeout;
    do
    {
      if (TryProbe(probe) is { } found)
        return found;

      Thread.Sleep(250);
    } while (DateTime.UtcNow < deadline);

    return TryProbe(probe);
  }

  /// <summary>A window that closes during the search (for example, a wizard that finishes) counts as not found yet.</summary>
  private static T? TryProbe<T>(Func<T?> probe) where T : class
  {
    try
    {
      return probe();
    }
    catch (ElementNotAvailableException)
    {
      return null;
    }
  }

  private static bool Act(AutomationElement element, Action<AutomationElement> action)
  {
    if (!element.Current.IsEnabled)
      throw new InvalidOperationException($"\"{element.Current.Name}\" is disabled.");

    action(element);
    return true;
  }

  private static void Invoke(AutomationElement element)
  {
    if (element.Current.FrameworkId == "Win32" && element.Current.ControlType == ControlType.Button)
      Win32Button.Click(new IntPtr(element.Current.NativeWindowHandle));
    else if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invoke))
      ((InvokePattern)invoke).Invoke();
    else if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var toggle))
      ((TogglePattern)toggle).Toggle();
    else
      Select(element);
  }

  /// <summary>Selects the element, or its nearest selectable ancestor (a list item that contains the matched text).</summary>
  private static void Select(AutomationElement element)
  {
    for (var current = element; current is not null; current = TreeWalker.ControlViewWalker.GetParent(current))
      if (current.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var item))
      {
        ((SelectionItemPattern)item).Select();
        return;
      }

    throw new InvalidOperationException($"\"{element.Current.Name}\" cannot be invoked or selected.");
  }

  private static object Read(AutomationElement element) => new
  {
    name = element.Current.Name,
    enabled = element.Current.IsEnabled,
    value = element.TryGetCurrentPattern(ValuePattern.Pattern, out var value) ? ((ValuePattern)value).Current.Value : null,
    toggled = element.TryGetCurrentPattern(TogglePattern.Pattern, out var toggle)
      ? ((TogglePattern)toggle).Current.ToggleState == ToggleState.On
      : (bool?)null,
  };

  private static string Describe(AutomationElement window) => string.Join(Environment.NewLine,
    window.FindAll(TreeScope.Descendants, Condition.TrueCondition)
          .Cast<AutomationElement>()
          .Where(e => !string.IsNullOrWhiteSpace(e.Current.Name) || !string.IsNullOrWhiteSpace(e.Current.AutomationId))
          .Select(e => $"{e.Current.ControlType.ProgrammaticName["ControlType.".Length..]} '{e.Current.Name}' " +
                       $"#{e.Current.AutomationId}{(e.Current.IsEnabled ? "" : " (disabled)")}"));
}
