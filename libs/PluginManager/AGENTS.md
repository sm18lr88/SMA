# libs/PluginManager - plugin host, packages, named-pipe RPC

## OVERVIEW
Fork of alexis-/PluginManager.Net for .NET 10: it runs one `PluginHost.exe` process per plugin, manages NuGet plugin packages, and talks to plugins over a custom named-pipe RPC. Score 17 (64 .cs files, 6 projects, ~22 consuming csprojs).

## STRUCTURE
```
src/
├── PluginManager.Core/            # host side: PluginManagerBase (partial x7), NuGet PackageManager/, Services/
├── PluginManager.Interop/         # plugin side: PluginBase, PluginHostBase, contracts
├── PluginManager.Remoting/        # RPC wire layer (net10.0, nullable ON)
├── PluginManager.PluginHost/      # PluginHost.exe (WPF WinExe), PluginLoader.cs
├── PluginManager.Shared/          # source-linked into Interop + PluginHost (no csproj)
└── PluginManager.Shared.MgrHost/  # source-linked into Core + PluginHost (no csproj)
Releases/KEEPME                    # placeholder dir
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Plugin start/stop/crash lifecycle | `Core/PluginManagerBase.Process.cs` | process spawn, pipe handshake, monitoring |
| Package scan / metadata bind | `Core/PluginManagerBase.Packages.cs` | |
| Service publish/revoke registry | `Core/PluginManagerBase.Service.cs`, `.Remote.cs` | |
| NuGet install/uninstall/search | `Core/PackageManager/PluginPackageManager.cs` | 741 LOC hotspot |
| Locked-DLL cleanup | `NuGetDeleteOnRestartManager` (Core/PackageManager) | deletes in-use files on next start |
| Plugin-side base class | `Interop/Plugins/PluginBase.cs` | `RegisterService`, `ConsumeService`, `OnInjected` |
| Assembly/deps loading in host | `Interop/PluginHost/PluginHostBase.Assembly.cs`, `PluginHost/PluginLoader.cs` | `AssemblyDependencyResolver` + plugin `deps.json` |
| RPC call/serve | `Remoting/RpcConnection.cs` (+ `.Receive.cs`), `RpcEndpoint.cs` | one duplex pipe, synchronous calls |
| By-ref objects / proxies | `Remoting/ReferenceTable.cs`, `RemoteProxy.cs` | |
| Wire format | `Remoting/ValueWriter.cs`, `ValueReader.cs`, `ValueTag.cs`, `TypeNames.cs`, `MethodKeys.cs` | custom binary format |
| Plugin dir lookup | `IPluginLocations` (Core/Contracts) | bundled (`PluginBundledDir`), development, NuGet |

## CONVENTIONS
- Six-parameter generic hierarchy: `PluginManagerBase<TParent, TPluginInstance, TMeta, ICustomPluginManager, ICore, IPlugin>` on the host side mirrors `PluginBase<TPlugin, IPlugin, ICore>` on the plugin side. SMA closes these generics in `src/Core`; keep the parameter lists in sync.
- `PluginManagerBase` is split by concern into partial files (`.Abstracts/.Helpers/.Packages/.Process/.Remote/.Service`). Add code to the matching partial, not the root file.
- Shared code is `<Compile Include ... Link=>` from `Shared*/` folders, not project references. An edit there recompiles into several assemblies.
- Core weaves with Fody: `PropertyChanged.Fody` + `Anotar.Custom.Fody` (`LogTo.*` -> `PluginManagerLogger`). `FodyWeavers.xml` is in Core.
- Remoting is the only nullable-enabled, implicit-usings project here; it uses file-scoped namespaces and a one-line purpose comment at file top.
- By-value RPC honours `[Serializable]`/`[NonSerialized]` (SYSLIB0050 suppressed for that reason). By-ref objects derive from `PerpetualMarshalByRefObject` (`Interop/Sys`).
- UI-thread property notifications are marshalled by `PropertyChangedNotificationInterceptor`.

## ANTI-PATTERNS
- NEVER reintroduce .NET Remoting/WCF/`MarshalByRefObject` lifetime leases; `PerpetualMarshalByRefObject` + `ReferenceTable` replace them.
- Do NOT load plugin assemblies into the SMA process; each plugin runs in its own PluginHost.exe.
- Do NOT remove the `InternalsVisibleTo SuperMemoAssistant.Tests` from Remoting; RPC tests depend on it.
- Do NOT add a csproj to `PluginManager.Shared*`; they are link-only source folders.
- Do NOT edit `nuget.config` here to add feeds; packages are pinned centrally at the repo root.
