namespace SuperMemoAssistant.Tests.Commands;

using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.SMA.Commands;
using Xunit;

/// <summary>Renders the palette panel off-screen, through its real XAML and bindings, without showing a window.</summary>
public sealed class CommandPaletteWindowTests
{
  private static readonly PaletteEntry[] Entries =
  [
    new(new PaletteCommand("Books", "Books", "Import", "Import a book or Kindle highlights", "Ctrl+Alt+Shift+K", HotKeyScopes.SM), () => { }),
    new(new PaletteCommand("SuperMemoAssistant", "Formulation", "Settings:F", "Open the Formulation settings", null, HotKeyScopes.Global), () => { }),
    new(new PaletteCommand("SuperMemoAssistant", "SMA", "Settings", "Show settings window", "Ctrl+Alt+Shift+O", HotKeyScopes.Global), () => { }),
  ];

  [Fact]
  public void ThePanelShowsTheRankedCommandsWithTheirPluginsAndHotKeys()
  {
    var (rows, empty) = RunOnStaThread(() => Render("settings", "palette-results"));

    Assert.Equal(2, rows);
    Assert.False(empty);
  }

  [Fact]
  public void ThePanelSaysSoWhenNoCommandMatches()
  {
    var (rows, empty) = RunOnStaThread(() => Render("zzz", "palette-empty"));

    Assert.Equal(0, rows);
    Assert.True(empty);
  }

  private static (int Rows, bool EmptyTextShown) Render(string query, string name)
  {
    var model  = new CommandPaletteModel(Entries, _ => 0, PaletteContext.ElementWindow) { Query = query };
    var window = new CommandPaletteWindow(model);
    var panel  = window.Panel;

    panel.Measure(new Size(window.Width, double.PositiveInfinity));
    panel.Arrange(new Rect(0, 0, window.Width, panel.DesiredSize.Height));
    panel.UpdateLayout();
    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);

    var frame = new RenderTargetBitmap((int)Math.Ceiling(panel.ActualWidth), (int)Math.Ceiling(panel.ActualHeight), 96, 96,
                                       PixelFormats.Pbgra32);
    frame.Render(panel);
    Save(frame, name);

    var emptyText = (UIElement)window.FindName("EmptyText");
    window.Close();
    return (model.Results.Count, emptyText.Visibility == Visibility.Visible);
  }

  private static void Save(BitmapSource frame, string name)
  {
    if (Environment.GetEnvironmentVariable("SMA_PALETTE_PNG") is not { Length: > 0 } folder)
      return;

    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(frame));
    using var file = File.Create(Path.Combine(folder, name + ".png"));
    encoder.Save(file);
  }

  private static T RunOnStaThread<T>(Func<T> action)
  {
    T result = default!;
    ExceptionDispatchInfo? failure = null;

    var thread = new Thread(() =>
    {
      try
      {
        result = action();
      }
      catch (Exception ex)
      {
        failure = ExceptionDispatchInfo.Capture(ex);
      }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromMinutes(1)), "The palette did not render within a minute.");
    failure?.Throw();
    return result;
  }
}
