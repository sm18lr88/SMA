---
name: supermemo-appearance
description: "Change how SuperMemo 20 and its flashcards look, on disk and without the SuperMemo UI: inspect and restyle cards, and theme SuperMemo's own windows. Use when the user wants to change how their SuperMemo items, cards, or elements look or read: element/window background colors, card (HTML component) background, text color, fonts, sizes, cloze styling, stray pasted colors, find/replace across cards, listing or viewing cards, or undoing such edits. Also use to theme SuperMemo 20's own windows (skins, dark mode, light mode) with palettes such as Catppuccin, Dracula, Nord, Gruvbox, Solarized, Tokyo Night, Rosé Pine, or VS Code Dark+/Dark Modern/Monokai, to choose which themes are installed or active, and to import base16/base24, VS Code, or Obsidian themes. Also use for sm20.exe, collection folders, compon.dat, supermemo.css, or DarkMode.css questions. Skip spaced-repetition algorithm, scheduling, priorities, grades, and statistics work."
---

# SuperMemo Appearance

Two tools do this work, and both are part of SuperMemoAssistant (SMA). There is no Python.

- **`sma-cards`** edits the cards of a collection on disk, and browses themes. SuperMemo has no API for this, so it edits the collection's own files, with backups.
- **The Themes plugin of SMA** themes SuperMemo's windows, cards, and status bar. It runs by itself every time the user starts SuperMemo through SMA. You do not run it.

## Run `sma-cards`

```
sma-cards <command> [...]
```

Find `sma-cards.exe` first: `Get-Command sma-cards` (it can be on PATH), else the `artifacts\tools\sma-cards` folder of the user's SMA checkout. If neither exists, build it in the SMA checkout with `dotnet publish src\Tools\SuperMemoAssistant.CardTool -c Release -o artifacts\tools\sma-cards`, and ask the user where the checkout is if you do not know. `sma-cards --help` prints all commands.

You never need to know where SuperMemo is. The tool finds the SuperMemo folder, in this order: `--sm-root "<SuperMemo folder>"`, the env variable `SMCARDS_ROOT`, the `sm20.exe` that SMA is set up with, the program registered for `.kno` files. Only when all four fail does it ask for `--sm-root`; then ask the user where SuperMemo is installed. `sma-cards theme status` prints the path of the `sm20.exe` it found. Everything below that says `<SuperMemo folder>` means the folder of that file.

Global options go before the command: `--collection "<folder or name part>"` (default: the last collection SuperMemo opened) and `--sm-root`.

## Safety rules

