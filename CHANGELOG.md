# Community changelog

**English** | [简体中文](CHANGELOG.zh-CN.md)

## 3.1.2 - 2026-10-09

- **Command palette.** Ctrl+Alt+Shift+P opens the palette again. Before, the palette did not open while SuperMemo showed an element. The palette and the other windows that hotkeys open now come to the front and take the keyboard focus.
- **Hotkeys settings.** The SMA settings have a new Hotkeys tab. You can change the hotkeys of the command palette and of the settings window there, or restore their defaults. Plugins keep their hotkeys in their own settings.
- **Hotkeys of plugins.** Hotkeys that only work in the element window work again, and a hotkey that you clear and then set to a new key works at once.
- **Hotkey conflicts.** The Hotkeys tab tells you when a plugin already uses the key that you press, and keeps the old key.
- **Books.** The window of "Import a book or Kindle highlights" opens again. Before, it failed to open.
- **Plugin list.** The plugins that come with SMA are no longer marked as development plugins, show the Official label, and Image Occlusion shows its full name.
- **Plugin windows.** Every window and dialog of a plugin comes to the front when it opens, also from a hotkey. Before, some opened behind SuperMemo.
- **Browse Plugins.** The list no longer offers the plugins that come with SMA: they are already installed. The setup wizard says so too.
- **Settings windows.** The settings window of each plugin shows the plugin name in its title, and the change log in the About tab wraps long lines.
- **Update log.** The log file records the result of each update check.
- **Removed plugins.** SMA no longer installs the Email and OmniMemo plugins. They were never finished: Email did nothing, and OmniMemo showed an empty window that could not be closed.
- **PDF settings.** The settings window of the PDF plugin has the title "PDF Settings".
- **Messages.** The command palette shows a hint in its search box. Messages about empty lists and download errors are clearer and point to the issue tracker of this project.

## 3.1.1 - 2026-10-08

- **License screens.** The setup wizard and the Import plugin show the current license of this project, read from its `LICENSE` file, instead of an outdated copy from 2018. The Import plugin's warning about a SuperMemo bug no longer speaks for the former author.
- **What's new window.** The window that SMA shows after an update lists the changes of the 3.x versions, and its link opens the home page of this project.
- **Publisher.** The program files, the installer, and the plugin catalog name the SMA Community as publisher. The licenses of the Books, Formulation, Local API, and Writing plugins name their 2026 authors.

## 3.1.0 - 2026-10-07

