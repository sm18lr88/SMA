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




namespace SuperMemoAssistant.Interop.SMA.Notifications
{
  using System;
  using System.Collections.Generic;

  /// <summary>
  ///   Represents the data relating to the interactions of a user with a Windows Toast Notification (e.g. when the user
  ///   presses a button, or clicks on the notification).
  /// </summary>
  [Serializable]
  public class ToastActivationData
  {
    #region Constructors

    /// <summary>Creates a new instance</summary>
    /// <param name="pluginPackageName"></param>
    /// <param name="pluginVersion"></param>
    /// <param name="arguments"></param>
    /// <param name="userInput"></param>
    public ToastActivationData(string                     pluginPackageName,
                               string                     pluginVersion,
                               Dictionary<string, string> arguments,
                               Dictionary<string, string> userInput)
    {
      PluginPackageName = pluginPackageName;
      PluginVersion     = pluginVersion;
      Arguments         = arguments;
      UserInput         = userInput;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>Package Name of the Plugin from which the notification originated.</summary>
    public string PluginPackageName { get; }

    /// <summary>Version of the Plugin from which the notification originated.</summary>
    public string PluginVersion { get; }

    /// <summary>The arguments of the action chosen by the user.</summary>
    public Dictionary<string, string> Arguments { get; }

    /// <summary>Optional user input (e.g. if the toast had a text box, or a combo box).</summary>
    public Dictionary<string, string> UserInput { get; }

    #endregion
  }
}
