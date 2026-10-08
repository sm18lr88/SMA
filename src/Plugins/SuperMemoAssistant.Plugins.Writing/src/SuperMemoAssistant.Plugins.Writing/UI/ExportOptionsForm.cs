using Forge.Forms.Annotations;
using SuperMemoAssistant.Plugins.Writing.Export;

namespace SuperMemoAssistant.Plugins.Writing.UI
{
  /// <summary>The options dialog of "Compile the current branch". It starts from the settings.</summary>
  [Form(Mode = DefaultFields.None)]
  [Title("Compile the current branch")]
  [DialogAction(CancelAction,
                "Cancel",
                IsCancel = true)]
  [DialogAction(CompileAction,
                "Compile",
                IsDefault = true,
                Validates = true)]
  public sealed class ExportOptionsForm
  {
    public const string CancelAction  = "cancel";
    public const string CompileAction = "compile";

    public ExportOptionsForm(WritingCfg config)
    {
      Format           = config.DefaultFormat;
      IncludeItems     = config.IncludeItems;
      IncludeTitlePage = config.IncludeTitlePage;
      SkippedTitles    = config.SkippedTitles;
    }

    [Field(Name = "Output format")]
    [SelectFrom(typeof(OutputFormat))]
    public OutputFormat Format { get; set; }

    [Field(Name = "Include items (questions and answers)")]
    public bool IncludeItems { get; set; }

    [Field(Name = "Include a title page (root title and references)")]
    public bool IncludeTitlePage { get; set; }

    [Field(Name = "Skip branches with these titles (one per line)")]
    [MultiLine]
    public string SkippedTitles { get; set; } = string.Empty;

    public CompileOptions ToOptions()
    {
      return new CompileOptions
      {
        Format           = Format,
        IncludeItems     = IncludeItems,
        IncludeTitlePage = IncludeTitlePage,
        SkippedTitles    = CompileOptions.ParseTitleList(SkippedTitles),
      };
    }
  }
}
