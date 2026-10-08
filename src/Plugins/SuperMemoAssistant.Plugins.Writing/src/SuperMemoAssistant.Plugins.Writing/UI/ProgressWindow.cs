using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace SuperMemoAssistant.Plugins.Writing.UI
{
  /// <summary>A small progress window. Its Cancel button, Escape, or closing the window cancels the work.</summary>
  internal sealed class ProgressWindow : Window
  {
    private readonly ProgressBar _bar  = new() { Height = 18, IsIndeterminate = true };
    private readonly TextBlock   _text = new() { Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap };

    public ProgressWindow(string title, CancellationTokenSource cancellation)
    {
      Title                 = title;
      Width                 = 420;
      SizeToContent         = SizeToContent.Height;
      ResizeMode            = ResizeMode.NoResize;
      WindowStartupLocation = WindowStartupLocation.CenterScreen;
      Topmost               = true;

      var cancel = new Button
      {
        Content             = "Cancel",
        Width               = 90,
        Margin              = new Thickness(0, 12, 0, 0),
        HorizontalAlignment = HorizontalAlignment.Right,
        IsCancel            = true,
      };

      cancel.Click += (_, _) =>
      {
        cancel.IsEnabled = false;
        _text.Text       = "Cancelling...";
        cancellation.Cancel();
      };

      Closing += (_, _) => cancellation.Cancel();

      var panel = new StackPanel { Margin = new Thickness(16) };
      panel.Children.Add(_text);
      panel.Children.Add(_bar);
      panel.Children.Add(cancel);
      Content = panel;
    }

    /// <summary>Shows a message, and a determinate bar when <paramref name="total" /> is known.</summary>
    public void Report(string message, int done, int? total)
    {
      _text.Text = message;

      if (total is > 0)
      {
        _bar.IsIndeterminate = false;
        _bar.Maximum         = total.Value;
        _bar.Value           = done;
      }
      else
      {
        _bar.IsIndeterminate = true;
      }
    }
  }
}
