namespace SuperMemoAssistant.Plugins.Books.Planning
{
  using System;

  /// <summary>
  ///   Computes SuperMemo priorities. In SuperMemo a larger percentage is a lower priority, so each further chapter
  ///   gets a slightly larger value and the queue presents earlier chapters first.
  /// </summary>
  public static class PriorityCalculator
  {
    /// <summary>Limits a priority to 0..100. A value that is not a number becomes 0.</summary>
    public static double Clamp(double priority) => double.IsNaN(priority) ? 0 : Math.Clamp(priority, 0, 100);

    /// <summary>The priority of the child at <paramref name="index" /> (0 is the first child).</summary>
    public static double ForChild(double parentPriority, double step, int index)
    {
      var safeStep = double.IsNaN(step) ? 0 : Math.Max(0, step);
      return Math.Round(Clamp(Clamp(parentPriority) + safeStep * (Math.Max(0, index) + 1)), 3);
    }
  }
}
