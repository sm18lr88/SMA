# SuperMemoAssistant 20 Community

[English](README.md) | **简体中文**

> 本文档是英文 [README](README.md) 的中文译本。英文版是主要版本;如两者不一致,以英文版为准。

SuperMemoAssistant(SMA)为 **SuperMemo 20(64 位)** 提供插件,运行在 **.NET 10** 上。本项目延续 [supermemo/SuperMemoAssistant](https://github.com/supermemo/SuperMemoAssistant) 和 [supermemo/SuperMemoAssistant.Plugins.PDF](https://github.com/supermemo/SuperMemoAssistant.Plugins.PDF)。感谢原作者与贡献者,本项目保留其 MIT 版权声明。这是独立的社区项目,不是上游官方版本。

安装方法见[安装指南](docs/installation.zh-CN.md)。

## 运行要求

- Windows x64 和 [.NET 10 桌面运行时](https://dotnet.microsoft.com/download/dotnet/10.0)(x64)。缺少运行时时,`Setup.exe` 会自动安装。
- SuperMemo **20,64 位**。不支持 SuperMemo 19.1。
- SMA 使用 SuperMemo 20 测试。如果你的 SuperMemo 版本不受支持,SMA 会告诉你,并且不会连接它。

## 插件

安装程序包含十一个插件,随 SMA 一起更新。

| 插件 | 功能 |
| --- | --- |
| Books | 将 EPUB 图书和 Kindle 标注导入为渐进阅读主题。 |
| Dictionary | 查询所选单词,并根据释义创建元素。 |
| Email | 改进 SuperMemo 的电子邮件支持。 |
| Formulation | 根据知识表述的 20 条规则检查卡片,并建议如何改进。 |
| Image Occlusion | 创建图像遮挡卡片。 |
| Import | 导入网页、浏览器标签页以及 RSS 或 Atom 订阅。 |
| LaTeX | 在元素中渲染 LaTeX 公式。 |
| [Local API](src/Plugins/SuperMemoAssistant.Plugins.LocalApi/README.md) | 在本机运行受令牌保护的 HTTP 服务器,浏览器扩展和脚本可以借此添加元素并跳转。默认关闭。 |
| OmniMemo | 添加 OmniMemo 窗口,可在任意程序中按 Alt+Shift+F 打开。 |
| PDF | 渐进阅读 PDF 文件,并摘录文字、图像和页面范围。 |
| Writing | 将一个分支编译为一个 Markdown 或 HTML 文档,并把 Markdown 大纲导入为一个主题分支。 |

SMA 的“浏览插件”还可以从[插件源](docs/plugin-feed.zh-CN.md)安装和更新插件。

**命令面板:** 按 Ctrl+Alt+Shift+P,可以按名称查找并运行 SMA 或插件的命令。面板还显示每个命令的快捷键,并且只列出能在你刚才所在窗口中运行的命令。

**可选,不在安装程序中:** [Themes 插件](src/Plugins/SuperMemoAssistant.Plugins.Themes/README.md)为 SuperMemo 的窗口、卡片和状态栏设置主题。你打开它之前,它不会工作。为了添加主题,它会修改 SuperMemo 自己的文件。它会备份原文件,关闭该插件即可恢复。请从“浏览插件”安装。

## 文档

- [安装指南](docs/installation.zh-CN.md)
- [构建、测试与发布说明](docs/build.md)
- [发布与插件源](docs/plugin-feed.zh-CN.md)
- [给插件作者的说明](docs/plugin-authors.zh-CN.md)
- [更新日志](CHANGELOG.zh-CN.md)
- [项目来源与许可](docs/provenance.zh-CN.md)
- [参与贡献](CONTRIBUTING.zh-CN.md)

## 测试

SMA 在 `src/Tests` 中有单元测试、集成测试和端到端测试。端到端测试需要手动开启,它们在隐藏会话中针对 SuperMemo 的隔离副本运行。托管 CI 上没有 SuperMemo,因此会跳过它们。见 [docs/build.md](docs/build.md)。

自动化测试没有覆盖:

- 从一个已发布版本更新到下一个版本。更新源还没有发行版。
- 插件的交互使用,例如导入 PDF。测试只检查每个插件能够加载并连接。
- Themes 插件在 SuperMemo 中的外观。有人查看了截图,没有自动化测试来判断外观。

## 构建

```powershell
dotnet build SuperMemoAssistant.slnx
dotnet test --solution SuperMemoAssistant.slnx
```

应用构建到 `artifacts\app-dev`(已被 git 忽略)。构建它需要 Visual Studio 的 C++ 工作负载。构建把所有警告视为错误,完整构建为 0 个警告。详情见 [docs/build.md](docs/build.md)。

## 隐私

SMA 不发送任何遥测数据:没有崩溃报告、分析、使用数据或设备标识符。错误只写入你电脑上的日志文件,位于 `%UserProfile%\SuperMemoAssistant\Logs`。要报告问题,请在通知区域右键单击 SMA 图标,选择“Open logs folder”。检查日志中是否有隐私信息,然后把它附加到 [GitHub issue](https://github.com/sm18lr88/SMA/issues)。

SMA 只在以下情况连接互联网:在 GitHub 上检查更新、在“浏览插件”中列出插件,以及你使用的、需要联网的插件功能,例如词典查询或网页导入。

## 参与贡献

构建方法、测试强制的规则,以及一次修改必须包含的内容,见 [CONTRIBUTING.zh-CN.md](CONTRIBUTING.zh-CN.md)。

## 许可

SMA 及其插件使用原项目的 [MIT 许可](LICENSE)。第三方依赖保留各自的许可。

PDF 插件使用 PDFium(BSD-3-Clause),来自 `bblanchon.PDFium.Win32` 包(Apache-2.0)。PDF 查看器控件源自 Apache-2.0 代码,见 `src/Plugins/SuperMemoAssistant.Plugins.PDF/libs/SuperMemoAssistant.Pdfium.Wpf` 中的 `NOTICE` 文件。

发布文件不包含 SuperMemo 程序、集合、个人设置、日志或私人授权。

## 源码结构

- `src/Core`:应用及其核心。
- `src/Interop`:插件 API。
- `src/Services`:应用和插件共用的服务。
- `src/Plugins`:插件、PDF 引擎及其查看器。
- `src/Themes`:Themes 插件和 `sma-cards` 的引擎。
- `src/Tools`:插件源生成器,以及 `sma-cards`。后者是控制台工具,在 SuperMemo 关闭时编辑集合中的卡片。
- `skills/supermemo-appearance`:给 AI 助手使用的智能体技能,用于改变 SuperMemo 及其卡片的外观。把该文件夹链接或复制到助手的技能文件夹即可。
- `src/Tests`:单元、集成和端到端测试。
- `libs`:SMA 使用的库,例如插件管理器。
- `build`:发布和插件源脚本。