1. Always run a command without `--apply` first. It prints a preview and writes nothing. Summarize the preview for the user.
2. `--apply` refuses while `sm*.exe` runs, because SuperMemo keeps the collection in memory and overwrites the files on exit. Ask the user to close SuperMemo. Never start, stop, or kill SuperMemo or SMA yourself.
3. Each `--apply` copies the original files to `<SuperMemo folder>\smcards-backups\<collection>\<timestamp>-<label>\` first. `backups` lists them. `restore <name> --apply` puts them back and backs up the current state before it does that.
4. Do not edit scheduling or learning data: `info\` files other than `compon.dat`, `alg17\`, `history\`, `stats\`, `subsets\`, or repetition fields. The tool does not touch these files. Do not write ad-hoc scripts that change them.
5. After the first apply of a new kind of change, ask the user to open SuperMemo and confirm the result.

## Two different "backgrounds"

- **Element background** is the window area behind the card components. It is stored per element in `info\compon.dat`. Change it with `element-color`.
- **Card background** is the inside of each HTML component, where the question or answer text shows. It comes from the shared stylesheet rule `BODY`. Change it with `css set BODY background-color ...`.

## Request → command

| User wants                                             | Command                                                                                                                                      |
| ------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| See what exists                                        | `list` (filters: `--kind item`, `--grep text`, `--elements '53,57,60-81'`, `--template Item`), `list --templates`, `show 57`                 |
| Element background for all items                       | `element-color '#1E2A38' --kind item` (add `--with-templates` so template-based new elements match)                                          |
| Restore theme default background                       | `element-color default ...`                                                                                                                  |
| Card background, text color, font, size                | `css set BODY background-color '#FDF6E3'`, `css set BODY color '#222222'`, `css set BODY font-family Georgia`, `css set BODY font-size 14pt` |
| Cloze or extract look                                  | `css show`, then `css set .Cloze ...`, `.clozed`, `.Extract`, `.Highlight`                                                                   |
| Remove a stylesheet property                           | `css unset BODY font-size`                                                                                                                   |
| Pasted colors or fonts inside cards override the theme | `html strip-style background-color color font-family font-size`                                                                              |
| Fix wording across cards                               | `html replace 'old' 'new'` (`--ignore-case`, `--regex` with `\1` groups)                                                                     |
| Undo                                                   | `backups`, then `restore <name>` and `restore <name> --apply`                                                                                |

Add `--apply` only after the user agrees with the preview. Narrow every write with selection flags when the user means "my cards" and not the whole collection.

## Facts that affect results

- SuperMemo 20 copies `bin\DarkMode.css` or `bin\LightMode.css` over the live `bin\supermemo.css` when the user switches the theme. `css` commands change the live file and the active theme file by default. Use `--mode both` to change both themes, or `--mode light`/`--mode dark` for one theme.
- When the Themes plugin is on, it writes the card colors of the active theme into those files at every start and after SuperMemo exits, so a hand edit of `BODY` colors is replaced. Change the theme instead (below), or edit other properties such as fonts.
- Inline styles inside a card (for example `<FONT style="BACKGROUND-COLOR: ...">` from pasted text) override the stylesheet. Use `html strip-style` on them.
- Components marked `[inline]` in `list` keep their short text only in `registry\text.rtx`. The tool shows this text but cannot edit it. Tell the user to edit these in SuperMemo.
- Elements that the user adds later get SuperMemo's defaults. Run `element-color` again for them, or include `--with-templates`.
- After `html replace`, the titles in SuperMemo's Contents window can show the old text until SuperMemo updates them.
- In PowerShell, quote comma lists (`--elements '53,57'`) and `#` colors.

## Themes for SuperMemo's windows and cards

A theme from the library (shipped base16/base24 and VS Code palettes, plus any the user imported) can change four things:

- **Window style:** the toolbars, panels, lists, grids, buttons, and dialogs. The plugin adds your themes to SuperMemo's own list of window styles. Installed styles appear in SuperMemo under **Window → Themes**, next to the built-in styles.
- **Element area:** the area around the cards and the status bar. The plugin switches theming on for them (setting `ThemeElements`). Some parts of SuperMemo, such as the Statistics grid cells, the outstanding bar, the labels of the learn bar ("8.1%", "4 days"), and the title and menu bar, keep their own colors. While this switch is on, per-element colors (`element-color`, compon.dat) are ignored.
- **Card colors:** the palette's card rules (`BODY`, `.Cloze`, `.clozed`, `.Extract`, `.Highlight`, links, ...) are written to one stylesheet per window style: `bin\themes\<style name>.css`. Each file starts from the user's `DarkMode.css`/`LightMode.css`, so fonts and sizes stay as they are.
- **Live switching:** after the user picks a theme in **Window → Themes**, all cards change within about 2 seconds. This works only on the SuperMemo 20 build of 2026-06-06; on any other build the plugin skips it and says so.

### How it runs

