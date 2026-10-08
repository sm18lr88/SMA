# Installation

**English** | [简体中文](installation.zh-CN.md)

This guide describes how to install SuperMemoAssistant (SMA) for SuperMemo 20.

## Requirements

- Windows x64.
- SuperMemo 20, 64-bit.
- The .NET 10 Desktop Runtime (x64). `Setup.exe` installs it when it is missing.
- Free disk space. SMA and its plugins use approximately 85 MB. The .NET runtime needs more space if it is not installed.

SMA is verified with the SuperMemo 20 build `x64-6A2496CE-00C63200`. With other SuperMemo 20 builds, SMA checks at start whether it can work with them. If it cannot, it does not connect to SuperMemo and tells you why.

## Install

1. Save your work, and close SuperMemo and SMA.
2. Download `SuperMemoAssistant-win-Setup.exe` from the GitHub Releases of this repository.
3. Run `Setup.exe`. You do not need administrator rights.
4. On the first start, read the SMA license and select your collection.

Releases are signed with the project's own self-signed certificate. Windows does not trust a self-signed certificate, so SmartScreen can still show a warning. To continue, select "More info" and then "Run anyway".

To check that `Setup.exe` comes from this project, run `(Get-AuthenticodeSignature .\SuperMemoAssistant-win-Setup.exe).SignerCertificate.Thumbprint` in PowerShell, or open the file's Properties > Digital Signatures > Details > View Certificate > Details. The thumbprint must be `EBF4E4E3588D9EDFDEF5684F8F5D35879B8D7C6E`. The public certificate is [`build/signing/sma-codesign.cer`](../build/signing/sma-codesign.cer).

For an unattended installation, run `Setup.exe --silent`.

## Folders

```text
Program:                    %LocalAppData%\SuperMemoAssistant\current
Settings, plugins, logs:    %UserProfile%\SuperMemoAssistant
```

SMA runs only from the program folder above, or from an `app-dev` build folder. The portable zip does not run.

To move the settings folder, use one of these methods:

- Set the `SMA_APPDATA_DIR` environment variable to a folder. SMA then uses `<folder>\SuperMemoAssistant`. This method has priority.
- Create `%UserProfile%\supermemoassistant.json` with an `AppDataDirPath` value. If SMA cannot use that folder, it uses the default folder and writes the error to `%TEMP%\SuperMemoAssistant.log`.

## Plugins

The installer includes nine plugins: Books, Dictionary, Formulation, Image Occlusion, Import, LaTeX, Local API, PDF, and Writing. They update together with SMA.

**Themes** is an optional plugin that is not in the installer. It themes the windows, cards, and status bar of SuperMemo. It changes `sm20.exe`, so it is off until you turn it on in its settings. Install it with "Browse plugins" and restart SMA.

"Browse plugins" in the SMA settings lists the plugins of the plugin feed. There, you can install a plugin or update it. After you install a plugin that has the same name as a bundled plugin, restart SMA. SMA then starts the copy with the higher version.

A plugin must target .NET 10 and SMA 3.0 or later. SMA does not start older plugins, and it writes "Outdated interop version" to its log.

## Updates

SMA looks for updates on the GitHub Releases of this repository. The "Stable" channel accepts only stable releases. The "Beta" and "Nightly" channels also accept pre-releases. You can change the channel in the SMA settings.

## Build the installer

To build the installer from source, run these commands:

```powershell
dotnet build SuperMemoAssistant.slnx
pwsh build\pack.ps1
```

The installer and the release packages are written to `artifacts\releases`. For the build requirements, see [docs/build.md](build.md).

## Uninstall

1. Save your work, and close SuperMemo and SMA.
2. In Windows Settings, open "Apps", and uninstall "SuperMemo Assistant".

The uninstaller removes the program folder. It does not remove the settings folder. The elements that SMA added to your collection stay in your collection.

SMA does not change the SuperMemo program, with one exception: if you install the optional Themes plugin and turn it on, SMA rebuilds `sm20.exe` from an untouched copy before each start. Turn Themes off before you uninstall SMA or the plugin, so that SMA restores the original `sm20.exe`. See the [Themes plugin](../src/Plugins/SuperMemoAssistant.Plugins.Themes/README.md).

## SuperMemo 19.1

This version does not support SuperMemo 19.1. The former 19.1 release r11.2 cannot install or update this version.
