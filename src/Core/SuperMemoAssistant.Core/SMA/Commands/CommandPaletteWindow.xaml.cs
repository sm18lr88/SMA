namespace SuperMemoAssistant.SMA.Commands
{
  using System;
  using System.ComponentModel;
  using System.Windows;
  using System.Windows.Input;

  /// <summary>The palette window. It closes when it loses focus, and reports the chosen command through <see cref="Chosen" />.</summary>
  public partial class CommandPaletteWindow : Window
  {
    private const int PageSize = 8;

    private readonly CommandPaletteModel _model;
    private          bool                _isClosing;

    public CommandPaletteWindow(CommandPaletteModel model)
    {
      _model      = model ?? throw new ArgumentNullException(nameof(model));
      DataContext = model;

      InitializeComponent();

      _model.PropertyChanged += Model_PropertyChanged;
      UpdateEmptyText();
      Loaded += (_, _) => QueryBox.Focus();
    }

    /// <summary>The command that the user chose, or null when the palette closed without a choice.</summary>
    public PaletteEntry Chosen { get; private set; }

    /// <summary>The panel to render in tests, without showing the window.</summary>
    internal FrameworkElement Panel => Root;

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.Down:     _model.MoveSelection(1); break;
        case Key.Up:       _model.MoveSelection(-1); break;
        case Key.PageDown: _model.MoveSelection(PageSize); break;
        case Key.PageUp:   _model.MoveSelection(-PageSize); break;
        case Key.Enter:    Choose(_model.Selected); break;
        case Key.Escape:   Close(); break;
        default:           return;
      }

      ResultsList.ScrollIntoView(_model.Selected);
      e.Handled = true;
    }

    private void ResultsList_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
        Choose(_model.Selected);
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
      if (_isClosing == false)
        Close();
    }

    private void Choose(PaletteEntry entry)
    {
      if (entry == null)
        return;

      Chosen = entry;
      Close();
    }

    private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(CommandPaletteModel.Results))
        UpdateEmptyText();
    }

    private void UpdateEmptyText()
    {
      EmptyText.Visibility   = _model.Results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
      ResultsList.Visibility = _model.Results.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    protected override void OnClosing(CancelEventArgs e)
    {
      // Closing deactivates the window, and WPF throws when Close is called again during a close.
      _isClosing = true;
      base.OnClosing(e);
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
      _model.PropertyChanged -= Model_PropertyChanged;
      base.OnClosed(e);
    }
  }
}
