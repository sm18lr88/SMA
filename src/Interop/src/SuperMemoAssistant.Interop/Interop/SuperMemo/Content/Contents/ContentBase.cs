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




namespace SuperMemoAssistant.Interop.SuperMemo.Content.Contents
{
  using System;
  using System.ComponentModel;
  using System.Windows;
  using Models;
  using Size = System.Drawing.Size;

  [Serializable]
  public abstract class ContentBase : INotifyPropertyChanged
  {
    #region Constructors

    protected ContentBase(AtFlags             displayAt,
                          VerticalAlignment   verticalAlignment   = VerticalAlignment.Stretch,
                          HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch,
                          Size                size                = default)
    {
      DisplayAt           = displayAt;
      VerticalAlignment   = verticalAlignment;
      HorizontalAlignment = horizontalAlignment;
      Size                = size;
    }

    #endregion




    #region Properties & Fields - Public

    public AtFlags             DisplayAt           { get; set; }
    public VerticalAlignment   VerticalAlignment   { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; }
    public Size                Size                { get; set; }
    public Size                MinSize             { get; private set; }

    #endregion




    #region Methods

    public void OnSizeChanged()
    {
      MinSize = ComputeMinSize();
    }

    protected Size ComputeMinSize()
    {
      return new Size(Math.Max(Size.Width, MinCompSize.Width),
                      Math.Max(Size.Height, MinCompSize.Height));
    }

    #endregion




    #region Methods Abs

    public abstract ContentTypeFlags ContentType { get; }
    public abstract Size             MinCompSize { get; }

    #endregion




    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }
}
