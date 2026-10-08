using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperMemoAssistant.Plugins.Writing.Import
{
  /// <summary>
  ///   Priorities for topics in document order. In SuperMemo a larger percentage is a lower priority, so the values
  ///   ascend: the first topic has the highest priority.
  /// </summary>
  public static class PrioritySequence
  {
    /// <summary>The default distance between two consecutive priorities, in percentage points.</summary>
    public const double DefaultStep = 0.1;

    /// <summary>
    ///   Returns <paramref name="count" /> priorities from <paramref name="start" />. The step is reduced when the
    ///   values would pass 100, so that the last value is 100 at most.
    /// </summary>
    public static IReadOnlyList<double> Create(double start, int count, double step = DefaultStep)
    {
      if (double.IsNaN(start) || start < 0 || start > 100)
        throw new ArgumentOutOfRangeException(nameof(start), start, "The priority must be between 0 and 100.");

      ArgumentOutOfRangeException.ThrowIfNegative(count);
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);

      if (count == 0)
        return [];

      var effectiveStep = count == 1 ? 0 : Math.Min(step, (100 - start) / (count - 1));

      return Enumerable.Range(0, count)
                       .Select(i => Math.Min(100, start + i * effectiveStep))
                       .ToArray();
    }
  }
}
