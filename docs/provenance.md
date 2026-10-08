# Project provenance and licenses

**English** | [简体中文](provenance.zh-CN.md)

This repository starts from the SuperMemoAssistant source code and build material that a user kept. The recovered material has no complete Git history. Therefore this repository is a source snapshot. It does not claim to match any single upstream commit.

## Upstream projects

- SMA: https://github.com/supermemo/SuperMemoAssistant (the root MIT license).
- PDF plugin: https://github.com/supermemo/SuperMemoAssistant.Plugins.PDF (the MIT license in the plugin folder).
- Interop: https://github.com/supermemo/SuperMemoAssistant.Interop
- PluginManager: https://github.com/alexis-/PluginManager.Net
- Process.NET: https://github.com/supermemo/Process.NET
- Extensions.System.IO: https://github.com/alexis-/Extensions.System.IO
- pngcs: https://github.com/supermemo/pngcs

The other plugins and the Services name their original projects in their own READMEs and project files. Each folder keeps its original license and copyright notices.

## PDF engine

- PDFium is the PDF engine of Chromium (BSD-3-Clause). The native `pdfium.dll` comes from the NuGet package `bblanchon.PDFium.Win32` (Apache-2.0), built by https://github.com/bblanchon/pdfium-binaries.
- `SuperMemoAssistant.Pdfium` is the managed binding of this repository (MIT).
- `SuperMemoAssistant.Pdfium.Wpf` derives from Apache-2.0 code. Its folder keeps the license and a `NOTICE` file.

## Themes

- `src/Themes`, `src/Plugins/SuperMemoAssistant.Plugins.Themes`, `src/Tools/SuperMemoAssistant.CardTool` and `skills/supermemo-appearance` are a C# port of the maintainer's smcards project, a Python tool. The Python code is retired and is not part of this repository (it was MIT licensed). The test recordings in `src/Tests/SuperMemoAssistant.Themes.Tests/Golden` are outputs of that code, and `Golden/README.md` says how they were made.
- The theme palettes in `src/Themes/SuperMemoAssistant.Themes/Assets/themes.json` come from [tinted-theming/schemes](https://github.com/tinted-theming/schemes) (MIT, each scheme belongs to its author) and from the themes built into Visual Studio Code (MIT, Microsoft Corporation). See `src/Themes/SuperMemoAssistant.Themes/Assets/NOTICE.md`.
- The Themes engine was developed and tested against a local SuperMemo 20 installation. No part of SuperMemo is stored in this repository: the golden files hold hashes only.

## What the public source excludes

The public source does not contain binary dependencies, NuGet caches, signing keys, or local configuration.
