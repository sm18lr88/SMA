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
// Created On:   2018/06/01 14:25
// Modified On:  2019/02/28 22:18
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SuperMemoAssistant.Sys.IO.Devices
{
  /// <summary>
  /// Represents a sequence of keys
  /// </summary>
  [Serializable]
  public class KeyCollection : List<(Key key, Task<bool> task)>
  {
    #region Constructors

    /// <summary>
    /// New instance with all modifiers set to false
    /// </summary>
    /// <param name="keys"></param>
    public KeyCollection(params Key[] keys)
      : this(false,
             false,
             false,
             false,
             keys) { }

    /// <summary>
    /// New instance
    /// </summary>
    /// <param name="ctrl"></param>
    /// <param name="alt"></param>
    /// <param name="shift"></param>
    /// <param name="win"></param>
    /// <param name="keys"></param>
    public KeyCollection(bool         ctrl  = false,
                bool         alt   = false,
                bool         shift = false,
                bool         win   = false,
                params Key[] keys)
    {
      Ctrl  = ctrl;
      Alt   = alt;
      Shift = shift;
      Win   = win;

      Add(keys);
    }

    /// <summary>
    /// New instance
    /// </summary>
    /// <param name="keys"></param>
    public KeyCollection(params (Key key, Task<bool> task)[] keys)
      : this(false,
             false,
             false,
             false,
             keys) { }

    /// <summary>
    /// New instance
    /// </summary>
    /// <param name="ctrl"></param>
    /// <param name="alt"></param>
    /// <param name="shift"></param>
    /// <param name="win"></param>
    /// <param name="keys"></param>
    public KeyCollection(bool                                ctrl  = false,
                bool                                alt   = false,
                bool                                shift = false,
                bool                                win   = false,
                params (Key key, Task<bool> task)[] keys)
    {
      Ctrl  = ctrl;
      Alt   = alt;
      Shift = shift;
      Win   = win;

      AddRange(keys);
    }

    #endregion




    #region Methods

    public void Add(params Key[] keys)
    {
      AddRange(keys.Select(key => (key, (Task<bool>)null)));
    }

    public static KeyCollection operator +(KeyCollection ks,
                                  Key  key)
    {
      ks.Add((key, null));

      return ks;
    }

    public static KeyCollection operator +(Key  key,
                                  KeyCollection ks)
    {
      ks.Insert(0,
                (key, null));

      return ks;
    }

    public static KeyCollection operator +(KeyCollection                           ks,
                                  (Key key, Task<bool> syncTask) vt)
    {
      ks.Add(vt);

      return ks;
    }

    public static KeyCollection operator +((Key key, Task<bool> syncTask) vt,
                                  KeyCollection                           ks)
    {
      ks.Insert(0,
                vt);

      return ks;
    }


    public IEnumerable<Key> GetModifiers()
    {
      List<Key> modifiers = new List<Key>();

      if (Alt)
        modifiers.Add(Key.LeftAlt);

      if (Ctrl)
        modifiers.Add(Key.LeftCtrl);

      if (Shift)
        modifiers.Add(Key.LeftShift);

      if (Win)
        modifiers.Add(Key.LWin);

      return modifiers;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "We only use C#")]
    public static implicit operator KeyCollection(HotKey hotKey)
    {
      return new KeyCollection(
        hotKey.Ctrl,
        hotKey.Alt,
        hotKey.Shift,
        hotKey.Win,
        hotKey.Key
      );
    }

    #endregion




    #region Properties & Fields

    public bool Alt   { get; set; }
    public bool Ctrl  { get; set; }
    public bool Shift { get; set; }
    public bool Win   { get; set; }

    public IEnumerable<(VKey vkey, Task<bool> task)> VKeys =>
      this.Select(tv => ((VKey)KeyInterop.VirtualKeyFromKey(tv.key), tv.task));
    public IEnumerable<VKey> VKeysModifiers => GetModifiers().Select(k => (VKey)KeyInterop.VirtualKeyFromKey(k));

    #endregion
  }
}
