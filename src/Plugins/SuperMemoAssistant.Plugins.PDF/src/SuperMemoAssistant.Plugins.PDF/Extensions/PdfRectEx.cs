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
// Created On:   2018/06/10 21:23
// Modified On:  2018/06/10 22:02
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using SuperMemoAssistant.Pdfium;

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{
  public static class PdfRectEx
  {
    #region Methods

    /// <summary>
    ///   Checks whether rect1 and rect2 are next to each other, or intersecting along X.
    ///   Assumes <paramref name="rect1" /> before <paramref name="rect2" />.
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerance">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAdjacentAlongXWith(this PdfRect rect1,
                                            PdfRect      rect2,
                                            float         tolerance)
    {
      return rect1.Right + tolerance >= rect2.Left;
    }

    /// <summary>
    ///   Checks whether rect1 and rect2 are next to each other, or intersecting along Y.
    ///   Assumes <paramref name="rect1" /> before <paramref name="rect2" />.
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerence">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAdjacentAlongYWith(this PdfRect rect1,
                                            PdfRect      rect2,
                                            float         tolerence)
    {
      return rect1.Top + tolerence >= rect2.Bottom;
    }

    /// <summary>Checks whether rect1 and rect2 are aligned along X.</summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerence">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAlongsideXWith(this PdfRect rect1,
                                        PdfRect      rect2,
                                        float         tolerence)
    {
      return rect1.Left - tolerence <= rect2.Right && rect1.Right + tolerence >= rect2.Left
        || rect2.Left - tolerence <= rect1.Right && rect2.Right + tolerence >= rect1.Left;
    }

    /// <summary>Checks whether rect1 and rect2 are aligned along X.</summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerence">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAlongsideYWith(this PdfRect rect1,
                                        PdfRect      rect2,
                                        float         tolerence)
    {
      return rect1.Bottom - tolerence <= rect2.Top && rect1.Top + tolerence >= rect2.Bottom
        || rect2.Bottom - tolerence <= rect1.Top && rect2.Top + tolerence >= rect1.Bottom;
    }

    #endregion
  }

  public class PdfRectXComparer : IComparer<PdfRect>
  {
    #region Methods Impl

    /// <inheritdoc />
    public int Compare(PdfRect rect1,
                       PdfRect rect2)
    {
      return rect1.Left.CompareTo(rect2.Left);
    }

    #endregion
  }

  public class PdfRectYComparer : IComparer<PdfRect>
  {
    #region Methods Impl

    /// <inheritdoc />
    public int Compare(PdfRect rect1,
                       PdfRect rect2)
    {
      return rect1.Bottom.CompareTo(rect2.Bottom);
    }

    #endregion
  }
}
