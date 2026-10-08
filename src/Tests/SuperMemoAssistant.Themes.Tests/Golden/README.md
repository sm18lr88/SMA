# Golden data

These files are recordings of the Python implementation of smcards, the tool that the C# theme engine replaces. Each file holds inputs and the outputs that the Python code gave for them. The C# tests give the same inputs to the C# code and compare the outputs. This is how the port was proved equal, and it keeps the C# code equal to a behavior that already worked on a real installation.

| File | What it records |
| --- | --- |
| `colors.json` | Color parsing and math, CSS `calc()` and `var()` |
| `vsf.json` | VCL style files: read, write, recolor (as hashes of the inflated data) |
| `dfm.json` | Form resources and the `StyleElements` edit |
| `pe.json`, `liveswitch.json` | Edits of the program file and live theme switching |
| `build.json` | The rebuilt `sm20.exe`: the parts that must equal the original |
| `library.json` | Palette completion, ids, style names, library lookup |
| `cards.json` | CSS rule editing, card rules, ini editing, settings plans |
| `import.json` | The base16/base24, VS Code, and Obsidian importers |
| `cardcli.json` | A scripted session of 52 card commands: output, exit code, and file hashes after every step |
| `cardunits.json` | The small algorithms of card editing (diff, replacement templates, HTML coding) |

The recordings contain hashes and small inputs only. They contain no part of `sm20.exe`.

## Change them only on purpose

The Python generators are not part of this repository. The maintainer keeps them locally. A recording changes in two cases:

- A fix that you want in the C# code and not in the Python code. Edit the recording by hand in the same commit as the fix, and say why in the commit message.
- A new test of a behavior that the Python code has. Run the generator from the tag, copy the new section here, and keep the other sections as they are.

Do not replace a recording to make a failing test pass.
