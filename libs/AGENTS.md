# libs/ - forked third-party libraries

## OVERVIEW
Forks of upstream libraries ported to .NET 10 / x64, built as part of `SuperMemoAssistant.slnx` (`/libs/` folder). Has its own file because it is a separate domain from `src/`: upstream code with upstream licenses and namespaces, plus high centrality (Extensions.System.IO is referenced by ~25 projects and PluginManager.Interop by ~22).

## STRUCTURE
```
libs/
├── Extensions.System.IO/  # NormalizedPath/FilePath/DirectoryPath (Wyam-derived), 8 files
├── PluginManager/         # plugin host + named-pipe RPC fork (own AGENTS.md)
├── Process.NET/           # Win32 process/window interop fork; sole consumer src/Core/SuperMemoAssistant.Core
└── pngcs/                 # VENDORED PngJ port, shared project; do not scan/refactor
    └── docs/html/         # GENERATED Doxygen output - never edit or index
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Path value types used across SMA | `Extensions.System.IO/src/NormalizedPath.cs` | abstract base; `FilePath`, `DirectoryPath` sealed |
| Relative path math | `Extensions.System.IO/src/RelativePathResolver.cs` | internal static |
| Path collections / comparison | `PathCollection.cs`, `PathEqualityComparer.cs` | `IReadOnlyList<TPath>` over `NormalizedPath` |
| PNG read/write | `pngcs/PngReader.cs`, `pngcs/PngWriter.cs` | line-oriented, no interlace support |
| Who consumes pngcs | `src/Services/SuperMemoAssistant.Services.Medias.Images` | only consumer (via `pngcs.projitems`) |
| Plugin RPC / host | `PluginManager/` | see child AGENTS.md |

## CONSUMERS (csproj references from src/)
- `Extensions.System.IO`: Core, Interop, nearly every plugin and service. Changing its API breaks the whole solution.
- `PluginManager.Interop`: Interop, every plugin, every service.
- `PluginManager.Core`, `Process.NET`: only `src/Core/SuperMemoAssistant.Core`.
- `PluginManager.Remoting`: Interop + `SuperMemoAssistant.Tests` (`InternalsVisibleTo`).

## CONVENTIONS
- Each lib keeps its upstream `LICENSE`/`LICENSE.txt` and README with a fork note. Keep upstream attribution headers (MIT, "Created On / Modified By") on touched files.
- Root namespaces mirror the folder (`Extensions.System.IO`, `Process.NET`, `PluginManager`), except that pngcs keeps upstream `Hjg.Pngcs.*`.
- Most libs target `net10.0-windows`; only `PluginManager.Remoting` is plain `net10.0` with `Nullable`/`ImplicitUsings` enabled. The other libs inherit the repo default (nullable off, implicit usings off).
- `#region` blocks (`Constructors`, `Methods`, `Properties & Fields - Non-Public`) are the house layout in the forked code; keep it when editing.
- Process.NET: version lives in `Properties/AssemblyInfo.cs` (keep `GenerateAssemblyInfo` off); full dispose pattern on every type owning native handles.
- pngcs is a shared project (`.shproj` + `.projitems`): its files compile INTO the consumer and have no own assembly or csproj.

## ANTI-PATTERNS
- Do NOT add NuGet packages for these libs: they are source forks on purpose (.NET 10/x64 ports).
- Do NOT edit or regenerate `pngcs/docs/html/` or `Doxyfile` output.
- Do NOT "fix" upstream TODO/hack comments in pngcs (`Zlib/ZlibOutputStreamMs.cs`, `PngWriter.cs`); they are inherited.
- Do NOT add interlaced-PNG writing: `PngWriter` forces `Interlaced = 0` by design.
- No test projects live in libs/; put lib tests in `src/Tests/SuperMemoAssistant.Tests` (Remoting already exposes internals to it).
