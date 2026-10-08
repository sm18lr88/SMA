# SuperMemo/ - in-process model of the running SuperMemo collection

## OVERVIEW
Host-side implementation of the Interop SuperMemo contracts: registries, elements, components, UI windows, and the integration engine (Hooks/). 98 files, ~13k LOC - largest single area of Core.

## STRUCTURE
```
SuperMemo/
├── SuperMemoRegistry.cs, SuperMemoUI.cs   # SuperMemoRegistryCore boots 9 registries
├── Common/        # version-neutral: Registry/, Elements/, Content/ (components + XAML controls), UI/, Extensions/
├── SuperMemo20/   # SM20 specifics: SuperMemo20.cs, Elements/, Files/, Registry/Members, UI/ElementWdw.cs
├── Hooks/         # integration engine; SMHookIOBase base class (details: local/integration-internals.md)
└── Natives/       # native interop declarations (details: local/integration-internals.md)
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Element cache / tracking | Common/Elements/ElementRegistryBase.cs | 1,082 LOC; ConcurrentDictionary<int, ElementBase>, mutex + AsyncManualResetEvent |
| Add a registry type | Common/Registry (RegistryBase<T>, *FileDescriptor) + SuperMemoRegistryCore | Element, Binary, Component, Concept, Text, Image, Template, Sound, Video |
| Registry file parsing | Common/Registry *FileDescriptor, SMConst.Files (Interop) | reads collection .mem/.rtx files |
| Element window automation | SuperMemo20/UI/ElementWdw.cs | 717 LOC |
| Component types (html, image, rtf, sound...) | Common/Content | ComponentBase -> Component*; XamlControl* for WPF layout |
| Hooks/ and Natives/ internals | local/integration-internals.md | gitignored, maintainer-only; may be absent |

## CONVENTIONS
- Hierarchy: PerpetualMarshalByRefObject (libs/PluginManager) -> SMHookIOBase -> RegistryBase<T> -> typed registry. Registries are handed to plugins across processes - keep them remotable.
- Version split: shared logic in Common/, anything sm20-specific in SuperMemo20/. Elements: ElementBase -> Item/Topic/Task/ConceptGroup.
- Namespaces: SuperMemoAssistant.SuperMemo.{Common|SuperMemo20|Hooks}.<Subsystem>; files use relative `using` (e.g. `using Builders;`).
- Region layout per file: License & Metadata, Constructors, Properties & Fields (Non-Public/Public), Properties Impl, Constants & Statics.

## ANTI-PATTERNS
- Do not touch the Elements dictionary outside ElementRegistryBase's locking/async events.
- Text.cs uses `#pragma warning disable CA1065`; don't copy that elsewhere.
- Do not read registry files directly while SuperMemo writes them; changes arrive via the registries' FileCreated/Written events.
