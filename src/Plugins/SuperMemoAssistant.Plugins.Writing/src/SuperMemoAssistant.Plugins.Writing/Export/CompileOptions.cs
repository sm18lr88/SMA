using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>The file format of a compiled document.</summary>
  public enum OutputFormat
  {
    Markdown,
    Html,
  }

  /// <summary>How a branch is compiled into one document.</summary>
  public sealed record CompileOptions
  {
    /// <summary>The default titles of branches that are not part of the text, such as a to-do list.</summary>
    public static IReadOnlyList<string> DefaultSkippedTitles { get; } = ["TO-DO", "TODO"];

    public OutputFormat Format { get; init; } = OutputFormat.Markdown;

    /// <summary>Whether items become sections. Items are questions and answers, so the default is no.</summary>
    public bool IncludeItems { get; init; }

    /// <summary>Whether the document starts with the root title and the root references.</summary>
    public bool IncludeTitlePage { get; init; } = true;

    /// <summary>Branches whose title matches one of these (case and outer spaces ignored) are left out.</summary>
    public IReadOnlyList<string> SkippedTitles { get; init; } = DefaultSkippedTitles;

    /// <summary>The number of elements read from the tree source in one call.</summary>
    public int BatchSize { get; init; } = 50;

    public bool IsSkippedTitle(string title)
    {
      var trimmed = title.Trim();

      return SkippedTitles.Any(s => string.Equals(s.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Splits a settings text (one title per line, or titles separated by commas) into titles.</summary>
    public static IReadOnlyList<string> ParseTitleList(string text)
    {
      return text.Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
  }
}
