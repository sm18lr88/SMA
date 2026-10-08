# src/Core - host app, core runtime, SuperMemo integration

## OVERVIEW
The SuperMemoAssistant.exe app, its runtime library, and the SuperMemo integration projects. Earned its file: 5 projects, ~25k LOC, process boundary owner.

## STRUCTURE
```
Core/
├── SuperMemoAssistant/          # WPF exe: Program.cs (Velopack hooks) -> AppBootstrap -> App.xaml; Setup/ first-run screens, UI/Settings
├── SuperMemoAssistant.Core/     # runtime lib: SMA/ lifecycle, Plugins/ SMAPluginManager, SuperMemo/ (own AGENTS.md), Sys/
├── SuperMemoAssistant.Hooks.Agent/   # NativeAOT integration component, published as a native sidecar
├── SuperMemoAssistant.Hooks.Common/  # message contract shared by agent + SMA (AgentMessage records)
└── SuperMemoAssistant.Hooks.Symbols/ # integration support library
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Startup order / service wiring | SuperMemoAssistant/AppBootstrap.cs | logging -> config -> hotkeys -> SMA -> plugin manager |
| Process-wide statics | SuperMemoAssistant.Core/SMA/Core.cs | Core.SMA, Core.SM, Core.Hook, Core.Logger... (host side of Svc) |
| SuperMemo launch / collection load | SMA/SMA.cs + SMA.LaunchHooks.cs, LaunchHookRunner.cs | partial class split by concern |
| Plugin install/start/stop | Plugins/SMAPluginManager.*.cs | .Initialization/.Packages/.Process/.Service/.LogAdapter partials |
| Toasts | SMA/NotificationManager.cs | Windows SDK toast projection (needs 10.0.19041 TFM) |
| Hooks.* internals | local/integration-internals.md | gitignored, maintainer-only; may be absent |

## CONVENTIONS
- Partial classes slice big types by concern (SMAPluginManager x6, SMA x4) - add a new partial, don't grow one file.
- Hooks.Agent + Hooks.Common: IsAotCompatible, InvariantGlobalization, Nullable+ImplicitUsings ON, no reflection, `unsafe` allowed. Core/app projects: Nullable OFF.
- Agent build output is managed-only; the real native DLL comes from `dotnet publish`. App csproj targets PublishSidecars / PublishSidecarsToPublishDir publish Hooks.Agent and libs/PluginManager PluginHost.exe next to the exe.
- Dev builds land in artifacts/app-dev/ (SMAExecutableInfo.IsDev detects that folder) - not Velopack's `current`.
- AgentMessage subtypes are sealed records; protocol changes need both AgentProtocol serializer and agent dispatcher updated.

## ANTI-PATTERNS
- NEVER add reflection, dynamic codegen, or non-AOT-safe packages to Hooks.Agent/Hooks.Common - NativeAOT publish breaks.
- Do not reference SuperMemoAssistant.Themes or Plugins.Themes from Core (guarded by Tests/Architecture/OptionalComponentTests).

## NOTES
- App.xaml.cs (290 LOC) mixes theme/config loading with startup + global exception handlers.
- SuperMemoAssistant.Tests has InternalsVisibleTo on Core and Interop.
