// Word counts, sentences, and quotes for rule messages.
namespace SuperMemoAssistant.Plugins.Formulation.Text;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>Small plain-text helpers shared by the rules.</summary>
public static partial class TextTools
{
  /// <summary>The longest quote, in characters, that a message contains.</summary>
  public const int MaxQuoteLength = 80;

  /// <summary>Counts the words of <paramref name="text" />. Placeholders and punctuation are not words.</summary>
  public static int CountWords(string? text) => string.IsNullOrEmpty(text) ? 0 : WordRegex().Count(text);

  /// <summary>Splits <paramref name="text" /> into sentences at line breaks and after ".", "!", or "?".</summary>
  public static IReadOnlyList<string> Sentences(string? text)
  {
    if (string.IsNullOrWhiteSpace(text))
      return [];

    return SentenceBreakRegex().Split(text)
                               .Select(s => s.Trim())
                               .Where(s => s.Length > 0)
                               .ToList();
  }

  /// <summary>Returns the first word of <paramref name="sentence" />, or an empty string.</summary>
  public static string FirstWord(string sentence)
  {
    var match = WordRegex().Match(sentence);

    return match.Success && sentence[..match.Index].All(c => !char.IsLetterOrDigit(c) && c != '[')
      ? match.Value
      : string.Empty;
  }

  /// <summary>Returns <paramref name="text" /> on one line in double quotes, shortened with "…" when it is long.</summary>
  public static string Quote(string? text)
  {
    var oneLine = SpacesRegex().Replace(text ?? string.Empty, " ").Trim();

    if (oneLine.Length > MaxQuoteLength)
      oneLine = oneLine[..(MaxQuoteLength - 1)].TrimEnd() + "…";

    return $"\"{oneLine}\"";
  }

  [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)*")]
  private static partial Regex WordRegex();

  [GeneratedRegex(@"\n|(?<=[.!?])\s+")]
  private static partial Regex SentenceBreakRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex SpacesRegex();
}
