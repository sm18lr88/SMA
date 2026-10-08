namespace SuperMemoAssistant.Plugins.Books
{
  using System.ComponentModel;
  using Forge.Forms.Annotations;
  using Newtonsoft.Json;
  using SuperMemoAssistant.Services.UI.Configuration;
  using SuperMemoAssistant.Sys.ComponentModel;

  /// <summary>The settings of the Books plugin.</summary>
  [Form(Mode = DefaultFields.None)]
  [Title("Books Settings", IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel", "Cancel", IsCancel = true)]
  [DialogAction("save", "Save", IsDefault = true, Validates = true)]
  public class BooksCfg : CfgBase<BooksCfg>, INotifyPropertyChangedEx
  {
    /// <summary>The default priority of an imported book topic.</summary>
    [Field(Name = "Default priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo, 0, StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo, 100, StrictValidation = true)]
    public double DefaultPriority { get; set; } = 30;

    /// <summary>How much lower the priority of each further chapter or highlight is, in percentage points.</summary>
    [Field(Name = "Chapter priority step (%)")]
    [Value(Must.BeGreaterThanOrEqualTo, 0, StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo, 10, StrictValidation = true)]
    public double ChapterPriorityStep { get; set; } = 0.2;

    /// <summary>EPUB documents with fewer text characters are merged into the previous chapter.</summary>
    [Field(Name = "Merge chapters shorter than (characters)")]
    [Value(Must.BeGreaterThanOrEqualTo, 0, StrictValidation = true)]
    public int MergeThreshold { get; set; } = 1500;

    /// <inheritdoc />
    [JsonIgnore]
    public bool IsChanged { get; set; }

    /// <inheritdoc />
    public override string ToString() => "Books";

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
