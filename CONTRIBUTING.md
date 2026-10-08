# Contributing

**English** | [简体中文](CONTRIBUTING.zh-CN.md)

Thank you for helping. This guide says how to build the project, what the tests check, and what a change must include.

## Build and test

You need Windows x64, the .NET 10 SDK, and for the NativeAOT agent the Visual Studio C++ workload. Details are in [docs/build.md](docs/build.md).

```powershell
dotnet build SuperMemoAssistant.slnx
dotnet test --solution SuperMemoAssistant.slnx
```

- The build treats every warning as an error. A clean build has 0 warnings.
- The end-to-end tests are opt-in (`SMA_E2E=1`). They start a sandbox copy of SuperMemo on a hidden desktop. Run them before a release and after a change to launch, plugin loading, or the Themes plugin.
- Tests find SuperMemo from `SMA_SM_ROOT`, else from the `sm20.exe` that SMA is set up with, else from the `.kno` association. Do not write a drive letter or a user folder into code, tests, docs, or the agent skill. Tests check some of these rules.

## Rules that tests enforce

- `Core` and `Interop` do not reference the Themes engine. The engine references no SMA, WPF, or plugin code, and its public types are a fixed list (`ArchitectureTests`). Add a public type only on purpose, and change the list in the same commit.
- The Themes engine and the `sma-cards` tool are compared with recordings of the former Python tool (`src/Tests/SuperMemoAssistant.Themes.Tests/Golden`). Read its `README.md` before you change a recording. Never replace one to make a failing test pass.
- The agent skill (`skills/supermemo-appearance`) names no drive or user folder, and its links resolve (`SkillTests`).

## What a change includes

- **Tests.** A fix has a test that fails without it. A flaky test is a bug: find the cause. For example, a test that reads the clock must take the clock as a parameter.
- **The skill.** If you change a `sma-cards` command or a setting of the Themes plugin, change `skills/supermemo-appearance` in the same commit.
- **Docs.** User-facing text exists in English and Simplified Chinese (`*.md` and `*.zh-CN.md`). Change both. Add an entry to `CHANGELOG.md` and `CHANGELOG.zh-CN.md` for anything a user can notice. Use short sentences and one term for one thing.
- **Decisions.** A change of structure (a new process, a new boundary, a new way to load code) needs an ADR in `docs/decisions`. Say what you rejected and why.
- **Plugin API.** A new member of `ISuperMemoAssistant` or `ISMAPlugin` is additive. It needs a minor version (`Directory.Build.props`) and a changelog entry that names the minimum SMA version.

## Commits

One logical change per commit, and every commit must build. The first line is a plain imperative sentence without a prefix, for example `Find SuperMemo from the sm20.exe that SMA is set up with`. The body says why.

## Releases

See [Releases and the plugin feed](docs/plugin-feed.md) and "CI and release" in [docs/build.md](docs/build.md). Push with `git push origin main --follow-tags`.

## License

By contributing, you agree that your contribution uses the [MIT License](LICENSE) of this project.
