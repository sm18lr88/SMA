# 社区版本更新日志

[English](CHANGELOG.md) | **简体中文**

> 英文版 [CHANGELOG](CHANGELOG.md) 是主要版本,包含完整的变更列表。本文件给出 3.0.0 的摘要,并保留 SuperMemo 19.1 各版本的中文原文。

## 3.1.0 — 2026-10-07

- **隐私:不再有遥测。** SMA 及其插件不再向 Sentry 或任何其他服务发送数据。此前插件内置了原上游项目的报告密钥,会在未经询问的情况下报告错误。插件浏览器不再发送设备标识符。`SMA_SENTRY_DSN` 设置和 `SuperMemoAssistant.Services.Sentry` 库已移除;插件改为继承 `SMAPluginBase`。错误只写入本地日志文件。通知区域图标菜单中的“Open logs folder”会打开日志文件夹,你可以自行把日志附加到 issue 中。
- **签名的发布物。** `Setup.exe` 和程序文件使用本项目自签名的证书签名。由于 Windows 不信任自签名证书,SmartScreen 仍会警告;[安装说明](docs/installation.zh-CN.md)介绍了如何核对指纹。
- **修复:卡住的启动钩子会拖延后续钩子。** 每个启动钩子现在在自己的线程上运行,因此一个不返回的钩子不再妨碍后续钩子在各自的时限内启动。
- **修复:设置期间的 Windows 搜索错误。** SMA 查找 SuperMemo 时如果 Windows 索引查询失败,SMA 会记录警告并用其他方式继续查找,而不是记录未处理的错误。
- **命令面板:** 按 Ctrl+Alt+Shift+P,可以按名称查找并运行 SMA 或插件的命令(上游需求 #224)。输入几个字母即可,例如 “ibk” 能找到 “Import a book or Kindle highlights”。面板显示每个命令的快捷键,先列出最近用过的命令,并为每个插件提供打开其设置的命令;不能在你刚才所在窗口中运行的命令会被隐藏。
- **面板命令(插件 API):** 插件的全局快捷键无需修改即出现在面板中;`SMAPluginBase.RegisterPaletteCommand` 可添加没有快捷键的命令。见[给插件作者的说明](docs/plugin-authors.zh-CN.md)。
- **Local API 插件:** 新的内置插件只在本机(127.0.0.1 和 localhost,端口 47321)运行一个 HTTP 服务器。浏览器扩展、用户脚本和命令行工具可以读取状态和元素、创建主题和卡片,并显示某个元素。每个请求都需要插件设置中的访问令牌;服务器拒绝网页、其他主机名和其他计算机。它默认关闭。见[插件 README](src/Plugins/SuperMemoAssistant.Plugins.LocalApi/README.md)(上游需求 #263)。
- **PDF 插件:摘录标题。** 新的文字摘录以其文字开头作为标题,知识树因此能显示每个摘录的内容;图像摘录和 PDF 摘录使用其书签和页码。“Extract titles” 设置可恢复使用文章标题,另一个设置决定最大长度(默认 80 个字符)。摘录的引用信息不变(上游问题 #255)。
- **PDF 插件:一次摘录多个章节。** 书签菜单新增 “PDF Extract each subsection” 和 “PDF Extract this level”,为每个章节各创建一个 PDF 摘录并使用书签标题,例如一本书的每一章。已摘录的章节会被跳过,并报告结果(上游需求 #245)。
- **Themes 插件(可选):** 新的 Themes 插件用一个配色库为 SuperMemo 的窗口、卡片和状态栏设置主题。它不在安装程序中,请从“浏览插件”安装。它默认关闭,因为每次启动前它都会从未修改的原始文件重建 `sm20.exe`;关闭它会恢复原文件。它取代了此前完成同样工作的 Python 工具和启动器 smcards:SMA 在 SuperMemo 启动前应用设置,在其退出后重新给卡片着色,不再需要 Python。见 [ADR 0003](docs/decisions/0003-themes-optional-plugin-and-launch-hooks.md)。
- **主题导入(Themes 插件):** Themes 插件的设置窗口可以导入你自己的主题:base16 和 base24 配色方案、VS Code 主题和扩展,以及 Obsidian 主题文件夹。导入的内容保存在你自己的库文件中,导入本身不会修改 `sm20.exe`。
- **`sma-cards` 工具:** 新的控制台工具(`src/Tools/SuperMemoAssistant.CardTool`,不在安装程序中)在 SuperMemo 关闭时编辑集合中的卡片:元素颜色、卡片样式表,以及卡片 HTML 中的文字和内联样式。修改文件的命令在加上 `--apply` 之前只是试运行,并且每个命令都会先备份文件。`sma-cards theme list`、`theme show` 和 `theme status` 用于浏览配色库,以及查看 `sm20.exe` 当前的内容。该工具自己查找 SuperMemo,不假定任何位置:先看 `--sm-root`,再看环境变量 `SMCARDS_ROOT`,再看 SMA 配置的 `sm20.exe`,最后看为 `.kno` 文件注册的程序。
- **`supermemo-appearance` 智能体技能:** 让 AI 智能体驱动 `sma-cards` 和 Themes 插件的技能现在位于仓库中(`skills/supermemo-appearance`)。它不含你机器上的任何位置,并有测试检查这一点。
- **启动钩子(插件 API):** `ISuperMemoAssistant.RegisterLaunchHook` 让插件在 SuperMemo 关闭时执行工作:一次在读取 `sm20.exe` 之前,一次在 SuperMemo 退出之后、插件停止之前。失败或超时的钩子会被报告,但不会阻止 SuperMemo。这是提升次版本号的唯一原因:为 3.0.0 构建的插件仍可加载,但调用该钩子的插件需要 SMA 3.1.0 或更高版本。
- **修复:插件连接关闭时崩溃。** 如果插件连接的管道在 SMA 或 `PluginHost` 仍在发送数据时被释放(例如关闭过程中释放远程对象),进程可能因未处理的 `ObjectDisposedException` 而停止。现在连接把这种情况与其他管道关闭同样处理。

## 3.0.0 — 2026-10-07

- **SuperMemo 20(x64)与 .NET 10:** SMA 现在支持 SuperMemo 20 和 .NET 10。启动时,它会检查自己能否配合你的 SuperMemo 版本,并有测试覆盖已验证的版本。不再支持 SuperMemo 17-19.1(x86)。
- **插件 RPC:** 插件运行在独立进程中,通过命名管道 RPC 与 SMA 通信。插件必须针对 `net10.0-windows` 和 Interop 3.0 或更高版本重新编译。
- **PDF:** PDF 插件改用 PDFium 引擎,没有评估限制,并随安装程序提供。查看器支持在表单字段中输入文字,以及 NextPage、PrevPage、FirstPage、LastPage 命名操作。
- **插件与更新:** 安装程序包含七个插件。“浏览插件”从本项目在 GitHub Pages 上的插件源安装和更新插件。SMA 从本项目的 GitHub Releases 更新自身。
- **修复:** 插件加载时崩溃、插件缺少依赖、x64 指针被截断、`OnSMStarted` 和 `OnSMStopped` 调用错误的处理程序、`AtFlags` 取值错误、插件包被锁定等问题。
- **隐私:** 应用只有设置 `SMA_SENTRY_DSN` 时才发送崩溃报告,但插件仍会自行报告。3.1.0 已全部移除。

## 19.1-community-r11.2 — 2026-10-06

- HTML 源文件改为集合 `sma/ImportedHtml` 中的唯一持久文件，解决依赖 `Windows TEMP/sm_element_0.htm` 引起的文件缺失与 PDF 导入失败。
- 在调用 SuperMemo 原生接口前验证文件存在，并记录调用前后的路径和文件状态。
- 保留已验证的 Unicode 标题、无 BOM HTML 和隐藏 PDF 元数据；本次没有更换 PDF SDK。
- 用户确认中文和日文文件名的两个 PDF 均重新导入成功。
- 完整包包含这次 Core 修复和更新后的程序内日志；独立目录的全新安装、升级、恢复与备份检查通过。

## r11.1

- 完整安装器支持保留设置、集合列表和 SDK 的覆盖升级及程序恢复。
- 增加暂存校验、旧程序与 PDF DLL 备份、失败回滚；仅在校验值不同时更新 PDF DLL。
- 直接校验 SuperMemo EXE 的 SHA-256，移除依赖 Windows TEMP 临时源文件的 C# 编译步骤。

## r9–r11

- 通过 Unicode 接口写入中文标题，修复标题中的问号。
- 使用无 BOM HTML、实体转义及可兼容的识别正则，正文只显示一次文件名，隐藏 Base64 元数据。
- 增加通知注册异常处理、完整包安装入口和校验清单。

## 早期 19.1 适配

- 已核对与 SuperMemo 19.1 的兼容性。
- 修复 PDF 元素创建相关适配；增加失败提示、日志和导入锁释放。

历史详细日志保留在根目录 `ChangeLogs` 和 PDF 插件的 `ChangeLogs`。此前的诊断包不是推荐安装版本。
