# Notes for plugin authors

[Back to the README](../README.md)

## Breaking changes from SMA 2.x

Plugins written for SMA 2.x need these changes:

- Rebuild the plugin for `net10.0-windows` (x64) against the Interop assemblies of this repository. SMA refuses a plugin that targets Interop older than 3.0.0 and logs "Outdated interop version".
- To change the WPF application (for example, to merge resource dictionaries), override `ConfigureApplication`. PluginHost owns the one `Application` of the process.
- Expose each object that crosses a process boundary through a public interface. SMA generates proxies for interfaces only.
- `RemoteTask`, `RemoteCancellationToken`, and delegates keep working. Pass a plain delegate where 2.x used `ActionProxy`.
- `RemotingException` is in `PluginManager.Remoting`. By-value data must be marked `[Serializable]`.
- Several names changed. `RemotingServicesEx` is now `RpcServices` (`Connect`, `Serve`, `NewPipeName`). `IPluginBase.ChannelName` is now `PipeName`. The `*17` types are now `*20`, for example `SM20`. For the full list, see the [CHANGELOG](../CHANGELOG.md).
- `AtFlags` has the values of SuperMemo 20. If your plugin stores or compares these values, check it.
- `OnSMStarted` and `OnSMStopped` call their own handlers. In 2.x, they called the `OnSMStarting` handlers.

## Commands in the palette

The command palette (Ctrl+Alt+Shift+P) lets users find and run a command by its name.

- Every global hotkey that a plugin registers with `RegisterGlobal` appears in the palette. The title is the description of the hotkey, and the palette also shows the plugin name and the current hotkey. You do not need to do anything else.
- For a feature that does not need a hotkey, call `RegisterPaletteCommand(id, title, execute)` in `OnPluginInitialized` or later. Write the title as an action, for example "Copy the API token".
- The scope decides where a command can run. The palette hides an `SM` command when the user opened it from another application, and an `SMBrowser` command outside the element window.
- SMA runs a command after the palette closes and the focus returns to the window that the user came from. The call comes on a background thread, as for a hotkey.
- SMA removes the commands of a plugin when the plugin stops. SMA 3.0 has no palette: there, the calls are logged and have no effect.

## How plugins run

- **Plugin RPC** (`PluginManager.Remoting`): Plugins run in separate `PluginHost` processes. A named-pipe RPC gives them `MarshalByRefObject` objects, events, delegates, and synchronous properties across the process boundary.
- **Plugin loading:** Each plugin folder has its own `deps.json`. PluginHost uses it to load the managed and native dependencies of the plugin. SMA finds plugins in three places: plugins bundled next to `SuperMemoAssistant.exe`, development plugins, and plugins from the feed.
