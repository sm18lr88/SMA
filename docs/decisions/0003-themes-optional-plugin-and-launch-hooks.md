# 0003. Themes as an optional plugin, started through launch hooks

- Status: accepted (2026-10-07, when the Themes work was merged). The decision owner is the repository maintainer.
- Relates to: the plugin host design (plugins run in their own `PluginHost` processes).

## Context

smcards was a separate Python project (it is now retired, see below). It themed SuperMemo 20: it rebuilds `sm20.exe` from an untouched original to add window styles and theme switching, and it writes card stylesheets and the ini keys that pick a style. It can do this only while SuperMemo is closed. Its own launcher syncs card colors before SuperMemo starts and again after it exits.

SMA already owns the start of SuperMemo: it starts SuperMemo, connects to it, and exits when SuperMemo exits. Two launchers cannot both own the start.

Evidence from the code (2026-10-07):

- `SMA.StartAsync` loads the collection config, then checks that `sm20.exe` is compatible, then raises `OnCollectionSelected`, then starts the process. A plugin that reacts to `OnCollectionSelected` is too late to change the exe that SMA has already inspected.
- `SMA.OnSMStopped` raises the internal event first. Its subscribers clean up the plugin hosts and shut SMA down. The remote `OnSMStoppedEvent` that plugins receive comes after that.
- A plugin counts as started when its host process has connected (`ConnectPlugin`). The host calls `OnInjected` and so `OnPluginInitialized` afterwards. The first end-to-end log showed the hook registration after "1 started successfully". Awaiting the plugin start is therefore not enough for a hook that registers during initialization.
- The rebuilt exe keeps the build id and passes SMA's compatibility check like the original. Before this ADR only the README said so. Now a test proves it.

The earlier docs said "SMA does not change the SuperMemo program". That stays true by default, and stops being true for a user who installs and enables Themes.

## Decision drivers

1. Changes to `sm20.exe` must happen while SuperMemo is closed, and in a fixed order with SMA's compatibility check and with plugin shutdown.
2. A user who does not want a modified `sm20.exe` must never get one.
3. One launcher.
4. Installable through SMA, with no Python runtime on the user's machine.
5. The behavior must match what smcards already proved on a real installation.

## Views

| Module | Responsibility | Boundary | Runs in |
| --- | --- | --- | --- |
| `SuperMemoAssistant.Themes` | Theme policy, theme importers, card editing, and the file formats they edit (styles, forms, the program file, CSS, ini, compon.dat) | A library with 13 public types: the theme contract and importer, and `CardCommandLine` as the only door to card editing. It depends only on the .NET base class library. | The plugin's process, or the tool's |
| `SuperMemoAssistant.CardTool` (`sma-cards`) | A console front end of `CardCommandLine`: list and show elements, set element colors, edit stylesheets and card HTML, back up and restore | One `Program.cs` of a few lines. A test checks that it holds no logic and references only the engine | Run by a person or an agent, with SuperMemo closed. Not in the installer |
| `SuperMemoAssistant.Plugins.Themes` | Settings, the launch hook adapter, the settings window | The composition root of the feature | One `PluginHost` process, packaged for the plugin feed |
| `LaunchHookRunner` and `ISuperMemoAssistant.RegisterLaunchHook` | A generic seam: run plugin work around a SuperMemo start | Knows nothing about themes | The SMA process |
| `Golden` folder of the theme tests | Recordings of the former Python implementation: inputs and the outputs it gave | Not shipped | Test runs only. The Python code itself is retired and is kept by the maintainer outside this repository |

No new process or service exists. The plugin process already existed. Size is not the reason for any boundary here: the engine is a library, and the only deployable that changes is one plugin.

## Options

- **A. Keep the current shape.** smcards stays a separate Python tool with its own launcher. This is viable and stays available to anyone who wants only the CLI. Rejected as the answer: two launchers, a Python runtime, no install through SMA, and no tests in SMA's CI.
- **B. A plugin that uses the existing events.** Rejected: the ordering above is wrong for both the start and the exit.
- **C. Load hook assemblies into the SMA process.** Rejected: it gives up the crash isolation of the plugin host design, and adds a new loading mechanism.
- **D. A launch-hook seam over the existing plugin RPC (chosen).** The plugin registers delegates. SMA calls them at the right moments. Nothing new crosses a process boundary.
- **E. Bundle Themes in the installer.** Rejected: changing `sm20.exe` must be opt-in, so Themes ships only through the plugin feed.
- **F. Wrap the Python tool.** Rejected: it needs Python and `uv` on every machine. The code is ported to C#. The Python was kept only until the port was proved, and is now retired.

### Where card editing goes

