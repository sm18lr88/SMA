// Builds the plain-text item model from the HTML of the question and answer components.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using Text;

/// <summary>The raw content of an item: the HTML of its question components and of its answer components.</summary>
/// <param name="QuestionHtml">The HTML (or HTML-encoded text) of each question component.</param>
/// <param name="AnswerHtml">The HTML (or HTML-encoded text) of each answer component.</param>
/// <param name="ReferencesReadable">Whether the item has HTML components, which is where SuperMemo keeps references.</param>
public sealed record ItemHtml(IReadOnlyList<string> QuestionHtml, IReadOnlyList<string> AnswerHtml, bool ReferencesReadable);

/// <summary>Converts <see cref="ItemHtml" /> into a <see cref="FormulationItem" />.</summary>
public static class ItemModelBuilder
{
  /// <summary>Builds the item model: plain texts, cloze placeholders and deletions, list entries, and references.</summary>
  public static FormulationItem Build(ItemHtml html)
  {
    ArgumentNullException.ThrowIfNull(html);

    var questionHtml = string.Join("<br>", html.QuestionHtml);
    var answerHtml   = string.Join("<br>", html.AnswerHtml);
    var question     = HtmlText.ToPlainText(questionHtml);
    var answer       = HtmlText.ToPlainText(answerHtml);

    // A cloze hint replaces "[...]" with a word in brackets, inside SuperMemo's cloze span.
    var hints        = HtmlText.ClozeSpans(questionHtml).Count(s => !HtmlText.IsPlaceholder(s));
    var placeholders = HtmlText.CountPlaceholders(question) + hints;
    var isCloze      = placeholders > 0;

    var references = html.ReferencesReadable
      ? ReferenceParser.ParseFirst(html.QuestionHtml.Concat(html.AnswerHtml)) ?? new ItemReferences()
      : null;

    return new FormulationItem
    {
      Question              = question,
      Answer                = answer,
      IsCloze               = isCloze,
      ClozePlaceholderCount = placeholders,
      ClozeDeletions        = isCloze ? FindDeletions(question, answer, answerHtml, placeholders) : [],
      AnswerListItems       = HtmlText.ListItems(answerHtml),
      References            = references,
    };
  }

  private static IReadOnlyList<string> FindDeletions(string question, string answer, string answerHtml, int placeholders)
  {
    var spans = HtmlText.ClozeSpans(answerHtml);

    if (spans.Count > 0)
      return spans;

    if (answer.Length == 0)
      return [];

    if (placeholders == 1 && DeletionFromContext(question, answer) is { } deletion)
      return [deletion];

    return [answer];
  }

  // Some templates repeat the whole sentence in the answer; the deletion is then the text that replaces the placeholder.
  private static string? DeletionFromContext(string question, string answer)
  {
    if (HtmlText.FindPlaceholder(question) is not { } placeholder)
      return null;

    var prefix = question[..placeholder.Index].TrimEnd();
    var suffix = question[(placeholder.Index + placeholder.Length)..].TrimStart();

    if (prefix.Length + suffix.Length == 0 || answer.Length <= prefix.Length + suffix.Length)
      return null;

    if (!answer.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
      || !answer.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
      return null;

    var middle = answer[prefix.Length..^suffix.Length].Trim();

    return middle.Length > 0 ? middle : null;
  }
}
