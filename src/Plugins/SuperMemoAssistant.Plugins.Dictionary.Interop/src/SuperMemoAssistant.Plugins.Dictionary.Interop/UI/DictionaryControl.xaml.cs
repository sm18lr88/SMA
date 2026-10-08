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
// 
// 
// Created On:   2018/12/31 04:46
// Modified On:  2019/02/23 14:55
// Modified By:  Alexis

#endregion




using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.Remoting;

namespace SuperMemoAssistant.Plugins.Dictionary.Interop.UI
{
  /// <summary>
  /// 
  /// </summary>
  /// <param name="success"></param>
  /// <returns>Whether to show an information popup in case of error.</returns>
  public delegate bool DictionaryWordExtractedDelegate(bool success);

  /// <summary>
  /// 
  /// </summary>
  /// <returns>Whether to continue with extract or not</returns>
  public delegate bool DictionaryWordExtractingDelegate();

  /// <summary>Interaction logic for DictionaryControl.xaml</summary>
  public partial class DictionaryControl : UserControl
  {
    #region Constants & Statics

    public static readonly DependencyProperty EntriesProperty =
      DependencyProperty.Register(nameof(Entries),
                                  typeof(PendingEntryResult),
                                  typeof(DictionaryControl),
                                  new PropertyMetadata(
                                    (o,
                                     ev) =>
                                    {
                                      if (o is DictionaryControl ctrl && ev.NewValue != null)
                                        ctrl.OnNewEntries((PendingEntryResult)ev.NewValue);
                                    }
                                  ));

    public static readonly DependencyProperty OnBeforeExtractProperty =
      DependencyProperty.Register(nameof(OnBeforeExtract), typeof(DictionaryWordExtractingDelegate), typeof(DictionaryControl));

    public static readonly DependencyProperty OnAfterExtractProperty =
      DependencyProperty.Register(nameof(OnAfterExtract), typeof(DictionaryWordExtractedDelegate), typeof(DictionaryControl));

    #endregion




    #region Properties & Fields - Non-Public

    private IDictionaryService Plugin { get; set; }

    private RemoteCancellationTokenSource CancelTokenSource { get; set; }

    private string Html { get; set; }

    private string Word { get; set; }

    #endregion




    #region Constructors

    //private mshtml.IHTMLDocument3 Document => (mshtml.IHTMLDocument3)Browser.Document;

    public DictionaryControl()
    {
      InitializeComponent();
    }

    #endregion




    #region Properties & Fields - Public

    public DictionaryWordExtractingDelegate OnBeforeExtract
    {
      get => (DictionaryWordExtractingDelegate)GetValue(OnBeforeExtractProperty);
      set => SetValue(OnBeforeExtractProperty, value);
    }


    public DictionaryWordExtractedDelegate OnAfterExtract
    {
      get => (DictionaryWordExtractedDelegate)GetValue(OnAfterExtractProperty);
      set => SetValue(OnAfterExtractProperty, value);
    }

    public PendingEntryResult Entries
    {
      set => SetValue(EntriesProperty,
                      value);
    }

    #endregion




    #region Methods Impl

    protected override void OnToolTipClosing(ToolTipEventArgs e)
    {
      CancelTokenSource.Cancel();
      CancelTokenSource = null;
      Plugin            = null;
      Html              = null;
      Word              = null;

      //LoadingIndicator.IsActive   = false;
      //LoadingIndicator.Visibility = Visibility.Collapsed;

      base.OnToolTipClosing(e);
    }

    #endregion




    #region Methods

    public bool Extract()
    {
      if (OnBeforeExtract != null && OnBeforeExtract() == false)
        return false;

      bool success = Svc.SM.Registry.Element.Add(
        out _,
        ElemCreationFlags.CreateSubfolders,
        new ElementBuilder(ElementType.Topic,
                           Html)
          .WithParent(Plugin.RootElement)
          .WithLayout(Plugin.Layout)
          .WithPriority(Plugin.ExtractPriority)
          .WithTitle(Word)
          .DoNotDisplay()
      );

      var displayError = OnAfterExtract == null || OnAfterExtract(success);

      if (success == false && displayError)
        MessageBox.Show("Element creation failed.",
                        "Error");

      return success;
    }

    private void BtnExtract_Click(object          sender,
                                  RoutedEventArgs e)
    {
      Extract();
    }

    private void OnNewEntries(PendingEntryResult pending)
    {
      //LoadingIndicator.IsActive   = true;
      //LoadingIndicator.Visibility = Visibility.Visible;

      Browser.NavigateToString(DictionaryConst.Loading);

      CancelTokenSource = pending.CancellationTokenSrc;
      Plugin            = pending.Plugin;

      pending.EntryResultTask.ContinueWith(
        task =>
        {
          Html = task?.Result == null
            ? DictionaryConst.Error
            : BuildHtml(task.Result).Result;

          Dispatcher.Invoke(
            () =>
            {
              //LoadingIndicator.IsActive   = false;
              //LoadingIndicator.Visibility = Visibility.Collapsed;
              Browser.NavigateToString(Html);
            }
          );
        }
      );
    }

    private async Task<string> BuildHtml(EntryResult entries)
    {
      return await Plugin.ApplyUserTemplate(entries);
    }

    #endregion
  }
}
