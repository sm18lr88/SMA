# 安装

[English](installation.md) | **简体中文**

> 本文档是英文 [installation.md](installation.md) 的中文译本。英文版是主要版本;如两者不一致,以英文版为准。

本指南说明如何为 SuperMemo 20 安装 SuperMemoAssistant(SMA)。

## 安装条件

- Windows x64。
- SuperMemo 20,64 位。
- .NET 10 桌面运行时(x64)。缺少运行时时,`Setup.exe` 会自动安装。
- 磁盘空间:SMA 及其插件约占 85 MB;如需安装 .NET 运行时,还需要更多空间。

已验证的 SuperMemo 20 构建标识为 `x64-6A2496CE-00C63200`。对其他 SuperMemo 20 版本,SMA 在启动时检查能否支持;如果不能,SMA 不会连接 SuperMemo,并说明原因。

## 安装步骤

1. 保存工作,关闭 SuperMemo 和 SMA。
2. 从本仓库的 GitHub Releases 下载 `SuperMemoAssistant-win-Setup.exe`。
3. 运行 `Setup.exe`,不需要管理员权限。
4. 首次启动时,阅读 SMA 许可并选择自己的集合。

发布物使用本项目自签名的证书签名。Windows 不信任自签名证书,因此 SmartScreen 仍可能显示警告。要继续,请依次选择“更多信息”和“仍要运行”。

要确认 `Setup.exe` 来自本项目,请在 PowerShell 中运行 `(Get-AuthenticodeSignature .\SuperMemoAssistant-win-Setup.exe).SignerCertificate.Thumbprint`,或打开文件的“属性 > 数字签名 > 详细信息 > 查看证书 > 详细信息”。指纹必须是 `EBF4E4E3588D9EDFDEF5684F8F5D35879B8D7C6E`。公开证书见 [`build/signing/sma-codesign.cer`](../build/signing/sma-codesign.cer)。

无人值守安装请运行 `Setup.exe --silent`。

## 目录

```text
程序:                %LocalAppData%\SuperMemoAssistant\current
设置、插件、日志:      %UserProfile%\SuperMemoAssistant
```

SMA 只能从上面的程序目录或 `app-dev` 构建目录运行,便携 zip 无法运行。

如需移动设置目录,可使用以下任一方法:

- 把环境变量 `SMA_APPDATA_DIR` 设为某个目录,SMA 随后使用 `<该目录>\SuperMemoAssistant`。此方法优先。
- 创建 `%UserProfile%\supermemoassistant.json`,在其中设置 `AppDataDirPath`。如果 SMA 无法使用该目录,会改用默认目录,并把错误写入 `%TEMP%\SuperMemoAssistant.log`。

## 插件

安装程序包含十一个插件:Books、Dictionary、Email、Formulation、Image Occlusion、Import、LaTeX、Local API、OmniMemo、PDF 和 Writing,随 SMA 一起更新。

**Themes** 是可选插件,不在安装程序中。它为 SuperMemo 的窗口、卡片和状态栏设置主题。它会修改 `sm20.exe`,因此在你于其设置中打开它之前,它一直处于关闭状态。请通过“浏览插件”安装,然后重启 SMA。

SMA 设置中的“浏览插件”列出插件源中的插件,可在其中安装或更新插件。安装与内置插件同名的插件后,请重启 SMA;SMA 随后启动版本较高的副本。

插件必须面向 .NET 10 和 SMA 3.0 或更高版本。SMA 不会启动较旧的插件,并在日志中写入 `Outdated interop version`。

## 更新

SMA 从本仓库的 GitHub Releases 获取更新。“Stable”通道只接受稳定版本,“Beta”和“Nightly”通道也接受预发布版本。可在 SMA 设置中更改通道。

## 构建安装程序

从源码构建安装程序:

```powershell
dotnet build SuperMemoAssistant.slnx
pwsh build\pack.ps1
```

安装程序和发布包写入 `artifacts\releases`。构建要求见 [docs/build.md](build.md)。

## 卸载

1. 保存工作,关闭 SuperMemo 和 SMA。
2. 在 Windows 设置中打开“应用”,卸载“SuperMemo Assistant”。

卸载程序删除程序目录,但不删除设置目录。SMA 添加到集合中的元素仍保留在集合中。

SMA 不会修改 SuperMemo 程序,只有一个例外:如果你安装了可选的 Themes 插件并打开它,SMA 会在每次启动前从未修改的副本重建 `sm20.exe`。卸载 SMA 或该插件之前,请先关闭 Themes,这样 SMA 会恢复原始的 `sm20.exe`。见 [Themes 插件](../src/Plugins/SuperMemoAssistant.Plugins.Themes/README.md)。

## SuperMemo 19.1

当前版本不支持 SuperMemo 19.1。以前的 19.1 发行版 r11.2 不能安装或更新当前版本。