Card editing (set the element background, edit the card stylesheet, replace text or strip inline styles in card HTML, back up, restore, diff) is a different job from theming, and it must run while SuperMemo is closed. SMA exits when SuperMemo exits, so a plugin cannot offer it as a command on demand.

- **Plugin command.** Rejected: SuperMemo is always running while the plugin can be used.
- **A queue applied by a launch hook.** Rejected: it hides the dry run, which is the safety feature of these commands, behind a restart.
- **Edits through the live SMA registry API.** Rejected: that is a new feature with different behavior, not the port of a proven one.
- **A console tool (chosen).** The engine hosts the logic, because backup, the closed-SuperMemo check and the stylesheet editing are shared with theming and need one write authority. Only `CardCommandLine` is public. The reader and the editors are internal and are tested through it.

Theme importers (base16/base24, VS Code themes and extensions, Obsidian themes) are part of the engine (`ThemeImporter`) and are used from the settings window of the plugin. They write the user's own library file in the data folder of the plugin.

## Decision

Choose D with the engine ported to C#, shipped as an optional feed-only plugin that is off until the user turns it on.

### Contract: `ISuperMemoAssistant.RegisterLaunchHook(name, beforeLaunch, afterExit)`

- Consumers are plugins. The owner is the SMA core. Both delegates are optional, but at least one is required.
- A hook registers while its plugin initializes. Before it runs any `beforeLaunch` hook, SMA calls `ISMAPlugin.WaitUntilInitialized` on every connected plugin, for up to 30 seconds each. The plugin base class answers when `OnPluginInitialized` has finished. A plugin built for an older SMA has no such member; the call fails and SMA treats that plugin as ready, because it cannot register hooks. A plugin that is installed while SMA runs registers too late for the current start, but its `afterExit` hook still runs.
- `beforeLaunch` runs once per SMA start, after the collection is chosen and before `sm20.exe` is inspected. `afterExit` runs once after the SuperMemo process exited, before the plugin hosts are stopped.
- Hooks run one at a time in registration order. Each has a time limit: 3 minutes before launch, 1 minute after exit.
- A hook that throws or exceeds its limit is logged and shown to the user. SuperMemo still starts and SMA still exits. A timed-out hook is abandoned, not cancelled. It can still be running when SuperMemo starts. The engine writes `sm20.exe` through a temporary file and an atomic move, so SuperMemo starts either the old or the new exe.
- A hook can run on every start, so it must be safe to repeat.
- Data crossing the boundary: two serializable classes with file paths only (`SMLaunchInfo`) and message strings (`LaunchHookResult`). Warnings become desktop notifications. Actions go to the log.
- Compatibility: the new member is additive. A plugin that calls it on an older SMA fails, so Themes catches that and tells the user to update SMA. The Interop and product version belongs to the release owner and is not changed here.

### Engine invariants

- `sm20.exe` is always built from the untouched original, and a verification check against the original must pass before the new exe replaces the old one. Restoring returns the original byte for byte.
- Every changed file is backed up first, under `smcards-backups` (the folder name smcards already uses, so an existing original backup is reused). Nothing is written while a SuperMemo process runs.
- Turning Themes off restores the original exe only if Themes turned it on (`AppliedState.Engaged`). An exe that another tool changed is left alone.
- Only styles that Themes installed are ever removed, and never a style that the collection still uses.
- A theme picked inside SuperMemo is kept: the chosen theme is written once, when the setting changes.

### Dependency direction

`Core` and `Interop` do not reference Themes. The plugin references the engine and the Interop. The engine references nothing of SMA, WPF, or the plugin framework. Tests enforce this.

## Consequences

- Positive: one launcher; the feature is installable and removable through SMA; the exe-rebuild logic runs in SMA's CI.
- Negative: the proof of equality is a set of fixed recordings. A behavior that the Python code had, and that no recording covers, is not checked. The .NET and Python compressors produce different bytes, so tests compare decompressed style data. The rebuilt exe and the one that the Python code built are equivalent, but not byte for byte.
- Not ported: `theme preview` (it wrote a bitmap and an HTML page), `theme verify` (screenshots on a hidden desktop, used as a one-off proof below), `theme use-style` (it activated a style that is inside `sm20.exe` but is not a library theme), `theme restore-exe` (the plugin restores when Themes is turned off), and `theme import --bundled` (a maintainer command for the shipped library). They remain in the former Python tool, which the maintainer keeps outside this repository. The `supermemo-appearance` agent skill now drives `sma-cards` and the Themes plugin, and it lives in this repository (`skills/supermemo-appearance`), so that it changes together with the tools it describes.
- Deferred on purpose: the controls that SuperMemo paints in code (the cells of the Statistics grid, the outstanding bar, the "8.1%" and "4 days" labels of the learn bar, the native title and menu bar). A screenshot showed that a style alone turns the two learn-bar labels into light text on white, so they are left out of the element switch.

