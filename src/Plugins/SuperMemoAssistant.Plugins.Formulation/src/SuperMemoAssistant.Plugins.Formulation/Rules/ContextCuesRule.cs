// Rule 13: a cloze sentence that starts with a pronoun lacks context.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Text;

/// <summary>Finds cloze sentences that start with a pronoun such as "He" or "This".</summary>
public sealed class ContextCuesRule()
  : FormulationRule(RuleIds.ContextCues, "Context cues simplify wording", "Context")
{
  /// <summary>The pronouns that make a cloze sentence depend on text that the item does not show.</summary>
  public static IReadOnlySet<string> Pronouns { get; } = new HashSet<string>(
    ["He", "She", "It", "They", "This", "These", "That", "Those", "His", "Her", "Its", "Their"],
    StringComparer.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    if (!item.IsCloze)
      yield break;

    var sentences      = TextTools.Sentences(item.Question);
    var clozeSentences = sentences.Where(s => s.Contains('[', StringComparison.Ordinal)).ToList();

    foreach (var sentence in clozeSentences.Count > 0 ? clozeSentences : sentences.Take(1))
    {
      var firstWord = TextTools.FirstWord(sentence);

      if (!Pronouns.Contains(firstWord))
        continue;

      yield return Finding(
        FindingSeverity.Advice,
        $"The cloze sentence {TextTools.Quote(sentence)} starts with \"{firstWord}\". Out of context, it can be unclear who or what that is. "
        + "You could name it or add a short context cue.");
      yield break;
    }
  }
}
