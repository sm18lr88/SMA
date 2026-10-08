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
// Created On:   2020/04/07 04:38
// Modified On:  2020/04/07 04:42
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo.Elements.Models
{
  using System;

  /// <summary>The element display state (e.g. Ctrl+E in element window)</summary>
  [Serializable]
  public enum ElementDisplayState
  {
    /// <summary>"Edit" mode</summary>
    Edit = 0,

    /// <summary>"Drag and Size" mode</summary>
    Drag = 1,

    /// <summary>"Presentation" mode</summary>
    Display = 2,

    /// <summary>Unknown</summary>
    Unused1 = 3,

    /// <summary>Unknown</summary>
    Unused2 = 4,

    /// <summary>Shows the show answer button</summary>
    Question = 5,

    /// <summary>Shows the grading buttons</summary>
    Grading = 6,

    /// <summary>Shows the next repetition button</summary>
    NextRepetition = 7,
  }
}
