#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion




namespace SuperMemoAssistant
{
  using System;
  using System.Diagnostics.CodeAnalysis;
  using System.Threading.Tasks;
  using System.Windows;
  using Anotar.Serilog;
  using CommandLine;
  using Hardcodet.Wpf.TaskbarNotification;
  using Installer;
  using Interop;
  using Interop.SuperMemo.Core;
  using Models;
  using Plugins;
  using Services.UI.Extensions;
  using Setup;
  using SMA.Utils;
  using Sys.Windows;
  using UI;
  using Utils;

  /// <summary>Interaction logic for App.xaml</summary>
  public partial class App : Application
  {
    private static readonly TimeSpan PluginInitializationTimeout = TimeSpan.FromSeconds(30);

    #region Properties & Fields - Non-Public

    private SplashScreenWindow _splashScreen;

    private TaskbarIcon _taskbarIcon;

    #endregion




    #region Methods Impl

    /// <summary>The application main stopping point</summary>
    /// <param name="e"></param>
    protected override void OnExit(ExitEventArgs e)
    {
      SMAUpdater.Instance.WaitForIdle();

      _taskbarIcon?.Dispose();
      _splashScreen?.Close();
      _splashScreen = null;

      SMA.Core.Logger?.Shutdown();

      base.OnExit(e);
    }

    #endregion




    #region Methods

