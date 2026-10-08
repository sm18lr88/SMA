// Rule 19: volatile knowledge needs a date.
namespace SuperMemoAssistant.Plugins.Formulation.Rules;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Engine;
using Text;

/// <summary>Finds words such as "currently" or "latest" in items that have no year and no Date reference.</summary>
public sealed partial class DateStampingRule() : FormulationRule(RuleIds.DateStamping, "Provide date stamping", "date")
{
  /// <inheritdoc />
  public override IEnumerable<FormulationFinding> Check(FormulationItem item, FormulationOptions options)
  {
    if (item.References?.HasDate == true)
      yield break;

    var text = item.Question + "\n" + item.Answer;

    if (YearRegex().IsMatch(text))
      yield break;

    foreach (var sentence in TextTools.Sentences(text))
    {
      var word = VolatileRegex().Match(sentence);

      if (!word.Success)
        continue;

      yield return Finding(
        FindingSeverity.Advice,
        $"\"{word.Value}\" in {TextTools.Quote(sentence)} can become out of date. You could add the year when it was true, or a Date reference.");
      yield break;
    }
  }

  [GeneratedRegex(@"\b(?:currently|nowadays|now|today|latest|recent(?:ly)?|this\s+year)\b", RegexOptions.IgnoreCase)]
  private static partial Regex VolatileRegex();

  [GeneratedRegex(@"\b(?:1[5-9]\d{2}|2[01]\d{2})\b")]
  private static partial Regex YearRegex();
}
