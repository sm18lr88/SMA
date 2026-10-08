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




namespace SuperMemoAssistant.Extensions
{
  using System;

  /// <summary>Extends <see cref="Type" /></summary>
  public static class TypeEx
  {
    #region Methods

    /// <summary>Checks whether <paramref name="toCheck" /> extends <paramref name="generic" />.</summary>
    /// <param name="toCheck">The derived type</param>
    /// <param name="generic">The inherited unbound generic type</param>
    /// <returns>Whether <paramref name="toCheck" /> inherits from <paramref name="generic" /></returns>
    public static bool IsSubclassOfUnboundGeneric(this Type toCheck, Type generic)
    {
      while (toCheck != null && toCheck != typeof(object))
      {
        var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;

        if (generic == cur)
          return true;

        toCheck = toCheck.BaseType;
      }

      return false;
    }

    #endregion
  }
}
