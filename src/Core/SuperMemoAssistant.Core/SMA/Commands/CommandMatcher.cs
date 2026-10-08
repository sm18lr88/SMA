namespace SuperMemoAssistant.SMA.Commands
{
  using System;

  /// <summary>
  ///   Fuzzy matching for the palette. Each word of the query must appear in the text as a subsequence of characters
  ///   (so "ibk" finds "Import a book or Kindle highlights"); matches at word starts and runs of adjacent characters score
  ///   higher.
  /// </summary>
  public static class CommandMatcher
  {
    private const int CharScore      = 1;
    private const int WordStartBonus = 8;
    private const int AdjacentBonus  = WordStartBonus;
    private const int GapPenalty     = 2;
    private const int NoMatch        = int.MinValue / 2;

    /// <summary>Returns the score of <paramref name="text" /> for <paramref name="query" />, or null when it does not match.</summary>
    public static int? Score(string query, string text)
    {
      if (string.IsNullOrWhiteSpace(query))
        return 0;

      if (string.IsNullOrEmpty(text))
        return null;

      var total = 0;

      foreach (var word in query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
      {
        var score = ScoreWord(word, text);
        if (score == NoMatch)
          return null;

        total += score;
      }

      return total;
    }

    /// <summary>
    ///   Dynamic programming over (query character, text position). best[p] is the best score of the query prefix that
    ///   ends with its last character at text position p. With a constant gap penalty, the best earlier position is a
    ///   running maximum, so one pass per query character is enough.
    /// </summary>
    private static int ScoreWord(string word, string text)
    {
      var previous = new int[text.Length];
      var current  = new int[text.Length];

      for (var p = 0; p < text.Length; p++)
        previous[p] = SameChar(word[0], text[p]) ? PositionScore(text, p) : NoMatch;

      for (var j = 1; j < word.Length; j++)
      {
        var bestBefore = NoMatch;

        for (var p = 0; p < text.Length; p++)
        {
          current[p] = NoMatch;

          if (p >= 1 && SameChar(word[j], text[p]))
          {
            var adjacent = previous[p - 1] == NoMatch ? NoMatch : previous[p - 1] + AdjacentBonus;
            var gapped   = bestBefore == NoMatch ? NoMatch : bestBefore - GapPenalty;
            var best     = Math.Max(adjacent, gapped);

            if (best != NoMatch)
              current[p] = best + PositionScore(text, p);
          }

          // Positions up to p - 1 can precede position p + 1 with a gap.
          if (p >= 1)
            bestBefore = Math.Max(bestBefore, previous[p - 1]);
        }

        (previous, current) = (current, previous);
      }

      var result = NoMatch;
      foreach (var score in previous)
        result = Math.Max(result, score);

      return result;
    }

    private static int PositionScore(string text, int p) =>
      CharScore + (p == 0 || !char.IsLetterOrDigit(text[p - 1]) || char.IsUpper(text[p]) && char.IsLower(text[p - 1])
        ? WordStartBonus
        : 0);

    private static bool SameChar(char a, char b) => char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
  }
}
