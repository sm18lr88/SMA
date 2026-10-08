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




namespace SuperMemoAssistant.Interop.SuperMemo.Elements.Models
{
  using System;
  using Builders;

  /// <summary>
  ///   Defines the result for the attempt at creating a specific element, as it was defined by its
  ///   <see cref="ElementBuilder" />
  /// </summary>
  [Serializable]
  public class ElemCreationResult
  {
    #region Constructors

    /// <summary>Internal use</summary>
    /// <param name="result"></param>
    /// <param name="builder"></param>
    public ElemCreationResult(ElemCreationResultCodes result, ElementBuilder builder)
    {
      Result  = result;
      Builder = builder;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The creation result. Use this property to determines what happened in case of failure</summary>
    public ElemCreationResultCodes Result { get; set; }

    /// <summary>The definition used to try and create this element</summary>
    public ElementBuilder Builder { get; }

    /// <summary>The new element id, if it was successfully created (see <see cref="Success" /></summary>
    public int ElementId { get; set; } = -1;

    /// <summary>
    /// Whether the element was successfully created
    /// </summary>
    public bool Success => Result.HasFlag(ElemCreationResultCodes.Success);

    #endregion
  }
}
