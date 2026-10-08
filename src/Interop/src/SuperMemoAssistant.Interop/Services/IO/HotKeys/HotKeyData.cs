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
// Created On:   2019/03/02 18:29
// Modified On:  2019/03/03 14:51
// Modified By:  Alexis

#endregion




using System;
using System.ComponentModel;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.Sys.ComponentModel;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Services.IO.HotKeys
{
  public class HotKeyData : INotifyPropertyChanged
  {
    #region Constructors

    public HotKeyData(string id,
                      string description,
                      bool   enabled,
                      bool   isGlobal,
                      HotKey defaultHotKey,
                      HotKey actualHotKey,
                      Action callback,
                      HotKeyScopes? scopes = null)
    {
      Id            = id;
      Description   = description;
      Enabled       = enabled;
      IsGlobal      = isGlobal;
      DefaultHotKey = defaultHotKey;
      ActualHotKey  = actualHotKey;
      Callback      = callback;
      Scopes        = isGlobal ? scopes ?? HotKeyScopes.SM : null;
    }

    #endregion




    #region Properties & Fields - Public

    public string Id          { get; }
    public string Description { get; }

    public bool Enabled  { get; set; }
    public bool IsGlobal { get; }

    public HotKey DefaultHotKey { get; }
    public HotKey ActualHotKey  { get; set; }

    public Action Callback { get; }

    /// <summary>Where a global hotkey works, or null for a hotkey that only works in a plugin window.</summary>
    public HotKeyScopes? Scopes { get; }

    #endregion




    #region Methods

    public void OnActualHotKeyChanged(object before, object after)
    {
      // A hotkey that had no key yet must also be registered when it gets one.
      if (Equals(before, after))
        return;

      HotKeyChanged?.Invoke(
        this,
        before as HotKey, after as HotKey
      );
    }

    #endregion




    #region Events

    public event PropertyChangedDelegate<HotKeyData, HotKey> HotKeyChanged;
    public event PropertyChangedEventHandler                 PropertyChanged;

    #endregion
  }
}
