# 发布与插件源

[返回 README](../README.zh-CN.md)

## 发布

CI(`.github/workflows/build.yml`)对每次推送和拉取请求进行构建和测试。托管运行器上没有 SuperMemo,因此端到端测试会跳过。

发布步骤:

1. 在本地以 `SMA_E2E=1` 运行端到端测试。
2. 用 `git tag vX.Y.Z` 创建标签,再用 `git push origin main --follow-tags` 推送。
3. CI 把安装程序发布为 GitHub Release,并把插件源部署到 GitHub Pages。

已安装的 SMA 从 GitHub Releases 获取更新;Beta 和 Nightly 通道也接受预发布版本。

发布作业用本项目自签名证书(`build/signing/sma-codesign.cer`,由 Sectigo 加盖时间戳)为 `Setup.exe` 和包中尚未签名的所有二进制文件签名。证书来自仓库机密 `SMA_SIGN_PFX_BASE64` 和 `SMA_SIGN_PFX_PASSWORD`;没有它们时(例如在复刻仓库中)发布物未签名。对自签名的 `Setup.exe`,Windows SmartScreen 仍会警告。插件源中的包未签名。

## 插件源

SMA 的“浏览插件”从本仓库 GitHub Pages 站点上的静态插件源列出、安装和更新插件。插件源分两部分:

- 目录 `https://sm18lr88.github.io/SMA/plugins.json` 列出 SMA 显示的插件。
- NuGet v3 源 `https://sm18lr88.github.io/SMA/nuget/index.json` 提供插件包。

推送 `vX.Y.Z` 标签后,CI 运行 `build\pack-plugins.ps1` 并部署 `artifacts\feed`。脚本打包 `build\plugin-feed\catalog.json` 中的每个插件;每个包在 `lib\net10.0-windows10.0.19041\` 下包含发布后的插件目录。包中保留 `SuperMemoAssistant.Interop.dll`,因为 SMA 读取其版本来拒绝过时的插件;与应用自带副本完全相同的其他程序集会被删除。

包版本是插件程序集的产品版本,即 `Directory.Build.props` 中的 `Version`,除非插件项目自行设置了版本。要发布插件更新,请在推送标签前提高该版本。每次部署都会替换整个站点,因此插件源只保存每个插件的当前版本。

插件源中的插件和内置插件可能同名。此时 SMA 启动版本较高的副本;版本相同时,启动插件源中的副本。安装此类插件后,请重启 SMA。

发布第三方插件有两种方法:

1. 提交拉取请求,把插件加入 `build\plugin-feed\catalog.json`。把 `PackageUrl` 设为预先构建的 `.nupkg` 文件,把 `Sha256` 设为其哈希值。包必须采用上述布局,且没有 NuGet 依赖。
2. 自建插件源。运行 `build\pack-plugins.ps1 -BaseUrl <你的网址> -Catalog <你的目录>` 并发布 `artifacts\feed`。用户随后把 `Configs\Core\CoreCfg.json` 中的 `Updates.PluginsUpdateUrl` 和 `Updates.PluginsUpdateNuGetUrls` 设为你的网址。SMA 只读取一个目录,因此你的目录还必须列出用户需要的其他插件。

要关闭在线目录,请把 `Updates.EnablePluginsUpdates` 设为 `false`。

在推送第一个标签之前,仓库所有者需要完成以下一次性设置:

1. 在 **Settings > Pages** 中,把 **Source** 设为 **GitHub Actions**。
2. 在 **Settings > Environments > github-pages** 中,在 **Deployment branches and tags** 下添加标签规则 `v*`。
