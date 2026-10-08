# src/Themes - theme engine (SuperMemoAssistant.Themes)

## OVERVIEW
Pure .NET library that themes SuperMemo 20: palettes, VCL style (.vsf) recoloring, form resources, card CSS, and applying themes to the SuperMemo install. ~7.6k LOC. Consumed by Plugins.Themes (UI) and Tools/CardTool (sma-cards CLI). Distinct domain + enforced contract.

## STRUCTURE
```
SuperMemoAssistant.Themes/
├── Assets/     # themes.json (576 palettes, 497 KB), native-themes.txt (38 curated ids), NOTICE.md - embedded resources
├── Library/    # ThemeLibrary.Load/Find, ThemeEntry, Palette, StyleNames
├── Import/     # ThemeImporter -> Base16Importer, VsCodeImporter (Jsonc), ObsidianImporter/Cascade/Base, CssSheet
├── Install/    # ThemeInstaller (PrepareLaunch/SyncAfterExit), ExePlan, CardSync, SafeWriter, ThemeContracts
├── Exe/        # executable resource handling (details: local/integration-internals.md)
├── Editing/    # CardCommandLine + card model/backups/diff, ThemeCommands
├── Styles/     # Vsf, VsfFile/Reader/Recolor (zlib VCL styles)
├── Forms/      # form resource reader
├── Colors/     # ColorMath, CssColor, CalcExpression
└── Cards/      # CardCss, CardRules, IniText
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Add/fix a palette source | Import/*Importer.cs | user imports land in user-themes.json, same id overrides built-in |
| Apply theme at SuperMemo start | Install/ThemeInstaller.cs | install changes only while SuperMemo is closed |
| Exe/ and Forms/ internals | local/integration-internals.md | gitignored, maintainer-only; may be absent |
| Card editing CLI | Editing/CardCommandLine.cs | verbs: list, show, element-color, css, html, theme, backups, restore, snapshot, diff |
| Shipped palettes | Assets/themes.json | LogicalName must equal GetManifestResourceStream name |

## CONVENTIONS
- TargetFramework net10.0 (no -windows), Nullable + ImplicitUsings ON, unsafe allowed.
- Public surface = exactly 13 types (ADR 0003): ThemeException, AppliedState, CardCommandLine, ImportResult, InstalledStyle, SuperMemoInstall, ThemeEntry, ThemeImporter, ThemeInstaller, ThemeLibrary, ThemeReport, ThemeSettings, ThemeStatus. Everything else `internal` (InternalsVisibleTo both test projects).
- Contracts are records; stable APIs sealed. Binary parsing via ReadOnlySpan/BinaryPrimitives little-endian.
- Errors users can act on throw ThemeException.

## ANTI-PATTERNS
- NEVER reference WPF, SuperMemoAssistant.Interop/Core, or the plugin framework (Themes.Tests/ArchitectureTests fails).
- NEVER exceed 250 lines per engine file (ArchitectureTests) - split instead.
- NEVER write SuperMemo install files directly - use SafeWriter (backup first, running-SuperMemo check, AV retry).
- Adding a public type requires updating ADR 0003 and ArchitectureTests together.
- Don't hand-edit themes.json; keep NOTICE.md attribution in sync with sources.
