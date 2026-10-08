using System.Windows.Input;
using SuperMemoAssistant.Interop.Plugins;
using SuperMemoAssistant.Plugins.Writing.UI;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.IO.HotKeys;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Plugins.Writing
{
  /// <summary>
  ///   Incremental writing: compiles the branch of the current element into one Markdown or HTML document, and imports
  ///   a Markdown outline as a branch of topics under the current element.
  /// </summary>
  public class WritingPlugin : SMAPluginBase<WritingPlugin>
  {
    public WritingCfg Config { get; private set; } = new();

    /// <inheritdoc />
    public override string Name => "Writing";

    public override bool HasSettings => true;

    /// <inheritdoc />
    protected override void OnPluginInitialized()
    {
      Config = Svc.Configuration.Load<WritingCfg>() ?? new WritingCfg();

      base.OnPluginInitialized();
    }

    /// <inheritdoc />
    protected override void OnSMStarted(bool wasSMAlreadyStarted)
    {
      Svc.HotKeyManager
         .RegisterGlobal(
           "CompileBranch",
           "Compile the current branch into one document",
           HotKeyScopes.SM,
           new HotKey(Key.W, KeyModifiers.CtrlAltShift),
           () => Dialogs.RunOnUiThread(new ExportCommand(Config).RunAsync, ExportCommand.Title))
         .RegisterGlobal(
           "ImportOutline",
           "Import a Markdown outline as a branch",
           HotKeyScopes.SM,
           new HotKey(Key.M, KeyModifiers.CtrlAltShift),
           () => Dialogs.RunOnUiThread(new ImportCommand(Config).RunAsync, ImportCommand.Title));

      base.OnSMStarted(wasSMAlreadyStarted);
    }

    /// <inheritdoc />
    public override void ShowSettings()
    {
      ConfigurationWindow.ShowAndActivate("Writing Settings", HotKeyManager.Instance, Config);
    }
  }
}
