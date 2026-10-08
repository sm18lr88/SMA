// Process entry point: Velopack install/update hooks, SMA service bootstrap, then the WPF application.
namespace SuperMemoAssistant
{
  using System;
  using System.Windows;
  using Microsoft.Toolkit.Uwp.Notifications;
  using Velopack;

  public static class Program
  {
    [STAThread]
    public static int Main()
    {
      // Must run first: Velopack handles install/uninstall/update hooks here and may exit the process.
      VelopackApp.Build()
                 .OnFirstRun(_ => MessageBox.Show("SuperMemo Assistant has been successfully installed.", "Installation success"))
                 .Run();

      AppBootstrap.Initialize();
      ToastNotificationManagerCompat.OnActivated += ToastActivationHandler.Handle;

      var app = new App();
      app.InitializeComponent();
      return app.Run();
    }
  }
}
