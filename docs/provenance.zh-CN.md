# 项目来源与许可

[English](provenance.md) | **简体中文**

> 本文档是英文 [provenance.md](provenance.md) 的中文译本。英文版是主要版本;如两者不一致,以英文版为准。

本仓库以用户保留的 SuperMemoAssistant 源码及构建资料为基础。恢复资料没有完整的 Git 历史,因此本仓库是源码快照,不声称与上游某一提交完全对应。

## 上游项目

- SMA:https://github.com/supermemo/SuperMemoAssistant(根目录 MIT 许可)。
- PDF 插件:https://github.com/supermemo/SuperMemoAssistant.Plugins.PDF(插件目录中的 MIT 许可)。
- Interop:https://github.com/supermemo/SuperMemoAssistant.Interop
- PluginManager:https://github.com/alexis-/PluginManager.Net
- Process.NET:https://github.com/supermemo/Process.NET
- Extensions.System.IO:https://github.com/alexis-/Extensions.System.IO
- pngcs:https://github.com/supermemo/pngcs

其余插件与 Services 的原项目在各自的 README 和工程文件中注明。各目录保留原许可和版权声明。

## PDF 引擎

- PDFium 是 Chromium 的 PDF 引擎(BSD-3-Clause)。原生 `pdfium.dll` 来自 NuGet 包 `bblanchon.PDFium.Win32`(Apache-2.0),由 https://github.com/bblanchon/pdfium-binaries 构建。
- `SuperMemoAssistant.Pdfium` 是本仓库的托管绑定(MIT)。
- `SuperMemoAssistant.Pdfium.Wpf` 源自 Apache-2.0 代码,其目录保留许可文件和 `NOTICE` 文件。

## Themes

- `src/Themes`、`src/Plugins/SuperMemoAssistant.Plugins.Themes`、`src/Tools/SuperMemoAssistant.CardTool` 和 `skills/supermemo-appearance` 是维护者的 smcards 项目(一个 Python 工具)的 C# 移植。Python 代码已退役,不在本仓库中(它采用 MIT 许可)。`src/Tests/SuperMemoAssistant.Themes.Tests/Golden` 中的测试记录数据是该代码的输出,`Golden/README.md` 说明了它们的生成方式。
- `src/Themes/SuperMemoAssistant.Themes/Assets/themes.json` 中的配色来自 [tinted-theming/schemes](https://github.com/tinted-theming/schemes)(MIT,每个方案归其作者所有)和 Visual Studio Code 内置的主题(MIT,Microsoft Corporation)。见 `src/Themes/SuperMemoAssistant.Themes/Assets/NOTICE.md`。
- Themes 引擎是针对本机的 SuperMemo 20 安装开发和测试的。本仓库不保存 SuperMemo 的任何部分:黄金文件只含哈希值。

## 公开源码不包含的内容

公开源码不包含二进制依赖、NuGet 缓存、签名密钥或本机配置。
