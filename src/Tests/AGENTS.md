# src/Tests - test projects

## OVERVIEW
Three projects: main xUnit suite, theme-engine suite, and a UI Automation agent used by E2E tests. ~10k LOC. Runner: xunit.v3 on Microsoft.Testing.Platform (global.json), OutputType Exe.

## STRUCTURE
```
Tests/
├── SuperMemoAssistant.Tests/          # Agent, Architecture, Books, Commands, E2E, Formulation, LaunchHooks, LocalApi,
│                                      # Memory, PDF, Pdfium, Plugins, Remoting, Setup, Symbols, Themes, Writing
├── SuperMemoAssistant.Themes.Tests/   # Cards, Colors, Editing, Exe, Forms, Import, Install, Library, Styles + Golden/
└── SuperMemoAssistant.Tests.UiAgent/  # WinExe; named-pipe JSON commands -> System.Windows.Automation
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Dependency/packaging guards | SuperMemoAssistant.Tests/Architecture/OptionalComponentTests | Core+Interop never ref Themes; pack.ps1 / catalog.json checks |
| Theme engine public contract | Themes.Tests/ArchitectureTests.cs | exactly 13 public types (ADR 0003), no WPF/SMA deps, engine files <250 lines |
| Real SuperMemo / installer runs | SuperMemoAssistant.Tests/E2E/ | SmaAppHarness, SuperMemoSandbox, HiddenDesktop, UiDriver, Registry/ShortcutGuard |
| UI automation verbs | Tests.UiAgent/Commands.cs | windows, wait-window, describe, exists, read, invoke, select, expand, set-value, screenshot... |
| Theme fixtures | Themes.Tests/GoldenData.cs, TestSandbox.cs, SuperMemoLocation.cs | sandbox fakes an SM install (TextOnlyInstall / InstallWithExe) |
| HTTP API tests | SuperMemoAssistant.Tests/LocalApi/ | fake ISuperMemoGateway, real HttpClient on free loopback port |

## CONVENTIONS
- Tests projects: Nullable + ImplicitUsings ON (unlike src); System.IO added as global using because UseWPF drops it.
- Access internals via InternalsVisibleTo("SuperMemoAssistant.Tests"/"...Themes.Tests") - don't make types public for tests.
- Opt-in E2E: `Assert.SkipUnless(Environment.GetEnvironmentVariable("SMA_E2E") == "1", ...)`. Needs SuperMemo 20 + built app; installer test needs artifacts/releases from build/pack.ps1. SuperMemo is found via `SMA_SM_ROOT`, else the configured sm20.exe, else the `.kno` handler; progress log `%TEMP%\sma-e2e.log`. Run command: root AGENTS.md COMMANDS (same as CONTRIBUTING.md).
- Tests needing a real sm20.exe skip (Assert.SkipWhen) when SuperMemoLocation finds none - CI stays green without SuperMemo.
- Locate repo root by walking up to SuperMemoAssistant.slnx, never by relative ../.. guesses.

## ANTI-PATTERNS
- NEVER hand-edit Themes.Tests/Golden/ fixtures to make a test pass - regenerate them deliberately and review the diff.
- No fixed sleeps in E2E - wait on windows/automation events via UiDriver / UiAgent wait-* commands.
- E2E tests must restore registry/shortcuts (RegistryGuard, ShortcutGuard) and run on HiddenDesktop.

## COMMANDS
```powershell
dotnet test --project src/Tests/SuperMemoAssistant.Tests --filter-namespace "SuperMemoAssistant.Tests.LocalApi"
dotnet test --project src/Tests/SuperMemoAssistant.Themes.Tests
```
