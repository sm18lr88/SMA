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
// Modified On:  2020/04/07 04:59
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo.Registry.Types
{
  using System.Collections.Generic;
  using System.Text.RegularExpressions;

  /// <summary>Represents an instance of a SuperMemo registry for the given type <typeparamref name="IType" />.</summary>
  /// <typeparam name="IType">The registry members' type</typeparam>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix",
                                                   Justification = "SM naming convention")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1715:Identifiers should have correct prefix",
                                                   Justification = "Interface type")]
  public interface IRegistry<out IType> : IEnumerable<IType>
  {
    /// <summary>The number of members in this registry</summary>
    int Count { get; }

    /// <summary>Retrieve registry element from memory at given <paramref name="id" /></summary>
    /// <param name="id"></param>
    /// <returns>Element or null if invalid index (deleted, out of bound, ...)</returns>
    IType this[int id] { get; }

    /// <summary>Find registry members by their name</summary>
    /// <param name="regex">A regex to match the name</param>
    /// <returns>The matched members</returns>
    IEnumerable<IType> FindByName(Regex regex);

    /// <summary>Returns the first element which name's matches <paramref name="regex" /></summary>
    /// <param name="regex">A regex to match the name</param>
    /// <returns>The first match or null</returns>
    IType FirstOrDefaultByName(Regex regex);

    /// <summary>Returns the first element which name's matches <paramref name="exactName" /></summary>
    /// <param name="exactName">the exact name of the registry member</param>
    /// <returns>The first match or null</returns>
    IType FirstOrDefaultByName(string exactName);
  }
}
