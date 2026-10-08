// Formulation advisor: checks SuperMemo items against the 20 rules of formulating knowledge.
namespace SuperMemoAssistant.Plugins.Formulation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Anotar.Serilog;
using Engine;
using Extensions;
using Integration;
using Interop.Plugins;
using Interop.SuperMemo.Core;
using Interop.SuperMemo.Elements.Types;
using PluginManager.Remoting;
using Services;
using Services.IO.HotKeys;
using Services.IO.Keyboard;
using Services.ToastNotifications;
using Services.UI.Configuration;
using Sys.IO.Devices;
using UI;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
///   Checks the current item on demand (hotkey) or when it is displayed. It only reads the collection, and it never shows a
///   modal dialog.
/// </summary>
public sealed class FormulationPlugin : SMAPluginBase<FormulationPlugin>
{
  private const string CheckHotKeyId = "CheckFormulation";

  private static readonly HotKey DefaultCheckHotKey = new(Key.F, KeyModifiers.CtrlAltShift);

  private readonly FormulationAnalyzer         _analyzer = new();
  private readonly ElementNotificationThrottle _throttle = new();
  private readonly Lock                        _autoCheckLock = new();
  private          CancellationTokenSource?    _autoCheckCts;
  private          FindingsWindow?             _findingsWindow;

  /// <summary>The user settings.</summary>
  public FormulationCfg Config { get; private set; } = new();

  /// <inheritdoc />
  public override string Name => "Formulation";

  /// <inheritdoc />
  public override bool HasSettings => true;

  /// <inheritdoc />
  protected override void OnPluginInitialized()
  {
    Config = Svc.Configuration.Load<FormulationCfg>() ?? new FormulationCfg();

    base.OnPluginInitialized();
  }

  /// <inheritdoc />
  protected override void OnSMStarted(bool wasSMAlreadyStarted)
  {
    _throttle.Reset();

    Svc.SM.UI.ElementWdw.OnElementChanged += OnElementChanged;

    // SuperMemo can start again in the same plugin session, and HotKeyManager rejects a second registration of an id.
    if (Svc.HotKeyManager.HotKeys.All(h => h.Id != CheckHotKeyId))
      Svc.HotKeyManager.RegisterGlobal(
        CheckHotKeyId,
        "Check the formulation of the current item",
        HotKeyScopes.SM,
        DefaultCheckHotKey,
        CheckCurrentItem);

    base.OnSMStarted(wasSMAlreadyStarted);
  }

  /// <inheritdoc />
  protected override void OnSMStopped()
  {
    lock (_autoCheckLock)
      CancelPendingAutoCheck();

    try
    {
      Svc.SM.UI.ElementWdw.OnElementChanged -= OnElementChanged;
    }
    catch (Exception ex) when (ex is RemoteException or RemotingException)
    {
      LogTo.Debug(ex, "Formulation: the element window was already gone.");
    }

    base.OnSMStopped();
  }

  /// <inheritdoc />
  protected override void Dispose(bool disposing)
  {
    if (disposing)
      lock (_autoCheckLock)
        CancelPendingAutoCheck();

    base.Dispose(disposing);
  }

  /// <inheritdoc />
  public override void ShowSettings()
  {
    ConfigurationWindow.ShowAndActivate("Formulation Advisor Settings", HotKeyManager.Instance, Config);
  }

  private void CheckCurrentItem() => _ = CheckCurrentItemAsync();

  private async Task CheckCurrentItemAsync()
  {
    try
    {
      var (isItem, findings) = await Task.Run(() => Check(Svc.SM.UI.ElementWdw.CurrentElement, CancellationToken.None))
                                         .ConfigureAwait(false);
      var summary = isItem ? FindingText.Summary(findings) : FindingText.NotAnItem;

      await Application.Current.Dispatcher.InvokeAsync(() => ShowFindings(summary, findings));
    }
    catch (Exception ex) when (ex is RemoteException or RemotingException)
    {
      LogTo.Warning(ex, "Formulation: the check of the current item failed.");
    }
  }

  private void OnElementChanged(SMDisplayedElementChangedEventArgs e)
  {
    if (!Config.AutoCheck)
      return;

    var cancellationToken = RestartAutoCheck();

    _ = AutoCheckAsync(e.NewElement, cancellationToken);
  }

  private async Task AutoCheckAsync(IElement? element, CancellationToken cancellationToken)
  {
    try
    {
      await Task.Run(() =>
                       {
                         var (isItem, findings) = Check(element, cancellationToken);

                         cancellationToken.ThrowIfCancellationRequested();

                         if (isItem && element is not null && _throttle.ShouldNotify(element.Id, findings))
                           FindingText.Notification(findings, CurrentHotKeyText()).ShowDesktopNotification();
                       },
                       cancellationToken)
                .ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      LogTo.Debug("Formulation: an automatic check was replaced by a newer one.");
    }
    catch (Exception ex) when (ex is RemoteException or RemotingException)
    {
      LogTo.Warning(ex, "Formulation: the automatic check failed.");
    }
  }

  private (bool IsItem, IReadOnlyList<FormulationFinding> Findings) Check(IElement? element, CancellationToken cancellationToken)
  {
    var html = ItemReader.Read(element, cancellationToken);

    if (html is null)
      return (false, []);

    return (true, _analyzer.Analyze(ItemModelBuilder.Build(html), Config.ToOptions()));
  }

  private CancellationToken RestartAutoCheck()
  {
    lock (_autoCheckLock)
    {
      CancelPendingAutoCheck();
      _autoCheckCts = new CancellationTokenSource();

      return _autoCheckCts.Token;
    }
  }

  private void CancelPendingAutoCheck()
  {
    _autoCheckCts?.Cancel();
    _autoCheckCts?.Dispose();
    _autoCheckCts = null;
  }

  private void ShowFindings(string summary, IReadOnlyList<FormulationFinding> findings)
  {
    _findingsWindow?.Close();

    var window = new FindingsWindow(summary, findings);
    window.Closed += (_, _) =>
    {
      if (ReferenceEquals(_findingsWindow, window))
        _findingsWindow = null;
    };

    _findingsWindow = window;
    window.ShowAndActivate();
  }

  private static string CurrentHotKeyText() =>
    (Svc.HotKeyManager.HotKeys.FirstOrDefault(h => h.Id == CheckHotKeyId)?.ActualHotKey ?? DefaultCheckHotKey).ToString();
}