- **Privacy: no telemetry.** SMA and its plugins no longer send anything to Sentry or to any other service. The plugins had built-in reporting keys of the former upstream project and reported errors without asking. The plugin browser no longer sends a device identifier. The `SMA_SENTRY_DSN` setting and the `SuperMemoAssistant.Services.Sentry` library are removed; plugins derive from `SMAPluginBase`. Errors are written only to the local log files. "Open logs folder" in the menu of the notification-area icon opens them, so that you can attach a log to an issue yourself.
- **Signed releases.** `Setup.exe` and the program files are signed with the project's own self-signed certificate. Windows SmartScreen still warns, because Windows does not trust a self-signed certificate; the [installation guide](docs/installation.md) shows how to check the thumbprint.
- **Fix: a hung launch hook delays the next ones.** Each launch hook now runs on its own thread, so a hook that never returns no longer keeps the hooks after it from starting within their time limit.
- **Fix: Windows Search errors during setup.** When the Windows index fails a query while SMA looks for SuperMemo, SMA logs a warning and continues with the other ways to find it, instead of logging an unhandled error.
- **Command palette.** Press Ctrl+Alt+Shift+P to find a command of SMA or of a plugin by its name, and to run it (upstream request #224). Typing a few letters is enough: "ibk" finds "Import a book or Kindle highlights". The palette shows the hotkey of each command, lists recent commands first, and has a command to open the settings of each plugin. It hides commands that cannot run in the window that you came from.
- **Palette commands (plugin API).** The global hotkeys of a plugin appear in the palette with no change to the plugin. `SMAPluginBase.RegisterPaletteCommand` adds a command without a hotkey. See [Notes for plugin authors](docs/plugin-authors.md).
- **Local API plugin.** A new bundled plugin runs an HTTP server on this computer only (127.0.0.1 and localhost, port 47321). Browser extensions, user scripts, and command-line tools can read the status and elements, create topics and items, and show an element. Every request needs the access token from the plugin settings. The server rejects web pages, other host names, and other computers. It is off until you turn it on. See the [plugin README](src/Plugins/SuperMemoAssistant.Plugins.LocalApi/README.md). This answers upstream issue supermemo/SuperMemoAssistant#263.
- **PDF plugin: extract titles.** A new text extract gets the beginning of its text as its title, so the knowledge tree shows what each extract is about. Image extracts and PDF extracts get their bookmark and pages. The "Extract titles" setting restores the article title, and another setting sets the maximum length (80 characters by default). The reference of an extract does not change (upstream issue #255).
- **PDF plugin: extract several sections at once.** The bookmark menu has "PDF Extract each subsection" and "PDF Extract this level". They create one PDF extract per section, with the bookmark title, for example one per chapter of a book. They skip sections that are already extracted and report the result (upstream request #245). A section now ends where the next section at the same or a higher level starts, also when the outline has entries without a page.
- **Themes plugin (optional).** The new Themes plugin themes the windows, cards, and status bar of SuperMemo from a library of color palettes. It is not in the installer: install it from "Browse plugins". It is off until you turn it on, because it rebuilds `sm20.exe` from an untouched original before each start. Turning it off restores the original. It replaces smcards, the Python tool and launcher that did this before: SMA applies the settings before SuperMemo starts and recolors the cards after it exits, and no Python is needed. See [ADR 0003](docs/decisions/0003-themes-optional-plugin-and-launch-hooks.md).
- **Theme import (Themes plugin).** The settings window of the Themes plugin imports your own themes: base16 and base24 schemes, VS Code themes and extensions, and Obsidian theme folders. The imports are kept in your own library file and never change `sm20.exe` by themselves.
- **`sma-cards` tool.** A new console tool (`src/Tools/SuperMemoAssistant.CardTool`, not in the installer) edits the cards of a collection while SuperMemo is closed: element colors, the card stylesheet, and text or inline styles in card HTML. Commands that change files are dry runs until you add `--apply`, and each one backs up the files first. `sma-cards theme list`, `theme show`, and `theme status` browse the palette library and show what `sm20.exe` holds now. The tool finds SuperMemo by itself, with no assumed location: `--sm-root`, then the `SMCARDS_ROOT` variable, then the `sm20.exe` that SMA is set up with, then the program registered for `.kno` files.
- **`supermemo-appearance` agent skill.** The skill that lets an AI agent drive `sma-cards` and the Themes plugin is now in the repository (`skills/supermemo-appearance`). It names no location on your machine, and a test checks that.
- **Launch hooks (plugin API).** `ISuperMemoAssistant.RegisterLaunchHook` lets a plugin run work while SuperMemo is closed: once before `sm20.exe` is read, and once after SuperMemo exits and before the plugins are stopped. A hook that fails or runs too long is reported and never blocks SuperMemo. This and the palette API are the reasons for the new minor version: plugins built for 3.0.0 still load, but a plugin that calls the hook needs SMA 3.1.0 or later. On SMA 3.0, palette calls are logged and have no effect.
- **Fix: crash when a plugin connection closes.** If the pipe of a plugin connection was disposed while SMA or `PluginHost` was still sending on it (for example when a remote object was released during shutdown), the process could stop with an unhandled `ObjectDisposedException`. The connection now treats this like any other closed pipe.
- **Formulation plugin.** The new formulation advisor checks items against 8 of the 20 rules of formulating knowledge. Press Ctrl+Alt+Shift+F to see its findings. An optional automatic check shows a notification only for warnings.
- **Writing plugin.** A new bundled plugin for incremental writing. Ctrl+Alt+Shift+W compiles the current branch into one Markdown or HTML document, with its images. Ctrl+Alt+Shift+M imports a Markdown outline as a branch of topics under the current element.
- **Books plugin.** The new Books plugin imports EPUB 2 and EPUB 3 books and Kindle "My Clippings.txt" highlights as incremental-reading topics. Press Ctrl+Alt+Shift+K in SuperMemo to open its import dialog.

## 3.0.0 - 2026-10-07

### Platform

- **SuperMemo 20 (x64).** SMA now supports SuperMemo 20 on .NET 10. When it starts, it checks that it can work with your SuperMemo build, and tests cover the verified build. SuperMemo 17-19.1 (x86) are no longer supported.
- **.NET 10 (x64).** The solution uses SDK-style projects, central package management, and `SuperMemoAssistant.slnx`. A new SuperMemo integration layer (NativeAOT) and Velopack replace the former integration libraries, Squirrel.Windows, and the custom MSBuild SDK, which are removed.
- **Plugin RPC.** `PluginManager.Remoting` is a named-pipe RPC with by-reference objects, events, delegates, and synchronous calls. It replaces .NET Remoting. Plugins must be rebuilt for `net10.0-windows`.

### PDF

- **PDFium engine.** `SuperMemoAssistant.Pdfium` binds the PDFium C API directly. It replaces a commercial SDK that had evaluation limits and could not be redistributed. See [ADR 0002](docs/decisions/0002-pdf-stack.md).
- **Bundled.** The installer includes the PDF plugin.
- **Viewer.** The WPF viewer is `SuperMemoAssistant.Pdfium.Wpf`. Typed characters reach form fields, and the form named actions NextPage, PrevPage, FirstPage, and LastPage work. The unused print and file-open toolbars are removed.
- **Extraction.** HTML extracts map each text run to its characters exactly. Unsaved changes go to `sma\PDF\Backups` when the element cannot be saved. Selections extend by word and to the end of the document. Copying to the clipboard retries when the clipboard is busy.

### Plugins, installer, and updates

- **Bundled plugins.** SMA loads plugins from a `Plugins` folder next to `SuperMemoAssistant.exe`. The installer includes seven plugins. A development plugin with the same name replaces a bundled plugin.
- **Plugin feed.** "Browse plugins" works again. A tag `vX.Y.Z` makes CI deploy a static NuGet v3 feed and catalog to GitHub Pages. Configs that name the retired upstream sources use the new feed. If a feed plugin and a bundled plugin have the same name, the higher version runs.
- **Updates.** `build\pack.ps1` builds a Velopack release. A tag `vX.Y.Z` makes CI publish it as a GitHub Release, which installed copies update from.
- **Compatibility gate.** SMA refuses plugins that target Interop older than 3.0.0, with the message "Outdated interop version".
- **Customization.** A plugin that must change the WPF application overrides `ConfigureApplication`.

### Fixes

- Every plugin crashed when it loaded, because the plugin base created a second WPF `Application`.
- Plugins did not ship their NuGet and native dependencies. PluginHost now loads them through the `deps.json` of each plugin.
- The registry, control, and element-window readers returned wrong values on x64.
- The update check cancelled itself before it ran, and it released a semaphore that it never took.
- `RemoteTask` hung when a callback threw an exception. `RemoteCancellationToken` lost earlier registrations.
- A `MarshalType` ordering bug sized `bool` as 4 bytes.
- `OnSMStarted` and `OnSMStopped` sent the `OnSMStarting` event to plugins.
- `AtFlags` did not match the values of SuperMemo.
- `KeyboardHook` did not raise `KeyboardPressed`.
- `IControlText.Text` threw `NotImplementedException` for plain-text components.
- Installed plugin packages stayed locked after SMA read them.
- SMA did not find its integration component when the path used `/`.
- Links in the UI did nothing.
- Recovered element files overwrote each other.
- A corrupt installed-plugins file stopped plugin loading. SMA now renames it to `.corrupt-<timestamp>` and continues with an empty list.

### Behavior

- SMA warns when SuperMemo does not open the files of the selected collection.
- SMA waits until the SuperMemo window is visible before it removes the title bar.
- A plugin that SMA stops exits after its reply is sent (`RpcConnection.RunAfterReply`).
- The agent suppresses the first `ShowWindow(SW_SHOWNORMAL)` call of SuperMemo.
- If the integration component fails to start, SMA stops connecting at once.
- Crash reporting of the app was opt-in through `SMA_SENTRY_DSN`, but the plugins still reported on their own. 3.1.0 removes all of it.
- `SMA_APPDATA_DIR` redirects the data folder of SMA.

### Breaking changes for plugin authors

- `SuperMemo17` → `SuperMemo20`, and every `*17` type → `*20` (for example, `SM17` → `SM20`).
- `RemotingServicesEx` → `RpcServices`. Its methods are `Connect`, `Serve`, and `NewPipeName`.
- `IPluginBase.ChannelName` → `PipeName`. The PluginHost option `-c/--channel` → `--pipe`.
- `SMAConst.Assembly.SMHookAgent` and `SMAFileSystem.HookAgentFile` replace the constants of the former integration library.
- `SMAPluginBase.DebuggerAttachStrategy` and `ActionProxy` are removed.

### Project

- CI builds and tests each push and pull request, and the build treats warnings as errors.
- All packages are at their current versions, and transitive pinning removes vulnerable versions.
- English is the primary documentation language. Chinese translations are in the `*.zh-CN.md` files.

## 19.1-community-r11.2 - 2026-10-06

- HTML source files are now unique, persistent files in the `sma/ImportedHtml` folder of the collection. This fixes missing files and failed PDF imports that came from the dependency on `Windows TEMP/sm_element_0.htm`.
- SMA checks that the file exists before it asks SuperMemo to import it. It logs the path and the file state before and after the import.
- The verified Unicode titles, BOM-free HTML, and hidden PDF metadata are unchanged.
- The user confirmed that two PDFs, with Chinese and Japanese file names, imported again successfully.
- The full package includes this Core fix and the updated in-app logs. A fresh installation, an upgrade, a recovery, and the backups passed checks in separate folders.

## r11.1

- The full installer supports in-place upgrades that keep the settings and the collection list. It also supports program recovery.
- Staging checks, backups of the old program and of the PDF DLL, and rollback on failure were added. The installer updates the PDF DLL only when its checksum is different.
- The installer checks that the SuperMemo program is the verified version.

## r9–r11

- Chinese titles are written through the Unicode interface. This fixes question marks in titles.
- SMA uses BOM-free HTML, entity escaping, and a compatible detection regular expression. The body shows the file name only once. The Base64 metadata is hidden.
- Exception handling for notification registration, an entry point for the full package installation, and a checksum list were added.

## Early 19.1 adaptation

- Compatibility with SuperMemo 19.1 was verified.
- The adaptation of PDF element creation was fixed. Failure messages, logs, and the release of the import lock were added.
