namespace SuperMemoAssistant.Plugins.Books.UI
{
  using System.ComponentModel;
  using System.Windows.Input;
  using MahApps.Metro.Controls;

  /// <summary>The dialog that previews and imports a book or a Kindle clippings file.</summary>
  public partial class BookImportWindow : MetroWindow
  {
    private readonly BookImportViewModel _viewModel;

    /// <summary>Creates the dialog.</summary>
    public BookImportWindow(BooksCfg config)
    {
      _viewModel  = new BookImportViewModel(config);
      DataContext = _viewModel;

      InitializeComponent();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
      if (_viewModel.IsBusy == false)
        return;

      _viewModel.Cancel();
      e.Cancel = true;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Escape)
        return;

      e.Handled = true;

      if (_viewModel.IsBusy)
        _viewModel.Cancel();
      else
        Close();
    }
  }
}
