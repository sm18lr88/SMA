// The plain-text view of an item that the rules analyze.
namespace SuperMemoAssistant.Plugins.Formulation.Engine;

using System.Collections.Generic;

/// <summary>An item reduced to plain text. Build it with <see cref="ItemModelBuilder" /> or directly in tests.</summary>
public sealed record FormulationItem
{
  /// <summary>The question text, without HTML and without the reference block.</summary>
  public string Question { get; init; } = string.Empty;

  /// <summary>The answer text, without HTML and without the reference block.</summary>
  public string Answer { get; init; } = string.Empty;

  /// <summary>Whether the question contains at least one cloze placeholder.</summary>
  public bool IsCloze { get; init; }

  /// <summary>The number of cloze placeholders ("[...]" or a cloze hint) in the question.</summary>
  public int ClozePlaceholderCount { get; init; }

  /// <summary>The texts that the cloze placeholders hide. Empty for a question and answer item.</summary>
  public IReadOnlyList<string> ClozeDeletions { get; init; } = [];

  /// <summary>The entries of HTML lists in the answer.</summary>
  public IReadOnlyList<string> AnswerListItems { get; init; } = [];

  /// <summary>The item references, or null when the references could not be read.</summary>
  public ItemReferences? References { get; init; }

  /// <summary>The texts that the user must recall: the cloze deletions, or else the answer.</summary>
  public IReadOnlyList<string> RecalledTexts =>
    IsCloze && ClozeDeletions.Count > 0 ? ClozeDeletions : [Answer];
}

/// <summary>The SuperMemo references of an item. Empty values mean that the field is missing.</summary>
public sealed record ItemReferences(string Title = "", string Source = "", string Link = "", string Date = "")
{
  /// <summary>Whether the item names a Source or a Link.</summary>
  public bool HasSource => !string.IsNullOrWhiteSpace(Source) || !string.IsNullOrWhiteSpace(Link);

  /// <summary>Whether the item has a Date reference.</summary>
  public bool HasDate => !string.IsNullOrWhiteSpace(Date);
}
