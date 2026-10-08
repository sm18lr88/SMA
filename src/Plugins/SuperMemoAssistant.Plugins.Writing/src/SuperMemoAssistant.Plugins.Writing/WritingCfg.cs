using System.ComponentModel;
using Forge.Forms.Annotations;
using Newtonsoft.Json;
using SuperMemoAssistant.Plugins.Writing.Export;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Sys.ComponentModel;

namespace SuperMemoAssistant.Plugins.Writing
{
  [Form(Mode = DefaultFields.None)]
  [Title("Writing Settings",
         IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel",
                "Cancel",
                IsCancel = true)]
  [DialogAction("save",
                "Save",
                IsDefault = true,
                Validates = true)]
  public class WritingCfg : CfgBase<WritingCfg>, INotifyPropertyChangedEx
  {
    public const double DefaultPriority = 30;

    [Field(Name = "Default output format")]
    [SelectFrom(typeof(OutputFormat))]
    public OutputFormat DefaultFormat { get; set; } = OutputFormat.Markdown;

    [Field(Name = "Include items (questions and answers) in compiled documents")]
    public bool IncludeItems { get; set; }

    [Field(Name = "Include a title page (root title and references)")]
    public bool IncludeTitlePage { get; set; } = true;

    [Field(Name = "Skip branches with these titles (one per line)")]
    [MultiLine]
    public string SkippedTitles { get; set; } = string.Join("\n", CompileOptions.DefaultSkippedTitles);

    [Field(Name = "Default priority of imported topics (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double DefaultImportPriority { get; set; } = DefaultPriority;

    [JsonIgnore]
    public bool IsChanged { get; set; }

    public override string ToString()
    {
      return "Writing";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
