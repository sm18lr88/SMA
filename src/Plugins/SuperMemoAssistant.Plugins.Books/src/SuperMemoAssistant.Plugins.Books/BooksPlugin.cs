namespace SuperMemoAssistant.Plugins.Books
{
  using System.Windows;
  using System.Windows.Input;
  using SuperMemoAssistant.Extensions;
  using SuperMemoAssistant.Interop.Plugins;
  using SuperMemoAssistant.Plugins.Books.UI;
  using SuperMemoAssistant.Services;
  using SuperMemoAssistant.Services.IO.HotKeys;
  using SuperMemoAssistant.Services.IO.Keyboard;
  using SuperMemoAssistant.Services.UI.Configuration;
  using SuperMemoAssistant.Sys.IO.Devices;

  /// <summary>Imports EPUB books and Kindle highlights into SuperMemo as incremental-reading topics.</summary>
  public class BooksPlugin : SMAPluginBase<BooksPlugin>
  {
    private BookImportWindow? _window;

    /// <summary>The plugin settings.</summary>
    public BooksCfg Config { get; private set; } = new();

    /// <inheritdoc />
    public override string Name => "Books";

    /// <inheritdoc />
    public override bool HasSettings => true;

    /// <inheritdoc />
    protected override void OnPluginInitialized()
    {
      Config = Svc.Configuration.Load<BooksCfg>() ?? new BooksCfg();

      base.OnPluginInitialized();
    }

    /// <inheritdoc />
    protected override void OnSMStarted(bool wasSMAlreadyStarted)
    {
      Svc.HotKeyManager.RegisterGlobal(
        "ImportBook",
        "Import a book or Kindle highlights",
        HotKeyScopes.SM,
        new HotKey(Key.K, KeyModifiers.CtrlAltShift),
        ShowImportWindow);

      base.OnSMStarted(wasSMAlreadyStarted);
    }

    /// <inheritdoc />
    public override void ShowSettings()
    {
      ConfigurationWindow.ShowAndActivate("Books Settings", HotKeyManager.Instance, Config);
    }

    private void ShowImportWindow()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (_window == null)
        {
          _window        =  new BookImportWindow(Config);
          _window.Closed += (_, _) => _window = null;
        }

        _window.ShowAndActivate();
      });
    }
  }
}
