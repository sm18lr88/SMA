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
// Modified On:  2020/04/06 19:47
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.Plugins
{
  using PluginManager.Interop.Contracts;
  using Sys.Remoting;

  /// <summary>SMA's plugins' interface</summary>
  public interface ISMAPlugin : IPluginBase
  {
    /// <summary>Whether the plugin has a settings interface available</summary>
    bool HasSettings { get; }

    /// <summary>General message callback</summary>
    /// <param name="msg">The message type</param>
    /// <param name="parameters">The parameters associated with this message</param>
    /// <returns></returns>
    RemoteTask<object> OnMessage(PluginMessage msg, params object[] parameters);

    /// <summary>Requests the plugin to show its settings dialog interface</summary>
    void ShowSettings();

    /// <summary>
    ///   Blocks until the plugin has finished its initialization (<c>OnPluginInitialized</c>), or the time limit passes.
    ///   SMA calls it for every connected plugin before it runs launch hooks, because a plugin registers its hooks while it
    ///   initializes and "connected" is reached earlier. A plugin built for an older SMA has no such member; SMA treats the
    ///   failed call as "ready".
    /// </summary>
    /// <param name="timeoutMilliseconds">The longest time to wait</param>
    /// <returns>True when the plugin finished initializing</returns>
    bool WaitUntilInitialized(int timeoutMilliseconds);
  }
}