    /// <summary>The application main starting point</summary>
    /// <param name="o"></param>
    /// <param name="e"></param>
    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "WPF API")]
    private async void Application_Startup(object           o,
                                           StartupEventArgs e)
    {
      _taskbarIcon = (TaskbarIcon)FindResource("TbIcon");

      if (ApplicationSingleton.IsOnlyInstance(_ => LogTo.Warning("SuperMemoAssistant is already running. Exiting."), e.Args) == false)
      {
        Shutdown(SMAExitCodes.ExitCodeSMAAlreadyRunning);
        return;
      }

      try
      {
        if (Parser.Default.ParseArguments<SMAParameters>(e.Args) is Parsed<SMAParameters> parsed)
          await LoadAppAsync(parsed.Value).ConfigureAwait(true);

        else
          Shutdown(SMAExitCodes.ExitCodeParametersError);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "An unknown exception occurred during SMA Application startup");

        Shutdown(SMAExitCodes.ExitCodeSMAStartupError);
      }
    }

    private async Task LoadAppAsync(SMAParameters args)
    {
      //
      // Make sure assemblies are available, and SMA is installed in "%LocalAppData%\SuperMemoAssistant"
      if (AssemblyCheck.CheckRequired(out var errMsg) == false || CheckSMALocation(out errMsg) == false)
      {
        LogTo.Warning(errMsg);
        await errMsg.ErrorMsgBox().ConfigureAwait(false);

        Shutdown(SMAExitCodes.ExitCodeDependencyError);
        return;
      }

      //
      // Load main configuration files
      var coreCfg = await LoadCoreConfig().ConfigureAwait(true);

      if (coreCfg == null)
      {
        errMsg = "The SMA core configuration file could not be loaded.";
        LogTo.Warning(errMsg);
        await errMsg.ErrorMsgBox().ConfigureAwait(false);

        Shutdown(SMAExitCodes.ExitCodeConfigError);
        return;
      }

      SMA.Core.CoreConfig = coreCfg;

      //
      // Initialize the plugin manager
      await SMAPluginManager.Instance.InitializeAsync().ConfigureAwait(true);

      //
      // Check if SMA is setup, and run the setup wizard if it isn't
      if (SMASetup.Run(coreCfg) == false)
      {
        LogTo.Warning("SMA Setup canceled. Exiting.");

        Shutdown(SMAExitCodes.ExitCodeSMASetupError);
        return;
      }

      //
      // Start plugins
      var pluginStartTask = SMAPluginManager.Instance.StartPlugins().ConfigureAwait(true);

      //
      // (Optional) Start the debugging tool Key logger (logs key strokes with modifiers, e.g. ctrl, alt, ..)
      if (args.KeyLogger)
        SMA.Core.KeyboardHotKey.MainCallback = hk => LogTo.Debug("Key pressed: {Hk}", hk);

      //
      // Show the change logs if necessary
      ChangeLogWindow.ShowIfUpdated(coreCfg);

      //
      // Determine which collection to open
      SMCollection smCollection = null;
      var          selectionWdw = new CollectionSelectionWindow(coreCfg);

      // Try to open command line collection, if one was passed
      if (args.CollectionKnoPath != null && selectionWdw.ValidateSuperMemoPath())
      {
        smCollection = new SMCollection(args.CollectionKnoPath, DateTime.Now);

        if (selectionWdw.ValidateCollection(smCollection) == false)
          smCollection = null;
      }

      // No valid collection passed, show selection window
      if (smCollection == null)
      {
        selectionWdw.ShowDialog();

        smCollection = selectionWdw.Collection;
      }

      //
      // If a collection was selected, start SMA
      if (smCollection != null)
      {
        _splashScreen = new SplashScreenWindow();
        _splashScreen.Show();

        SMA.Core.SMA.OnSMStartingInternalEvent += OnSMStartingEventAsync;
        SMA.Core.SMA.OnSMStoppedInternalEvent += OnSMStoppedEvent;

        // Wait for plugins to start, and then to finish initializing: launch hooks are registered during initialization
        await pluginStartTask;
        await SMAPluginManager.Instance.WaitForPluginsInitializedAsync(PluginInitializationTimeout).ConfigureAwait(true);

        Exception ex;

        if ((ex = await SMA.Core.SMA.StartAsync(smCollection).ConfigureAwait(true)) != null)
        {
          _splashScreen?.Close();
          _splashScreen = null;

          await $"SMA failed to start: {ex.Message}".ErrorMsgBox().ConfigureAwait(false);

          Shutdown(SMAExitCodes.ExitCodeSMAStartupError);

          return;
        }

        // Keep isolated app-*-test builds from replacing themselves during validation.
        if (SMAExecutableInfo.Instance.IsDev == false &&
            SMAExecutableInfo.Instance.DirectoryPath.FullPath.EndsWith("-test", StringComparison.OrdinalIgnoreCase) == false)
          await SMAUpdater.Instance.UpdateAsync().ConfigureAwait(false);
      }
      else
      {
        Shutdown();
      }
    }

    /// <summary>Called when SuperMemo's Element window is loaded</summary>
    private void ElementWdw_OnAvailable()
    {
      Dispatcher.Invoke(() =>
      {
        _splashScreen?.Close();
        _splashScreen = null;
      });

      SMA.Core.SM.UI.ElementWdw.OnAvailable -= ElementWdw_OnAvailable;
    }

    private Task OnSMStartingEventAsync(object sender, SMEventArgs eventArgs)
    {
      SMA.Core.SM.UI.ElementWdw.OnAvailableInternal += ElementWdw_OnAvailable;

      SMAUI.Initialize();

      return Task.CompletedTask;
    }

    private void OnSMStoppedEvent(object sender, SMProcessEventArgs e)
    {
      try
      {
        LogTo.Debug("Cleaning up {Name}", GetType().Name);

        Dispatcher.Invoke(Shutdown);
      }
      finally
      {
        LogTo.Debug("Cleaning up {Name}... Done", GetType().Name);
      }
    }

    /// <summary>Validates the location of SMA on disk</summary>
    /// <param name="error">Error result (if any)</param>
    /// <returns>Whether SMA is in the valid location on disk</returns>
    private bool CheckSMALocation(out string error)
    {
      error = null;

      if (SMAExecutableInfo.Instance.IsPathLocalAppData || SMAExecutableInfo.Instance.IsDev)
        return true;

      error = $"SuperMemoAssistant should be installed in '{SMAFileSystem.AppRootDir}\\current' (or run from '{SMAFileSystem.AppRootDir}\\app-dev' for development)";

      return false;
    }

    #endregion
  }
}
