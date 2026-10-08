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
  using Types;

  /// <summary>
  ///   Defines all the available fields in an element. Used to determine which field have been changed in the
  ///   <see cref="IElement.OnChanged" /> event
  /// </summary>
  [Flags]
  public enum ElementFieldFlags
  {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    None            = 0,
    Parent          = 1,
    NextSibling     = 2,
    PrevSibling     = 4,
    FirstChild      = 8,
    LastChild       = 16,
    DescendantCount = 32,
    ChildrenCount   = 64,
    Name            = 128,
    Components      = 256,
    Template        = 512,
    Concept         = 1024,
    AFactor         = 2048,
    Deleted         = 4096,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
  }
}
