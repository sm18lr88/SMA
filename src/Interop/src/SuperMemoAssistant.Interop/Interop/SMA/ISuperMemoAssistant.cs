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




namespace SuperMemoAssistant.Interop.SMA
{
  using System;
  using System.Collections.Generic;
  using Notifications;
  using SuperMemo;
  using SuperMemo.Core;

  /// <summary>SuperMemo Assistant service</summary>
  public interface ISuperMemoAssistant
  {
    /// <summary>The SuperMemo service</summary>
    ISuperMemo SM { get; }

    /// <summary>Available layout names</summary>
    IEnumerable<string> Layouts { get; }

    /// <summary>The desktop notification manager</summary>
    INotificationManager NotificationMgr { get; }

    /// <summary>Triggered when the collection to be loaded in SM has been selected.</summary>
    event Action<SMCollection> OnCollectionSelectedEvent;

    /// <summary>Triggered when the SM process is created.</summary>
    event Action OnSMStartingEvent;

    /// <summary>Triggered when the SM process is fully started, and the collection loaded.</summary>
    event Action OnSMStartedEvent;

    /// <summary>
    ///   Triggered when the SM process has been stopped. Make sure to provide a visual feedback for long-running tasks.
    /// </summary>
    /// <remarks>
    ///   Warning: While SMA only allows a single instance of its executable to be run, the user can open the collection that
    ///   was just closed by running the SuperMemo executable directly.
    /// </remarks>
    event Action OnSMStoppedEvent;

    /// <summary>
    ///   Registers work that needs SuperMemo to be closed, for example changing sm20.exe or the collection's settings files.
    ///   Register during plugin initialization (<c>OnPluginInitialized</c>). SMA waits, for up to 30 seconds, until every
    ///   connected plugin has finished initializing before it runs the beforeLaunch hooks.
    /// </summary>
    /// <param name="name">Name of the hook, shown with its warnings and in the log.</param>
    /// <param name="beforeLaunch">
    ///   Runs once per SMA start, after the collection is selected and before sm20.exe is read or started, so a change to
    ///   sm20.exe is the build SMA resolves symbols from and starts. May be null.
    /// </param>
    /// <param name="afterExit">
    ///   Runs once after the SuperMemo process has exited, before the plugins are stopped and SMA exits. May be null.
    /// </param>
    /// <remarks>
    ///   Hooks of all plugins run one after the other in registration order, each with a time limit (see
    ///   <c>LaunchHookRunner</c>). A hook that throws, or runs past its limit, is logged and reported to the user, and
    ///   SuperMemo still starts or SMA still exits: a hook can never block either. A hook can run on every start and every
    ///   exit, so it must be safe to repeat. Older SMA versions do not have this member; calling it there fails.
    /// </remarks>
    void RegisterLaunchHook(
      string                            name,
      Func<SMLaunchInfo, LaunchHookResult> beforeLaunch,
      Func<SMLaunchInfo, LaunchHookResult> afterExit);

    /// <summary>
    ///   Adds a command to the SMA command palette, or replaces the command with the same owner and id. Plugins do not
    ///   usually call this directly: their global hotkeys appear in the palette, and <c>SMAPluginBase.RegisterPaletteCommand</c>
    ///   adds a command that has no hotkey.
    /// </summary>
    /// <param name="command">What the palette shows.</param>
    /// <param name="execute">Runs the command. SMA calls it after the palette closes, on a background thread.</param>
    /// <remarks>Older SMA versions do not have this member; calling it there fails.</remarks>
    void RegisterCommand(PaletteCommand command, Action execute);

    /// <summary>Removes a command from the SMA command palette. Does nothing when the command is not registered.</summary>
    /// <remarks>Older SMA versions do not have this member; calling it there fails.</remarks>
    void UnregisterCommand(string owner, string id);
  }
}
