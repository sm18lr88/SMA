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




namespace SuperMemoAssistant.Interop.SuperMemo.Elements.Types
{
  using System;
  using System.Collections.Generic;
  using Content;
  using Core;
  using Models;
  using Registry.Members;
  using UI.Element;

  /// <summary>Represents an element in SuperMemo. See <see cref="IElementRegistry" /></summary>
  public interface IElement
  {
    /// <summary>
    ///   <see cref="Id" /> does not reliably identify an element. SuperMemo re-uses ids when elements are deleted, therefore
    ///   stored ids may reference different elements in the future.
    /// </summary>
    [Obsolete("Not implemented yet, returns the same as Id.")]
    string UniqueId { get; }

    /// <summary>The element id, starting from 1. Element id 1 is the root element.</summary>
    int Id { get; }

    /// <summary>The element's title (in the KT and registry)</summary>
    string Title { get; }

    /// <summary>Whether this element is marked as deleted.</summary>
    bool Deleted { get; }

    /// <summary>The type of the element (e.g. Topic, Item, Concept, ...)</summary>
    ElementType Type { get; }

    /// <summary>The container for all components belonging to this element</summary>
    IComponentGroup ComponentGroup { get; }

    /// <summary>The optional template applied on this element</summary>
    ITemplate Template { get; }

    /// <summary>The main concept associated with this element</summary>
    IConcept Concept { get; }

    /// <summary>The element's parent element. Should never be null, expect for the root element.</summary>
    IElement Parent { get; }

    /// <summary>The first children (first element under this branch)</summary>
    IElement FirstChild { get; }

    /// <summary>The last children (last element under this branch)</summary>
    IElement LastChild { get; }

    /// <summary>The element immediately after this one, when constrained to the depth level of this element</summary>
    IElement NextSibling { get; }

    /// <summary>The element immediately before this one, when constrained to the depth level of this element</summary>
    IElement PrevSibling { get; }

    /// <summary>
    ///   The number of elements underneath this element, including grandchildren, grand-grandchildren, and so on.
    /// </summary>
    int DescendantCount { get; }

    /// <summary>The number of elements underneath this element, including only the first degree children.</summary>
    int ChildrenCount { get; }

    /// <summary>The children immediately underneath this element</summary>
    IEnumerable<IElement> Children { get; }

    /// <summary>Returns a json representation of this element.</summary>
    /// <returns>Json string</returns>
    string ToJson();

    /// <summary>Displays this element in the <see cref="IElementWdw" /></summary>
    /// <returns>Success of operation</returns>
    bool Display();

    /// <summary>Moves this element to a new branch</summary>
    /// <param name="newParent">The target element destination</param>
    /// <returns>Success of operation</returns>
    bool MoveTo(IElement newParent);

    /// <summary>Deletes this element from the Collection</summary>
    /// <returns>Success of operation</returns>
    bool Delete();

    /// <summary>Removes any content from this element and removes it from the learning queue</summary>
    /// <returns>Success of operation</returns>
    bool Done();

    /// <summary>Raised when this element is changed</summary>
    event Action<SMElementChangedEventArgs> OnChanged;
  }
}
