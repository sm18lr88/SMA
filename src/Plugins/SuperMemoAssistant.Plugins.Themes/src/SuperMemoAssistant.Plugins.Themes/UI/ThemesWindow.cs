// The settings window: switches, a searchable list of themes with color swatches, and Save. Built in code; all logic is in the view model.
namespace SuperMemoAssistant.Plugins.Themes.UI
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Markup;

  internal sealed class ThemesWindow : Window
  {
    private const string RowTemplate = """
      <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
        <Grid Margin="2">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="70" />
            <ColumnDefinition Width="70" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="50" />
            <ColumnDefinition Width="Auto" />
          </Grid.ColumnDefinitions>
          <CheckBox Grid.Column="0" Content="Install" IsChecked="{Binding Install, Mode=TwoWay}" VerticalAlignment="Center" />
          <CheckBox Grid.Column="1" Content="Active" IsChecked="{Binding IsActive, Mode=TwoWay}" VerticalAlignment="Center" />
          <TextBlock Grid.Column="2" Text="{Binding Name}" VerticalAlignment="Center" />
          <TextBlock Grid.Column="3" Text="{Binding Variant}" VerticalAlignment="Center" Opacity="0.6" />
          <ItemsControl Grid.Column="4" ItemsSource="{Binding Swatches}">
            <ItemsControl.ItemsPanel>
              <ItemsPanelTemplate><StackPanel Orientation="Horizontal" /></ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
              <DataTemplate><Border Width="16" Height="16" Margin="1" Background="{Binding}" BorderBrush="#40808080" BorderThickness="1" /></DataTemplate>
            </ItemsControl.ItemTemplate>
          </ItemsControl>
        </Grid>
      </DataTemplate>
      """;

    private static ThemesWindow? _open;

    private ThemesWindow(ThemesViewModel viewModel)
    {
      Title                 = "Themes";
      Width                 = 780;
      Height                = 720;
      MinWidth              = 560;
      WindowStartupLocation = WindowStartupLocation.CenterScreen;
      DataContext           = viewModel;
      Content               = BuildContent(viewModel);
      Closed               += (_, _) => _open = null;
    }

    public static void ShowSingle(ThemesViewModel viewModel)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (_open is not null)
        {
          _open.Activate();

          return;
        }

        _open = new ThemesWindow(viewModel);
        _open.Show();
      });
    }

    /// <summary>Builds the window for tests, without showing it.</summary>
    internal static ThemesWindow Create(ThemesViewModel viewModel) => new(viewModel);

    private UIElement BuildContent(ThemesViewModel vm)
    {
      var root = new Grid { Margin = new Thickness(14) };

      for (var i = 0; i < 6; i++)
        root.RowDefinitions.Add(new RowDefinition { Height = i == 3 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

      Place(root, 0, new TextBlock
      {
        Text         = "SuperMemo Assistant applies these settings the next time SuperMemo starts through it, because sm20.exe can only be rewritten while SuperMemo is closed. "
                       + "SMA always rebuilds sm20.exe from the original (a copy is kept in smcards-backups). Turning this off restores the original.",
        TextWrapping = TextWrapping.Wrap,
        Margin       = new Thickness(0, 0, 0, 10),
      });

      var options = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

      options.Children.Add(Check("Apply themes to SuperMemo", nameof(ThemesViewModel.Enabled)));
      options.Children.Add(Check("Theme the card area and the status bar", nameof(ThemesViewModel.ThemeElements)));
      options.Children.Add(Check("Cards follow a theme picked in Window > Themes at once (code patch, supported builds only)", nameof(ThemesViewModel.LiveSwitching)));
      Place(root, 1, options);

      Place(root, 2, BuildFilterRow());

      var list = new ListBox { ItemTemplate = (DataTemplate)XamlReader.Parse(RowTemplate), HorizontalContentAlignment = HorizontalAlignment.Stretch };

      VirtualizingPanel.SetIsVirtualizing(list, true);
      list.SetBinding(ItemsControl.ItemsSourceProperty, nameof(ThemesViewModel.Rows));
      Place(root, 3, list);

      var messages = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };

      messages.Children.Add(Message(nameof(ThemesViewModel.ImportMessage), 1.0));
      messages.Children.Add(Message(nameof(ThemesViewModel.Status), 0.75));
      Place(root, 4, messages);
      Place(root, 5, BuildButtons(vm));

      return root;
    }

    private static void Place(Grid grid, int row, UIElement element)
    {
      Grid.SetRow(element, row);
      grid.Children.Add(element);
    }

    private static TextBlock Message(string property, double opacity)
    {
      var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2), Opacity = opacity };

      text.SetBinding(TextBlock.TextProperty, new Binding(property) { Mode = BindingMode.OneWay });

      return text;
    }

    private static CheckBox Check(string text, string property)
    {
      var box = new CheckBox { Content = text, Margin = new Thickness(0, 2, 0, 2) };

      box.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding(property) { Mode = BindingMode.TwoWay });

      return box;
    }

    private static UIElement BuildFilterRow()
    {
      var row    = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
      var search = new TextBox { MinWidth = 260, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Search by name" };
      var filter = new ComboBox { Width = 90, ItemsSource = new[] { ThemesViewModel.AllVariants, "Dark", "Light" } };

      search.SetBinding(TextBox.TextProperty, new Binding(nameof(ThemesViewModel.Search)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
      filter.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, new Binding(nameof(ThemesViewModel.VariantFilter)) { Mode = BindingMode.TwoWay });

      DockPanel.SetDock(filter, Dock.Right);
      row.Children.Add(filter);
      row.Children.Add(search);

      return row;
    }

    private UIElement BuildButtons(ThemesViewModel vm)
    {
      var curated    = new Button { Content = "Check the curated set", Padding = new Thickness(10, 3, 10, 3) };
      var importFile = new Button { Content = "Import file...", Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0), ToolTip = "A base16/base24 .yaml or a VS Code theme .json" };
      var importDir  = new Button { Content = "Import folder...", Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0), ToolTip = "A folder of .yaml schemes, a VS Code extension, or an Obsidian theme folder" };
      var save    = new Button { Content = "Save", Width = 90, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
      var cancel  = new Button { Content = "Cancel", Width = 90, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };

      curated.Click    += (_, _) => vm.UseCuratedSet();
      importFile.Click += (_, _) =>
      {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Import a theme file", Filter = "Theme files (*.yaml;*.yml;*.json)|*.yaml;*.yml;*.json|All files (*.*)|*.*" };

        if (dialog.ShowDialog(this) == true)
          vm.Import(dialog.FileName);
      };
      importDir.Click += (_, _) =>
      {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Import themes from a folder" };

        if (dialog.ShowDialog(this) == true)
          vm.Import(dialog.FolderName);
      };
      save.Click    += (_, _) => { vm.Save(); Close(); };
      cancel.Click  += (_, _) => Close();

      var buttons = new DockPanel { Margin = new Thickness(0, 12, 0, 0), LastChildFill = false };

      DockPanel.SetDock(cancel, Dock.Right);
      DockPanel.SetDock(save, Dock.Right);
      buttons.Children.Add(cancel);
      buttons.Children.Add(save);
      buttons.Children.Add(curated);
      buttons.Children.Add(importFile);
      buttons.Children.Add(importDir);

      return buttons;
    }
  }
}
