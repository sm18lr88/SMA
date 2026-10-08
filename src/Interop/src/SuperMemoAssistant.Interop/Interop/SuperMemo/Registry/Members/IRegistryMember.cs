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




namespace SuperMemoAssistant.Interop.SuperMemo.Registry.Members
{
  using System.Collections.Generic;
  using Elements.Types;

  /// <summary>
  /// Determines a member (e.g. a text, image, sound, ..) in SM's registry
  /// </summary>
  public interface IRegistryMember
  {
    /// <summary>
    /// The registry member's unique ID
    /// </summary>
    int    Id       { get; }

    /// <summary>
    /// The registry member's registry name
    /// </summary>
    string Name     { get; }

    /// <summary>
    /// Whether this member has been deleted
    /// </summary>
    bool   Empty    { get; }

    /// <summary>
    /// The number of components referencing that member (not always reliable)
    /// </summary>
    int    UseCount { get; }

    /// <summary>Retrieve linked file path (HTML, Image, Audio, ...)</summary>
    /// <returns>File path or null</returns>
    string GetFilePath();

    /// <summary>Retrieve elements that are using this registry</summary>
    /// <returns>All elements referencing that member</returns>
    IEnumerable<IElement> GetLinkedElements();

    /// <summary>
    /// Deletes the member from the registry
    /// </summary>
    /// <returns>Success of operation</returns>
    bool Delete();

    /// <summary>
    /// Renames the member
    /// </summary>
    /// <param name="newName"></param>
    /// <returns>Success of operation</returns>
    bool Rename(string newName);

    /// <summary>
    /// Starts a neural review for the current member
    /// </summary>
    /// <returns>Success of operation</returns>
    bool Neural();
  }
}
