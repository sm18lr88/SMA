# Third-party data in `themes.json`

`themes.json` holds color palettes only (hex values and theme names), derived from:

| Source | Entries | License |
|---|---|---|
| [tinted-theming/schemes](https://github.com/tinted-theming/schemes) (base16 and base24 schemes) | `source` starts with `tinted-theming` | MIT. Each scheme belongs to its listed author. |
| Themes built into Visual Studio Code (Default Dark+/Dark Modern/Dark (Visual Studio), Monokai, Monokai Dimmed) | `source` starts with `VS Code` | MIT, Microsoft Corporation |

Themes that a user imports in the Themes plugin are kept in the user's own `user-themes.json`, in the data folder of the plugin. They are never part of this file.
`obsidian-base.css` in the same folder is a local cache of one machine's Obsidian defaults.
