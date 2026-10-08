namespace SuperMemoAssistant.UI.Settings
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Automation;
  using System.Windows.Controls;
  using System.Windows.Input;
  using System.Windows.Media;
  using Interop.SMA;
  using Services.IO.HotKeys;
  using SMA.Commands;
  using Sys.IO.Devices;
  using Sys.Windows.Input;
  using MAHotKey = MahApps.Metro.Controls.HotKey;

  /// <summary>Lets the user change the hotkeys of SMA. Plugins list their own hotkeys in their settings.</summary>
  public partial class HotKeySettings : UserControl
  {
    public HotKeySettings()
    {
      var manager = SMA.Core.HotKeyManager;

      HotKeys = manager.HotKeys
                       .OrderBy(h => h.Description)
                       .Select(h => new HotKeyBinding(manager, SMA.Core.SMA.Commands, h))
                       .ToList();

      InitializeComponent();
    }

    public IReadOnlyList<HotKeyBinding> HotKeys { get; }

    private void HotKeyBox_Loaded(object sender, RoutedEventArgs e)
    {
      // Screen readers and UI Automation reach the text field inside the box, so the field carries the action name.
      if (sender is DependencyObject box && ((FrameworkElement)sender).DataContext is HotKeyBinding row && FindTextBox(box) is { } text)
        AutomationProperties.SetName(text, row.Data.Description);
    }

    private static TextBox FindTextBox(DependencyObject parent)
    {
      for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);

        if (child is TextBox text)
          return text;

        if (FindTextBox(child) is { } nested)
          return nested;
      }

      return null;
    }
  }

  /// <summary>One row of <see cref="HotKeySettings" />: shows a hotkey and applies the key that the user types.</summary>
  public sealed class HotKeyBinding : INotifyPropertyChanged, IDataErrorInfo
  {
    private readonly HotKeyManager   _manager;
    private readonly CommandRegistry _commands;

    private MAHotKey _hotKey;
    private string   _conflict;

    public HotKeyBinding(HotKeyManager manager, CommandRegistry commands, HotKeyData data)
    {
      _manager  = manager;
      _commands = commands;
      Data      = data;
      _hotKey   = ToMahApps(data.ActualHotKey);

      ResetCommand = new RelayCommand(() => HotKey = ToMahApps(Data.DefaultHotKey));
    }

    public HotKeyData Data { get; }

    public string DefaultHotKey => Data.DefaultHotKey?.ToString();

    public ICommand ResetCommand { get; }

    public MAHotKey HotKey
    {
      get => _hotKey;
      set
      {
        _hotKey = value;

        // Escape without a modifier clears the box, as in the plugin settings.
        if (value == null || value.ModifierKeys == ModifierKeys.None && value.Key == Key.Escape)
        {
          _conflict         = null;
          Data.ActualHotKey = null;
          return;
        }

        var hotKey = new HotKey(value.Key, (KeyModifiers)value.ModifierKeys);
        _conflict = ConflictOf(hotKey);

        if (_conflict == null)
          Data.ActualHotKey = hotKey;
      }
    }

    /// <inheritdoc />
    public string this[string columnName] =>
      columnName == nameof(HotKey) && _conflict != null
        ? $"Already used by {_conflict}"
        : null;

    /// <inheritdoc />
    public string Error => string.Empty;

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>UI Automation names a row after its text.</summary>
    public override string ToString() => Data.Description;

    /// <summary>Plugins run in their own processes, so their hotkeys are known through their palette commands.</summary>
    private string ConflictOf(HotKey hotKey)
    {
      var owner = _manager.Match(hotKey);
      if (owner != null && owner != Data)
        return $"\"{owner.Description}\"";

      var text    = hotKey.ToString();
      var command = _commands.Snapshot()
                             .Select(e => e.Command)
                             .FirstOrDefault(c => c.Owner != PaletteCommand.SmaOwner && c.HotKey == text);

      return command == null ? null : $"the {command.OwnerName} plugin";
    }

    private static MAHotKey ToMahApps(HotKey hotKey) =>
      hotKey == null ? null : new MAHotKey(hotKey.Key, (ModifierKeys)hotKey.Modifiers);
  }
}
