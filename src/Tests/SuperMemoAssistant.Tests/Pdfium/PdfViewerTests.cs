namespace SuperMemoAssistant.Tests.Pdfium;

using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SuperMemoAssistant.Pdfium.Wpf;
using Xunit;

/// <summary>Drives the WPF viewer off-screen through its real render path: layout, progressive canvas, and compositing.</summary>
public sealed class PdfViewerTests
{
  private const int Width = 700, Height = 900;

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void RendersTheFirstPage(bool progressive)
  {
    var pixels = RunOnStaThread(() =>
    {
      var viewer = new PdfViewer { UseProgressiveRender = progressive };
      viewer.LoadDocument(TestPdf.Build());
      viewer.Measure(new Size(Width, Height));
      viewer.Arrange(new Rect(0, 0, Width, Height));

      RenderTargetBitmap frame = null!;
      for (var pass = 0; pass < 50; pass++)
      {
        viewer.InvalidateVisual();
        viewer.UpdateLayout();
        frame = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        frame.Render(viewer);
      }

      SaveForInspection(frame, progressive);
      viewer.CloseDocument();

      var buffer = new byte[Width * Height * 4];
      frame.CopyPixels(buffer, Width * 4, 0);
      return buffer;
    });

    int white = 0, dark = 0, red = 0;
    for (var i = 0; i < pixels.Length; i += 4)
    {
      byte b = pixels[i], g = pixels[i + 1], r = pixels[i + 2];
      if (r > 240 && g > 240 && b > 240) white++;
      else if (r < 80 && g < 80 && b < 80) dark++;
      else if (r > 200 && g < 60 && b < 60) red++;
    }

    Assert.True(white > Width * Height / 4, $"The page background should be white ({white} white pixels).");
    Assert.True(dark > 200, $"The page text should be visible ({dark} dark pixels).");
    Assert.True(red > 200, $"The red image cell should be visible ({red} red pixels).");
  }

  private static void SaveForInspection(BitmapSource frame, bool progressive)
  {
    if (Environment.GetEnvironmentVariable("SMA_PDF_VIEWER_PNG") is not { Length: > 0 } folder)
      return;

    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(frame));
    using var file = File.Create(Path.Combine(folder, progressive ? "viewer-progressive.png" : "viewer-direct.png"));
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
    Assert.True(thread.Join(TimeSpan.FromMinutes(1)), "The viewer did not finish rendering within a minute.");
    failure?.Throw();
    return result;
  }
}
