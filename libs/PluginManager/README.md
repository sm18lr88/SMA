# PluginManager.Net

A Plugin Manager framework for .NET.

This copy is a fork of [alexis-/PluginManager.Net](https://github.com/alexis-/PluginManager.Net) for .NET 10:

- `PluginManager.Remoting` is the named-pipe RPC between the plugin manager and the plugin processes.
- `PluginHost` loads each plugin with `AssemblyDependencyResolver`, so the `deps.json` of the plugin locates its managed and native dependencies.
- The plugin manager finds plugins in three places: bundled plugins next to the application (`IPluginLocations.PluginBundledDir`), development plugins, and NuGet plugins.
