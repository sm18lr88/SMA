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
// Modified On:  2020/04/07 05:05
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo.Registry.Types
{
  using Members;

  /// <summary>The <see cref="ISound" /> registry</summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "<Pending>")]
  public interface ISoundRegistry : IRegistry<ISound>
  {
    /// <summary>
    ///   Adds a new audio file to the sound registry. If <paramref name="registryName" /> is already used, SM will
    ///   automatically append an additional character to differentiate them
    /// </summary>
    /// <param name="filePath">The path to the file</param>
    /// <param name="registryName">The name to use in the registry</param>
    /// <returns>The created registry member id, or -1 if the operation failed</returns>
    int Add(string filePath, string registryName);
  }
}
