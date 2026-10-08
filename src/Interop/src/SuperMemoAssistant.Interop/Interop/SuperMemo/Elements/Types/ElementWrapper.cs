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
// Created On:   2020/03/29 00:21
// Modified On:  2020/04/07 06:23
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo.Elements.Types
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Content;
  using Core;
  using Models;
  using Registry.Members;

  /// <summary>
  /// Wraps an element for purpose or avoid remoting exceptions
  /// </summary>
  public class ElementWrapper : IElement
  {
    #region Constructors

    /// <summary>
    /// New instance
    /// </summary>
    /// <param name="element"></param>
    public ElementWrapper(IElement element)
    {
      Original = element;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>
    /// The wrapped element
    /// </summary>
    public IElement Original { get; }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public string UniqueId => Original.UniqueId;
    /// <inheritdoc />
    public int Id => Original.Id;
    /// <inheritdoc />
    public string Title => Original.Title;
    /// <inheritdoc />
    public bool Deleted => Original.Deleted;
    /// <inheritdoc />
    public ElementType Type => Original.Type;
    /// <inheritdoc />
    public IComponentGroup ComponentGroup => Original.ComponentGroup;
    /// <inheritdoc />
    public ITemplate Template => Original.Template;
    /// <inheritdoc />
    public IConcept Concept => Original.Concept;
    /// <inheritdoc />
    public IElement Parent => new ElementWrapper(Original.Parent);
    /// <inheritdoc />
    public IElement FirstChild => new ElementWrapper(Original.FirstChild);
    /// <inheritdoc />
    public IElement LastChild => new ElementWrapper(Original.LastChild);
    /// <inheritdoc />
    public IElement NextSibling => new ElementWrapper(Original.NextSibling);
    /// <inheritdoc />
    public IElement PrevSibling => new ElementWrapper(Original.PrevSibling);
    /// <inheritdoc />
    public int DescendantCount => Original.DescendantCount;
    /// <inheritdoc />
    public int ChildrenCount => Original.ChildrenCount;
    /// <inheritdoc />
    public IEnumerable<IElement> Children => Original.Children.Select(c => new ElementWrapper(c));

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public bool Display() => Original.Display();

    /// <inheritdoc />
    public bool MoveTo(IElement newParent) => Original.MoveTo(newParent);

    /// <inheritdoc />
    public bool Delete() => Original.Delete();

    /// <inheritdoc />
    public bool Done() => Original.Done();
    
    /// <inheritdoc />
    public string ToJson()
    {
      return Original.ToJson();
    }

    #endregion




    #region Events

    /// <inheritdoc />
    public event Action<SMElementChangedEventArgs> OnChanged;

    #endregion
  }
}