## Transition

- Start: the Python smcards tool has changed the exe, or nothing has changed.
- Safe intermediate: Themes is installed and off. It has never engaged, so it leaves the exe alone. The settings window pre-checks the themes that are already in `sm20.exe`, so adopting them takes one click.
- Rollback: turn Themes off (the next start restores the original exe), or copy `smcards-backups\exe\sm20.exe.original` over `sm20.exe` while SuperMemo is closed.
- Card editing needs no transition: `sma-cards` reads and writes the same files and keeps the same backup folders as the Python CLI, so a backup made by the Python tool can be restored by `sma-cards`.
- Cleanup, done on 2026-10-07: the vendored copy `tools/smcards` is removed from this repository. The history of the Python code is kept by the maintainer outside this repository, and the separate smcards repository was retired.

## Confirmation

| Claim | Proof |
| --- | --- |
| The C# engine does what the Python code does: colors, style recoloring, form edits, exe edits, live switching, CSS, ini, card rules, library lookup | Test-backed: recordings of the Python code (the `Golden` folder), including hashes of recolored real styles and edited real forms |
| A rebuilt `sm20.exe` is equivalent to the reference build | Test-backed (real exe) |
| SMA's compatibility check gives the same result for a rebuilt exe as for the original | Test-backed, for four kinds of rebuild, with a control that shows the comparison can fail |
| The installer is idempotent, keeps in-app theme picks, removes only its own styles, restores on off, and does nothing while SuperMemo runs | Test-backed on a sandbox install |
| Hooks run before the compatibility check and before plugin shutdown, never block, and survive failures and time limits | Test-backed, including a real named pipe |
| Core and Interop do not reference Themes; the engine's public surface and references are as above; Themes is not bundled but is in the feed | Structurally enforced by tests, each shown to fail on a violation |
| The Themes plugin loads in a real `PluginHost` and registers its hook with the core | Runtime-backed (opt-in end-to-end test, `SMA_E2E=1`, hidden desktop) |
| A full session: SMA waits for plugin initialization, rebuilds the exe of a sandbox copy of SuperMemo, checks it, and starts a real SuperMemo from it that shows its windows; when that process ends, the after-exit hook runs before the plugin host stops | Runtime-backed (opt-in end-to-end test, sandbox copy, log order asserted). The "wait for initialization" rule exists because this kind of run exposed the race |
| Live theme switching works when SuperMemo draws cards, and the result looks right | Runtime-backed, by hand (2026-10-07): SuperMemo ran on a hidden desktop from an exe that the C# engine rebuilt, and the screenshots were reviewed. Cards showed a marker rule that exists only in `bin\themes\Nord.css`, never in `supermemo.css`, so the theme file was used. The card area and the status-bar panels were themed. **Not an automated test.** The same run found that the "8.1%" and "4 days" labels become light text on white, so they were taken out of the element switch |
| The theme importers read base16/base24, VS Code (files and extensions) and Obsidian like the Python importers did, and a cyclic `var()` does not hang them | Test-backed: recordings of the Python code over hand-written fixtures, and hashes of the output against an installed Obsidian when its build is the same. The cycle case is a fix in both implementations |
| The card commands behave like the Python CLI did | Test-backed: a scripted session of 52 steps (dry runs, applies, collisions, restore, diff, errors) compares output, exit code and the hashes of the files after every step. Golden data covers the diff algorithm, Python replacement templates, HTML decoding and encoding, slots and colors. A control shows that the session test fails when one format width changes |
| The card commands matched the Python CLI on a real collection | Runtime-backed, once (2026-10-07): 27 read-only commands against a real collection gave identical output. Not repeated by a test, because that collection changes whenever it is used |
| The engine takes over an install that the Python tool had changed, keeps the user's themes, and is idempotent | Runtime-backed, once (2026-10-07), on the maintainer's real install: the engine rebuilt `sm20.exe` from the original with the 70 styles the install already had (the shipped palettes and the user's imports, carried over to the plugin's data folder), and kept the active dark theme. `sma-cards theme status` read the result back (element theming on, live switching on). A second run changed no byte of the exe. A sandbox copy of that exe started on a hidden desktop and its screenshot showed the themed windows. The real SuperMemo was not started by the test |
| `sma-cards` and the engine keep their boundaries | Structurally enforced: the tool references only the engine and has a few lines of its own. The engine has no reference to SMA, WPF or the plugin framework, and its public surface is a fixed list |

## Reopen triggers

- A SuperMemo build that stops live theme switching from working.
- A need to run hooks for SuperMemo starts that SMA did not launch.
- A second plugin that needs launch hooks (the contract may need a priority or a veto).
- A need for one of the commands that were not ported (`theme preview`, `theme verify`): ask the maintainer for the former Python tool and port it.
