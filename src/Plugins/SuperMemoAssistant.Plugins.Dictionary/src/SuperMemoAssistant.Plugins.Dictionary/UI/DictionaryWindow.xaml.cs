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
// Created On:   2019/01/01 22:07
// Modified On:  2019/02/23 14:39
// Modified By:  Alexis

#endregion




using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.Dictionary.Interop;
using SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models;
using SuperMemoAssistant.Sys.Remoting;
using SuperMemoAssistant.Sys.Windows;

namespace SuperMemoAssistant.Plugins.Dictionary.UI
{
  /// <summary>Interaction logic for DictionaryWindow.xaml</summary>
  public partial class DictionaryWindow : Window
  {
    #region Constructors

    public DictionaryWindow(IDictionaryService dict,
                            string             word)
    {
      InitializeComponent();

      LookupWord(dict,
                 word);
    }

    #endregion




    #region Methods

    private void LookupWord(IDictionaryService dict,
                            string             word)
    {
      RemoteCancellationTokenSource cts = new RemoteCancellationTokenSource();

      var entryResultTask = LookupWordEntryAsync(cts.Token,
                                                 word,
                                                 dict);

      DataContext = new PendingEntryResult(cts, entryResultTask, dict);

      Title += ": " + word;
    }
    
    private async Task<EntryResult> LookupWordEntryAsync(RemoteCancellationToken ct,
                                                         string                  word,
                                                         IDictionaryService      dict)
    {
      var lemmas = await dict.LookupLemma(ct, word, dict.DefaultDictionary.GetLanguageId());

      if (lemmas?.Results == null
        || lemmas.Results.Any() == false
        || lemmas.Results[0].LexicalEntries.Any() == false
        || lemmas.Results[0].LexicalEntries[0].InflectionOf.Any() == false)
        return null;

      word = lemmas.Results[0].LexicalEntries[0].InflectionOf[0].Text;

      if (string.IsNullOrWhiteSpace(word))
        return null;

      return await dict.LookupEntry(ct, word, dict.DefaultDictionary.GetLanguageId());
    }

    private bool OnAfterExtract(bool success)
    {
      if (success)
        Close();

      return true;
    }

    private void Window_KeyDown(object       sender,
                                KeyEventArgs e)
    {
      var kbMod = KeyboardEx.GetKeyboardModifiers();

      switch (e.Key)
      {
        case Key.X:
          if (kbMod.Alt)
            DictionaryCtrl.Extract();
          break;

        case Key.Enter:
        case Key.Escape:
          Close();
          break;
      }
    }

    #endregion
  }
}
