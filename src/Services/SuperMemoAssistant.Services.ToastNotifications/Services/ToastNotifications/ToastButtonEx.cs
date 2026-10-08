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




namespace SuperMemoAssistant.Services.ToastNotifications
{
  using System.Diagnostics.CodeAnalysis;
  using Microsoft.QueryStringDotNET;
  using Microsoft.Toolkit.Uwp.Notifications;


  /// <summary>
  /// Extends <see cref="ToastButton"/>.
  /// </summary>
  [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "<Pending>")]
  public static class ToastButtonEx
  {
    #region Methods

    /// <summary>
    ///   Instantiates a new <see cref="ToastButton" /> with the given label <paramref name="content" /> and arguments
    ///   concatenated from <paramref name="arguments" />.
    /// </summary>
    /// <param name="content">The button label</param>
    /// <param name="activationType"></param>
    /// <param name="arguments">The activation arguments</param>
    /// <returns></returns>
    public static ToastButton Create(
      string                              content,
      ToastActivationType activationType = ToastActivationType.Background,
      params (string key, string value)[] arguments)
    {
      var queryStr = new QueryString();

      foreach (var arg in arguments)
        queryStr.Add(arg.key, arg.value);

      return new ToastButton(content, queryStr.ToString())
      {
        ActivationType = activationType
      };
    }

    #endregion
  }
}
