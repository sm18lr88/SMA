# SuperMemoAssistant 20 Community

**English** | [简体中文](README.zh-CN.md)

SuperMemoAssistant (SMA) adds plugins to **SuperMemo 20 (64-bit)** on **.NET 10**. This project continues [supermemo/SuperMemoAssistant](https://github.com/supermemo/SuperMemoAssistant) and [supermemo/SuperMemoAssistant.Plugins.PDF](https://github.com/supermemo/SuperMemoAssistant.Plugins.PDF). Credit belongs to the original authors and contributors, and this project keeps their MIT copyright notices. This is an independent community project. It is not an official upstream release.

To install SMA, see the [installation guide](docs/installation.md).

## Requirements

- Windows x64 and the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64). `Setup.exe` installs the runtime when it is missing.
- SuperMemo **20, 64-bit**. SuperMemo 19.1 is not supported.
- SMA is tested with SuperMemo 20. If your SuperMemo version is not supported, SMA tells you and does not connect to it.

## Plugins

The installer includes eleven plugins. They update together with SMA.

| Plugin | What it does |
| --- | --- |
| Books | Imports EPUB books and Kindle highlights as incremental-reading topics. |
| Dictionary | Looks up the selected word and creates elements from the definitions. |
| Email | Improves email support in SuperMemo. |
| Formulation | Checks items against the 20 rules of formulating knowledge and suggests how to improve them. |
| Image Occlusion | Creates image occlusion items. |
| Import | Imports web pages, browser tabs, and RSS or Atom feeds. |
| LaTeX | Renders LaTeX formulas in elements. |
| [Local API](src/Plugins/SuperMemoAssistant.Plugins.LocalApi/README.md) | Runs a token-protected HTTP server on your computer, so that browser extensions and scripts can add elements and navigate. It is off by default. |
| OmniMemo | Adds the OmniMemo window, which opens from any application with Alt+Shift+F. |
| PDF | Reads PDF files incrementally and extracts text, images, and page ranges. |
| Writing | Compiles a branch into one Markdown or HTML document, and imports a Markdown outline as a branch of topics. |

"Browse plugins" in SMA also installs and updates plugins from the [plugin feed](docs/plugin-feed.md).

**Command palette.** Press Ctrl+Alt+Shift+P to find a command of SMA or of a plugin by its name, and to run it. The palette also shows the hotkey of each command. It lists only the commands that can run in the window that you came from.

**Optional, not in the installer:** the [Themes plugin](src/Plugins/SuperMemoAssistant.Plugins.Themes/README.md) themes the windows, cards, and status bar of SuperMemo. It is off until you turn it on. To add themes, it modifies SuperMemo's own files. It keeps a backup of the originals, and turning the plugin off restores them. Install it from "Browse plugins".

## Documentation

- [Installation guide](docs/installation.md)
- [Build, test, and release notes](docs/build.md)
- [Releases and the plugin feed](docs/plugin-feed.md)
- [Notes for plugin authors](docs/plugin-authors.md)
- [Changelog](CHANGELOG.md)
- [Project provenance](docs/provenance.md)
- [Contributing](CONTRIBUTING.md)

## Testing

SMA has unit, integration, and end-to-end tests in `src/Tests`. The end-to-end tests are opt-in. They run against an isolated copy of SuperMemo in a hidden session. Hosted CI has no SuperMemo, so it skips them. See [docs/build.md](docs/build.md).

Automated tests do not cover:

- An update from one published version to the next. The update feed has no releases yet.
- Interactive use of the plugins, for example a PDF import. The tests check that each plugin loads and connects.
- How the Themes plugin looks in SuperMemo. A person reviewed screenshots, and no automated test judges the look.

## Build

```powershell
dotnet build SuperMemoAssistant.slnx
dotnet test --solution SuperMemoAssistant.slnx
```

The app builds into `artifacts\app-dev` (ignored by git). Building it needs the Visual Studio C++ workload. The build treats every warning as an error, and a clean build has 0 warnings. For details, see [docs/build.md](docs/build.md).

## Privacy

SMA sends no telemetry: no crash reports, analytics, usage data, or device identifiers. Errors are written only to log files on your computer, in `%UserProfile%\SuperMemoAssistant\Logs`. To report a problem, right-click the SMA icon in the notification area and select "Open logs folder". Check the log for private information, then attach it to a [GitHub issue](https://github.com/sm18lr88/SMA/issues).

SMA connects to the internet only to check for updates on GitHub, to list the plugins in "Browse plugins", and for plugin features that you use and that need it, for example dictionary lookups or web imports.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for how to build, what the tests enforce, and what a change must include.

## License

SMA and its plugins use the [MIT License](LICENSE) of the original project. Third-party dependencies keep their own licenses.

The PDF plugin uses PDFium (BSD-3-Clause), from the `bblanchon.PDFium.Win32` package (Apache-2.0). The PDF viewer control derives from Apache-2.0 code; see the `NOTICE` file in `src/Plugins/SuperMemoAssistant.Plugins.PDF/libs/SuperMemoAssistant.Pdfium.Wpf`.

Published files do not include the SuperMemo application, collections, personal settings, logs, or private licenses.

## Source layout

- `src/Core`: the app and its core.
- `src/Interop`: the plugin API.
- `src/Services`: shared services for the app and the plugins.
- `src/Plugins`: the plugins, the PDF engine, and its viewer.
- `src/Themes`: the engine of the Themes plugin and of `sma-cards`.
- `src/Tools`: the plugin feed generator, and `sma-cards`, a console tool that edits the cards of a collection while SuperMemo is closed.
- `skills/supermemo-appearance`: an agent skill for AI assistants that change how SuperMemo and its cards look. Link or copy the folder into your assistant's skills folder.
- `src/Tests`: unit, integration, and end-to-end tests.
- `libs`: libraries that SMA uses, such as the plugin manager.
- `build`: the release and plugin feed scripts.
