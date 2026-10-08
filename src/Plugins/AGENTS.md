# src/Plugins - first-party plugins

## OVERVIEW
16 plugin directories (14 entry plugins) loaded at runtime by PluginHost.exe (libs/PluginManager), each its own process. Own Directory.Build.props sets EnableDynamicLoading=true so every output carries its NuGet deps.

## STRUCTURE
```
Plugins/SuperMemoAssistant.Plugins.<Name>/
├── README.md                       # user-facing docs
├── src/SuperMemoAssistant.Plugins.<Name>/   # csproj + <Name>Plugin.cs (DevSandbox: flat, no src/)
└── libs/                           # Import (BrowserNativeHost, Import.Interop), PDF (Pdfium, Pdfium.Wpf - own AGENTS.md)
```
Plugins: Books, DevSandbox, Dictionary, Dictionary.Interop, Email, Formulation, ImageOcclusion, Import, Import.BrowserExtension (JS, no csproj), LateX (project LaTeX), LocalApi, OmniMemo, PDF, Template, Themes, Writing.

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| New plugin | copy Template/ | minimal SMAPluginBase<T> entry |
| Settings UI | <Name>Cfg : CfgBase<T> (Services.UI) | Forge.Forms attributes [Field]/[Action]; Svc.Configuration.Load<T>()/Save() |
| Cross-plugin API | Dictionary.Interop, Import/libs/Import.Interop | PublishService<IFoo,Impl>() / GetService<IFoo>() |
| HTTP API for external tools | LocalApi/Server/ (ApiPipeline, ApiEndpoints, LocalApiServer) | loopback, Host/Origin/token checks; SM writes serialized by SemaphoreSlim |
| EPUB/Kindle import | Books/ | hotkey Ctrl+Alt+Shift+K |
| RSS / browser import | Import/ + Import.BrowserExtension/ | native messaging host in libs/ |
| Card formulation rules | Formulation/ | IFormulationRule implementations |
| Theme SuperMemo | Themes/ | thin UI over src/Themes engine; optional, feed-only |
| Branch export/import (markdown/html) | Writing/ | BranchCompiler, OutlineImporter, HtmlToMarkdown |

## CONVENTIONS
- Entry class `<Name>Plugin : SMAPluginBase<T>`. Parameterless ctor; instantiated by reflection.
- Lifecycle: load config in OnPluginInitialized(); register hotkeys via Svc.HotKeyManager.RegisterGlobal(...) in OnSMStarted(bool).
- Services exposed to other processes extend PerpetualMarshalByRefObject.
- ProjectReferences use `$(RepoRoot)` paths. Nullable: ON in Books, Dictionary, LocalApi, OmniMemo, Themes, Writing; OFF in PDF, Template and older plugins.
- Tests that cover plugins live in src/Tests/SuperMemoAssistant.Tests/<Plugin>/ (LocalApi uses a fake ISuperMemoGateway).

## ANTI-PATTERNS
- NEVER edit ImageOcclusion/svgedit/ - vendored third-party SVG-Edit JS (359 files).
- Themes must stay OUT of build/pack.ps1 $bundledPlugins and IN build/plugin-feed/catalog.json (OptionalComponentTests).
- Don't add ReSharper ClassNeverInstantiated suppressions on entry classes - reflection instantiation is expected.
- Don't silently `?? new()` a missing config and continue with invalid state - validate after load.
