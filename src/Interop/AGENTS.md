# src/Interop - public plugin API (SuperMemoAssistant.Interop)

## OVERVIEW
The assembly every plugin compiles against: contracts, plugin base types, Svc locator, path/constant registries, shared utilities. ~192 files, ~20k LOC under src/SuperMemoAssistant.Interop/. Highest reference centrality in the repo.

## STRUCTURE
```
src/SuperMemoAssistant.Interop/
├── Interop/            # SMAConst, SMConst, SMAFileSystem, SMAExecutableInfo
│   ├── Plugins/        # ISMAPlugin, SMAPluginBase<T>, PluginHost, PluginApp.xaml
│   ├── SMA/            # ISuperMemoAssistant
│   └── SuperMemo/      # ISuperMemo + Content, Core, Elements, Learning, Registry, UI contracts (75 files)
├── Services/           # Svc / Svc<T>; Configuration/ (ConfigurationServiceBase); IO/ (hotkeys, keyboard hook)
├── Extensions/         # 32 *Ex static classes (StringEx, ProcessEx, RpcServices...)
├── Sys/                # SparseClusteredArray, Collections/ (BiDictionary), IO/ (IniFile 1,063 LOC), Windows/, Remoting/, Converters/
└── Exceptions/         # SMAException
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Plugin lifecycle hooks | Interop/Plugins/SMAPluginBase.cs | OnPluginInitialized, OnSMStarted(bool), PublishService<I,T> |
| Reach SMA/SM from a plugin | Services/Svc.cs | Svc.SM, Svc.Configuration, Svc.HotKeyManager, Svc.Plugin, OnSMAAvailable |
| Any app/data/plugin path | Interop/SMAFileSystem.cs | SMA_APPDATA_DIR env > pre-init JSON > %UserProfile%/SuperMemoAssistant |
| SuperMemo file/window names | Interop/SMConst.cs | Files, Paths, UI class names |
| Element/registry contracts | Interop/SuperMemo/** | implemented in Core/SuperMemoAssistant.Core/SuperMemo |
| Dev vs installed, SMA vs PluginHost | Interop/SMAExecutableInfo.cs | regex on exe path; IsDev = artifacts/app-dev |

## CONVENTIONS
- RootNamespace is `SuperMemoAssistant`; folders map to SuperMemoAssistant.Interop.*, .Services, .Extensions, .Sys.
- References libs/PluginManager (Interop + Remoting) and libs/Extensions.System.IO (DirectoryPath/FilePath types used by SMAFileSystem).
- GenerateDocumentationFile on; public members carry XML docs.
- Anything crossing the SMA <-> PluginHost boundary must be remotable (PerpetualMarshalByRefObject or serializable).
- Utilities go in Extensions/*Ex or Sys/, never in plugins when two plugins need them.

## ANTI-PATTERNS
- Breaking changes here break every plugin and the plugin feed - add members, don't rename/remove; mark [Obsolete] first.
- Do not reference Themes (OptionalComponentTests guards ISuperMemoAssistant's assembly).
- Svc statics are not thread-safe and tests must set them by hand - do not add more mutable statics.
- KeyboardHotKeyLegacy and SMConst.UI.MainMenuItemClassName are [Obsolete]; don't use in new code.
- SMAFileSystem writes config parse errors to %TEMP%/SuperMemoAssistant.log (bypasses Serilog) - look there when startup dies silently.
