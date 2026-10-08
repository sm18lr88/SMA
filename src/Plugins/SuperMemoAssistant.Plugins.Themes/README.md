# SuperMemo Assistant Themes plugin

This plugin themes SuperMemo: the windows and dialogs, the cards, and the status bar. It uses a library of hundreds of color palettes.

This plugin is part of [SuperMemoAssistant 20 Community](../../../README.md). It is **optional and is not in the installer**. Install it from **Browse plugins** in the SMA settings. After you install it, restart SMA.

## Turn it on

1. Open the settings of the Themes plugin.
2. Select the themes to add to SuperMemo's **Window > Themes** list. **Check the curated set** selects the themes that look right as window styles.
3. Select **Active** on one theme to use it now.
4. Select **Apply themes to SuperMemo**, and then **Save**.

SMA applies the settings the next time SuperMemo starts through SMA. It cannot do it earlier, because `sm20.exe` can only be changed while SuperMemo is closed.

The plugin does nothing until you turn it on. While it is off and has never been on, it does not touch `sm20.exe`, also if another tool changed it.

## Import your own themes

The settings window has two buttons next to **Check the curated set**:

- **Import file...** reads one base16 or base24 `.yaml` file, or one VS Code theme `.json` file (comments and `include` chains are allowed).
- **Import folder...** reads a folder of `.yaml` schemes, a VS Code extension folder (it reads the themes listed in its `package.json`), or an Obsidian theme folder (a folder that holds `theme.css`).

The new themes appear in the list. Tick **Install** on them, as for any other theme, and then save. If a new theme has the id of a theme from another source, the new id gets a short source tag, so it does not replace that theme. A theme from the same source replaces its older copy, so you can import a folder again after you change it. A file in a folder that cannot be read is skipped, and the window tells you which one.

The plugin keeps your imports in `user-themes.json` in its own data folder: the folder named after the plugin in the `Configs` folder of the SMA data. Obsidian themes need the default styles of Obsidian. The plugin reads them from an installed Obsidian and caches them in `obsidian-base.css` in the same folder. If Obsidian is not installed, the plugin uses built-in approximate defaults and tells you. You can set `OBSIDIAN_ASAR` to the `obsidian.asar` file to get the exact ones.

An import only adds themes to the list. It never changes `sm20.exe`. That happens at the next start of SuperMemo, if **Apply themes to SuperMemo** is on.

## What it changes

| What | How |
| --- | --- |
| `sm20.exe` | SMA rebuilds it from an untouched original. It adds the window styles of your themes, switches on theming for the card area and the status bar, and (on the supported build) makes cards follow a theme that you pick in **Window > Themes** at once. |
| `bin\themes\<style>.css`, `bin\DarkMode.css`, `bin\LightMode.css`, `bin\supermemo.css` | Card colors for each style. Your fonts and sizes stay. |
| `collection.ini`, `supermemo.ini` | The active theme, once, when you change the active theme in the settings. A theme that you pick later in SuperMemo is kept. |

Before the first change, SMA copies the original `sm20.exe` to `smcards-backups\exe\sm20.exe.original`. Every other changed file is copied to `smcards-backups\<collection>\` first. SMA checks that the program code of the new exe equals the original's before it replaces the exe. Nothing is written while a SuperMemo process runs.

After SuperMemo exits, SMA recolors the cards to the theme that SuperMemo saved, because SuperMemo saves a theme picked in **Window > Themes** only when it exits.

## Turn it off

Clear **Apply themes to SuperMemo** and save. On the next start SMA restores the original `sm20.exe` byte for byte. Do this before you uninstall the plugin. If you uninstall it first, the rebuilt exe stays, and `bin\themes` stays too. They do no harm, but you then need the original from `smcards-backups\exe` to undo them.

## Limits

- Some parts are painted by SuperMemo's own code with fixed colors, and no setting can change them: the cells of the Statistics grid (for example Memorized and Burden), the outstanding bar in the status bar, the "8.1%" and "4 days" labels of the learn bar, and the native title bar and menu bar. A style would change only the text color of the two learn-bar labels and leave it unreadable on their white background, so they keep SuperMemo's own look.
- While theming of the card area is on, the background color that you set for a single element in SuperMemo is ignored: the theme paints the card area.
- Instant theme switching works only on the SuperMemo 20 build of 2026-06-06. On another build, window styles and element theming still work, and the plugin tells you that instant switching was skipped.
- The plugin needs an SMA version that has launch hooks. On an older SMA it shows a message and does nothing.
- The plugin does not edit the text of single cards. For that, use the `sma-cards` console tool of the repository (`src/Tools/SuperMemoAssistant.CardTool`). It runs while SuperMemo is closed, and it is not part of the installer.

## How it works

The plugin is a thin shell around the `SuperMemoAssistant.Themes` engine. It registers a **launch hook** with SMA ([ADR 0003](../../../docs/decisions/0003-themes-optional-plugin-and-launch-hooks.md)). SMA runs the hook before it reads `sm20.exe` and again after SuperMemo exits.

The engine is a C# port of smcards, a Python tool that is retired now. The tests compare the engine with recordings of that tool (including real `sm20.exe` data and the importers), and check that SMA's compatibility check gives the same result for a rebuilt exe as for the original.

## Palettes

The palettes come from [tinted-theming/schemes](https://github.com/tinted-theming/schemes) (MIT) and from the themes built into Visual Studio Code (MIT). See `src/Themes/SuperMemoAssistant.Themes/Assets/NOTICE.md`.

## License

[MIT](../../../LICENSE)
