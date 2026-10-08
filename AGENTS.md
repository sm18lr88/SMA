# PROJECT KNOWLEDGE BASE

**Generated:** 2026-10-08
**Commit:** 9c6a61c
**Branch:** main

## OVERVIEW
SuperMemoAssistant (SMA): a WPF companion app for SuperMemo 20 (x64) that runs first-party plugins out of process over named-pipe RPC. Stack: .NET 10 (`net10.0-windows10.0.19041.0`, x64 only), Fody, xunit.v3, Velopack.

## STRUCTURE
```
SMA/
├── SuperMemoAssistant.slnx  # XML solution (not .sln), 41 projects
├── src/
│   ├── Core/       # exe, host runtime, SuperMemo integration projects (Hooks.*)
│   ├── Interop/    # plugin API every plugin compiles against
│   ├── Plugins/    # first-party plugins, each in its own PluginHost.exe process
│   ├── Services/   # optional plugin-side libs (settings UI, HTML, toasts, images)
│   ├── Themes/     # theme engine; no WPF/SMA deps
│   ├── Tests/      # xunit.v3 suites + UI Automation E2E agent
│   └── Tools/      # CardTool (`sma-cards` CLI), PluginFeed (NuGet v3 feed builder)
├── libs/           # source forks ported to .NET 10/x64 (PluginManager, Process.NET, Extensions.System.IO, pngcs)
├── build/          # pack.ps1, pack-plugins.ps1, plugin-feed/catalog.json
├── docs/           # bilingual EN/zh-CN docs + decisions/ ADRs 0001-0003
├── skills/supermemo-appearance/  # agent skill for restyling SM20 cards via sma-cards
├── .config/dotnet-tools.json     # pins vpk 1.2.161 (Velopack CLI)
└── assets/         # quotes.tsv, animations, icons
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| App startup / service wiring | src/Core/SuperMemoAssistant/AppBootstrap.cs | Program.cs handles Velopack hooks first |
| Plugin API surface | src/Interop/src/SuperMemoAssistant.Interop/Interop/Plugins/SMAPluginBase.cs | additive-only |
| Well-known paths | Interop/SMAFileSystem.cs | `SMA_APPDATA_DIR` overrides |
| Element/registry behavior | src/Core/SuperMemoAssistant.Core/SuperMemo/ | ElementRegistryBase is the hotspot |
| SuperMemo integration internals | local/integration-internals.md | gitignored, maintainer-only; may be absent |
| New plugin | src/Plugins/SuperMemoAssistant.Plugins.Template | docs/plugin-authors.md |
| Plugin process / RPC | libs/PluginManager/src | |
| Path value types | libs/Extensions.System.IO/src | used by ~25 projects |
| Plugin feed | src/Tools/SuperMemoAssistant.PluginFeed, build/plugin-feed/catalog.json, docs/plugin-feed.md | NuGet v3 + plugins.json on GitHub Pages |
| Theming / card CLI | src/Themes, Plugins.Themes, Tools/CardTool, skills/supermemo-appearance | `sma-cards` |
| Build prerequisites | docs/build.md | NativeAOT agent needs VS "Desktop development with C++" |
| Architecture decisions | docs/decisions/000{1,2,3}-*.md | 0001 is local-only (gitignored); 0002 PDFium; 0003 Themes optional + launch hooks |
| CI / release | .github/workflows/build.yml | release only on `refs/tags/v*` |

## CODE MAP
| Symbol | Type | Location | Refs | Role |
|--------|------|----------|------|------|
| Svc / Svc<T> | static class | Interop/Services/Svc.cs | every plugin | plugin-side service locator |
| SMAPluginBase<T> | abstract class | Interop/Interop/Plugins/ | every plugin | plugin lifecycle |
| ISuperMemoAssistant / ISuperMemo | interface | Interop/Interop/SMA, .../SuperMemo | high | host + SuperMemo contracts |
| SMAFileSystem | static class | Interop/Interop/SMAFileSystem.cs | high | all well-known paths |
| Core | static class | Core/SuperMemoAssistant.Core/SMA/Core.cs | high (host) | host-side singletons |
| SMAPluginManager | class (6 partials) | Core/SuperMemoAssistant.Core/Plugins/ | central | plugin install/start/stop |
| ElementRegistryBase | class | Core/.../SuperMemo/Common/Elements/ | central | element cache (1,082 LOC) |
| CfgBase<TCfg> | abstract class | Services.UI/Services/UI/Configuration | most plugins | settings model base |
| PluginManagerBase<...> | abstract partial | libs/PluginManager/src/PluginManager.Core | closed in src/Core | host plugin lifecycle |
| PluginBase<TPlugin,IPlugin,ICore> | abstract class | PluginManager.Interop/Plugins | every plugin | plugin-side base |
| RpcConnection / RpcEndpoint | sealed partial / static | PluginManager.Remoting | Interop, Tests | named-pipe RPC |
| NormalizedPath / FilePath / DirectoryPath | classes | libs/Extensions.System.IO/src | ~25 csprojs | path value types |

## CONVENTIONS
- `Directory.Build.props` is repo policy: x64 only (AnyCPU remapped), LangVersion latest, `TreatWarningsAsErrors`, `Nullable`/`ImplicitUsings` OFF by default (new projects opt in), Deterministic, Version 3.1.0.
- Central package management (`Directory.Packages.props`) with transitive pinning + NuGetAudit; csproj files carry no versions.
- NoWarn is intentional: CS1591, CS0067 (PropertyChanged.Fody), NU1701 (mshtml PIA), CA1416, SYSLIB0014, CS0618.
- Fody everywhere: Anotar.Serilog (`LogTo.*`), PropertyChanged, AsyncErrorHandler; one FodyWeavers.xml per project.
- SDK 10.0.401 (`global.json`, rollForward latestFeature); test runner is Microsoft.Testing.Platform.
- Every in-repo library is a ProjectReference; no internal NuGet.
- Large types split into partial classes by concern; MIT license region header on legacy files.
- Docs are bilingual: update both EN and zh-CN (`*.zh-CN.md` twin or sections in one file).

## ANTI-PATTERNS (THIS PROJECT)
- Never put package versions in a csproj; never add x86/AnyCPU; never disable TreatWarningsAsErrors to go green.
- Never break the plugin API (`ISuperMemoAssistant`, `ISMAPlugin`): additive only, and a new member needs a minor version bump.
- Never load plugins in-process; never reintroduce .NET Remoting or Squirrel.
- No remote telemetry of any kind (crash reporting, analytics, device IDs, Sentry). Errors go only to local logs (`%UserProfile%\SuperMemoAssistant\Logs`, tray menu "Open logs folder") that users attach to issues by hand.
- Keep SuperMemo internals (hooking, memory layout, symbols, binary patching) out of committed docs and AGENTS.md; they belong in gitignored `local/`.
- Core and Interop never reference Themes; the Themes engine never references Core/Interop/WPF/Plugins (ArchitectureTests enforce this).
- Themes is feed-only: keep it out of `pack.ps1` `$bundledPlugins` (OptionalComponentTests).
- Never hand-edit vendored or generated code: libs/pngcs, its Doxygen html, ImageOcclusion/svgedit/, Themes.Tests/Golden/, */external/*, *.Designer.cs.

## COMMANDS
```powershell
dotnet restore SuperMemoAssistant.slnx
dotnet build SuperMemoAssistant.slnx --no-restore -c Release
dotnet test --solution SuperMemoAssistant.slnx --no-build -c Release
$env:SMA_E2E='1'; dotnet test --solution SuperMemoAssistant.slnx; $env:SMA_E2E=$null   # opt-in E2E, real SuperMemo 20 x64
dotnet tool restore                                   # vpk
pwsh build/pack.ps1 -Version <x.y.z>                  # Velopack installer, bundles plugins
pwsh build/pack-plugins.ps1 -BaseUrl <url>            # plugin NuGet v3 feed + plugins.json
dotnet publish src/Tools/SuperMemoAssistant.CardTool -c Release -o artifacts/tools/sma-cards
```

## NOTES
- Dev app output goes to `artifacts/app-dev/`; bundled plugins live in `Plugins/` next to the exe.
- Env vars: `SMA_E2E`, `SMA_APPDATA_DIR`, `SMA_SM_ROOT`, `SMCARDS_ROOT`.
- CI runs on push to main, PRs, and tags `v*`. A tag triggers the GitHub Release (the update feed), then publishes the plugin feed to Pages.
- Core/Interop depend on libs/PluginManager (PluginHost, Remoting) and libs/Extensions.System.IO outside src/.
- Child AGENTS.md files:
  - src/Core/AGENTS.md
  - src/Core/SuperMemoAssistant.Core/SuperMemo/AGENTS.md
  - src/Interop/AGENTS.md
  - src/Plugins/AGENTS.md
  - src/Plugins/SuperMemoAssistant.Plugins.PDF/AGENTS.md
  - src/Services/AGENTS.md
  - src/Tests/AGENTS.md
  - src/Themes/AGENTS.md
  - libs/AGENTS.md
  - libs/PluginManager/AGENTS.md