The user starts SuperMemo through SMA (SMA's own start, or a shortcut to it). SMA opens the collection. Before SuperMemo starts, the plugin rebuilds `sm20.exe` from the untouched original (`smcards-backups\exe\sm20.exe.original`) with the chosen styles, and writes the card stylesheets. When SuperMemo exits, it recolors the cards to the theme SuperMemo saved. There is nothing to run by hand, and the result is the same on every start.

### The settings file

`<SMA data folder>\Configs\SuperMemoAssistant.Plugins.Themes\ThemesCfg.json`. SMA's data folder is `SuperMemoAssistant` inside the folder named by the env variable `SMA_APPDATA_DIR`; if that is not set, inside the `AppDataDirPath` of `%USERPROFILE%\supermemoassistant.json` when that file exists; otherwise inside `%USERPROFILE%`. The same folder holds the user's own library, `user-themes.json`, and the Obsidian cache, `obsidian-base.css`.

| Key                                                         | Meaning                                                                                                                          |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`                                                   | `true`: SMA keeps `sm20.exe` and the cards themed. `false`: at the next start SMA restores the original `sm20.exe`.             |
| `ActiveThemeId`                                             | The theme to use now (a library id, for example `gruvbox-dark-soft-vs-code`). Dark themes fill the dark slot and turn dark mode on; light themes fill the light slot and turn it off. It is written to the collection once, when the value changes. |
| `InstalledThemeIds`                                         | The themes that SuperMemo's own **Window → Themes** list gets. The active theme is always added.                                 |
| `ThemeElements`, `LiveSwitching`                            | The element-area switch and live switching (both `true` by default).                                                             |
| `Engaged`, `AppliedActiveThemeId`, `ManagedStyleResources`  | The plugin's own bookkeeping. Do not edit them.                                                                                  |

### Request → action

| User wants                          | Do this                                                                                                                                                                                                                                  |
| ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Browse the library                  | `sma-cards theme list --search gruvbox`, `--variant light`, `--source "VS Code"`. `sma-cards theme show "Tokyo Night Dark"` prints the id and every color role. `theme list` and `theme show` also include the user's imports.                |
| See what `sm20.exe` holds now       | `sma-cards theme status`: dark mode, the active light and dark styles, the element and live-switching settings, every style (built-in or added), and which library themes are in the exe.                                                         |
| Use another theme                   | With SMA and SuperMemo closed, set `ActiveThemeId` in `ThemesCfg.json` to the id from `theme show`. The change applies at the next start. Or the user opens the Themes settings in SMA, selects **Active** on a theme, and saves.        |
| Add themes to SuperMemo's own list  | Add their ids to `InstalledThemeIds` (same file, SMA closed), or use the settings window (**Install** boxes, **Check the curated set**).                                                                                                 |
| Import more themes                  | The settings window has **Import file...** (base16/base24 `.yaml`, a VS Code theme `.json`) and **Import folder...** (a folder of schemes, a VS Code extension, an Obsidian theme folder). Obsidian does not need to be installed.       |
| Turn theming off                    | Set `Enabled` to `false` (or clear **Apply themes to SuperMemo**). The next start restores the original `sm20.exe` byte for byte.                                                                                                        |
| Undo a bad result by hand           | With SuperMemo and SMA closed, copy `<SuperMemo folder>\smcards-backups\exe\sm20.exe.original` over `sm20.exe`, and `restore` the card files with `sma-cards backups` / `restore`. Then set `Enabled` to `false`, or the next start themes again. |

Theme rules:

1. Edit `ThemesCfg.json` only while SMA is closed. The plugin reads the file at every start and rewrites its bookkeeping keys after it applies a change.
2. Changes to `sm20.exe` are checked before they replace the file, and nothing is written while SuperMemo runs.
3. A SuperMemo update replaces `sm20.exe` and drops the added styles. The next start through SMA adds them again.
4. After you change the theme, ask the user to start SuperMemo through SMA and confirm how it looks. You cannot see it, and you must not start SuperMemo yourself.
5. Exact theme names are needed only when a search matches several themes. `theme show` lists the matches.

## Settings the tool does not know yet

If the user wants to change a SuperMemo setting that `sma-cards` has no command for, say so and ask them to change it in SuperMemo. Do not edit unknown files by hand.

## Local notes

The maintainer may keep extra technical notes in a `references` folder next to this file. They are not part of the repository, so this skill works without them.
