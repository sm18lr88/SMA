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
// Created On:   2019/04/14 20:45
// Modified On:  2019/04/15 00:00
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Linq;
using SuperMemoAssistant.Pdfium;
using SuperMemoAssistant.Extensions;

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{

  public static class PdfBookmarkEx
  {
    #region Methods

    public static string ToHierarchyString(this PdfBookmark bookmark)
    {
      List<PdfBookmark> bookmarkHierarchy = new List<PdfBookmark>();

      do
      {
        bookmarkHierarchy.Add(bookmark);
      } while ((bookmark = bookmark.Parent) != null);

      bookmarkHierarchy.Reverse();

      return StringEx.Join("::", bookmarkHierarchy.Select(b => b.Title));
    }

    /// <summary>Returns the destination of a go-to action in this document, or null for any other action.</summary>
    public static PdfDestination GetDestination(this PdfAction pdfAction) =>
      pdfAction?.ActionType == ActionType.GoTo ? pdfAction.Destination : null;

    #endregion
  }
}
