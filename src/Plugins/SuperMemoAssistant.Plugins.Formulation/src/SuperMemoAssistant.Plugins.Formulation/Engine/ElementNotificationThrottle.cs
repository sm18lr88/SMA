// Keeps the automatic check quiet: at most one notification per element per session.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>Decides whether the automatic check may show a notification for an element.</summary>
public sealed class ElementNotificationThrottle
{
  private readonly HashSet<int> _notifiedElementIds = [];
  private readonly Lock         _lock               = new();

  /// <summary>
  ///   Returns true, once per element, when <paramref name="findings" /> contain at least one warning. Advice alone never
  ///   causes a notification.
  /// </summary>
  public bool ShouldNotify(int elementId, IEnumerable<FormulationFinding> findings)
  {
    if (!findings.Any(f => f.Severity == FindingSeverity.Warning))
      return false;

    lock (_lock)
      return _notifiedElementIds.Add(elementId);
  }

  /// <summary>Starts a new session: every element can cause a notification again.</summary>
  public void Reset()
  {
    lock (_lock)
      _notifiedElementIds.Clear();
  }
}
