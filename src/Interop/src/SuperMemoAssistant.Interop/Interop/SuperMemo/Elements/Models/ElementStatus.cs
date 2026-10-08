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
// Created On:   2019/01/15 00:56
// Modified On:  2019/01/15 00:56
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo.Elements.Models
{
  /// <summary>
  /// Defines all the possible status an element can have
  /// </summary>
  public enum ElementStatus
  {
    /// <summary>
    /// The element is not in the learning queue but will be inserted once the learning queue has been processed
    /// </summary>
    Pending   = 0,

    /// <summary>
    /// The element is in the learning queue and will be reviewed in time
    /// </summary>
    Memorized = 1,

    /// <summary>
    /// The element is forgotten: neither in learning queue nor in the pending queue
    /// </summary>
    Dismissed = 2,

    /// <summary>
    /// This element doesn't exist anymore -- this is only a placeholder
    /// </summary>
    Deleted   = 3
  }
}
