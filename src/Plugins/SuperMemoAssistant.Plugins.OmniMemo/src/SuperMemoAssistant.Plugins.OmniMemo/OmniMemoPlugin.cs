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




namespace SuperMemoAssistant.Plugins.OmniMemo
{
  using PluginManager.Remoting;
  using System.Windows;
  using System.Windows.Input;
  using Anotar.Serilog;
  using Interop.SuperMemo.Content.Controls;
  using Interop.SuperMemo.Core;
  using Services;
  using Services.IO.Keyboard;
  using SuperMemoAssistant.Interop.Plugins;
  using SuperMemoAssistant.Extensions;
  using Sys.IO.Devices;
  using Views;

  // ReSharper disable once ClassNeverInstantiated.Global
  public class OmniMemoPlugin : SMAPluginBase<OmniMemoPlugin>
  {
    #region Properties & Fields - Non-Public

    private OmniMemoWindow MainWindow { get; set; }

    #endregion




    #region Constructors

    public OmniMemoPlugin() { }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "OmniMemo";

    public override bool HasSettings => false;

    #endregion




    #region Methods Impl

    protected override void OnPluginInitialized()
    {
      MainWindow = new OmniMemoWindow();
      Application.Current.MainWindow = MainWindow;

      base.OnPluginInitialized();
    }

    protected override void OnSMStarted(bool wasSMAlreadyStarted)
    {
      Svc.HotKeyManager
         .RegisterGlobal(
           "ShowOmniMemo",
           "Show the OmniMemo window",
           HotKeyScopes.Global,
           new HotKey(Key.F, KeyModifiers.AltShift),
           ShowOmniMemo);

      base.OnSMStarted(wasSMAlreadyStarted);
    }

    #endregion




    #region Methods

    [LogToErrorOnException]
    public void ShowOmniMemo()
    {
      Application.Current.Dispatcher.InvokeAsync(() => MainWindow.ShowAndActivate());
    }

    [LogToErrorOnException]
    public void OnElementChanged(SMDisplayedElementChangedEventArgs e)
    {
      try
      {
        IControlHtml ctrlHtml = Svc.SM.UI.ElementWdw.ControlGroup.GetFirstHtmlControl();
      }
      catch (RemotingException) { }
    }

    #endregion
  }
}
