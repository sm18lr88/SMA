# 参与贡献

[English](CONTRIBUTING.md) | **简体中文**

感谢你的帮助。本指南说明如何构建项目、测试检查什么,以及一次修改必须包含什么。

## 构建与测试

需要 Windows x64 和 .NET 10 SDK;构建 NativeAOT 代理还需要 Visual Studio 的 C++ 工作负载。详见 [docs/build.md](docs/build.md)。

```powershell
dotnet build SuperMemoAssistant.slnx
dotnet test --solution SuperMemoAssistant.slnx
```

- 构建把每个警告都当作错误。干净的构建有 0 个警告。
- 端到端测试需要手动开启(`SMA_E2E=1`)。它们在隐藏桌面上启动 SuperMemo 的沙盒副本。发布前,以及修改启动、插件加载或 Themes 插件之后,请运行它们。
- 测试按以下顺序查找 SuperMemo:`SMA_SM_ROOT`,SMA 配置的 `sm20.exe`,`.kno` 关联程序。不要在代码、测试、文档或智能体技能中写入盘符或用户文件夹。部分规则有测试检查。

## 由测试强制的规则

- `Core` 和 `Interop` 不引用 Themes 引擎。引擎不引用 SMA、WPF 或插件代码,其公开类型是一份固定清单(`ArchitectureTests`)。只在有意为之时才增加公开类型,并在同一次提交中修改清单。
- Themes 引擎和 `sma-cards` 工具与原 Python 工具的记录数据(`src/Tests/SuperMemoAssistant.Themes.Tests/Golden`)对照。修改记录数据之前,请先阅读其中的 `README.md`。不要为了让失败的测试通过而替换记录数据。
- 智能体技能(`skills/supermemo-appearance`)不含盘符或用户文件夹,且链接都能打开(`SkillTests`)。

## 一次修改应包含什么

- **测试。** 修复要有一个没有它就会失败的测试。不稳定的测试就是缺陷:要找出原因。例如,读取时钟的测试必须把时钟作为参数传入。
- **技能。** 如果修改了 `sma-cards` 的命令或 Themes 插件的设置项,请在同一次提交中修改 `skills/supermemo-appearance`。
- **文档。** 面向用户的文字有英文和简体中文两份(`*.md` 和 `*.zh-CN.md`),请两份一起修改。凡是用户能察觉的变化,都要在 `CHANGELOG.md` 和 `CHANGELOG.zh-CN.md` 中增加条目。使用短句,一个术语只指一件事。
- **决策。** 结构上的变化(新进程、新边界、新的代码加载方式)需要在 `docs/decisions` 中写一份 ADR,并说明你否决了什么、为什么。
- **插件 API。** `ISuperMemoAssistant` 或 `ISMAPlugin` 的新成员属于增量改动。它需要提升次版本号(`Directory.Build.props`),并在更新日志中写明所需的最低 SMA 版本。

## 提交

每次提交只做一件逻辑上完整的事,并且每次提交都必须能构建。第一行是不带前缀的祈使句,例如 `Find SuperMemo from the sm20.exe that SMA is set up with`。正文说明原因。

## 发布

见[发布与插件源](docs/plugin-feed.zh-CN.md)和 [docs/build.md](docs/build.md) 中的“CI 与发布”。请使用 `git push origin main --follow-tags` 推送。

## 许可证

提交贡献即表示你同意你的贡献使用本项目的 [MIT 许可证](LICENSE)。
