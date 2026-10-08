# 给插件作者的说明

[返回 README](../README.zh-CN.md)

## 与 SMA 2.x 相比的破坏性变更

为 SMA 2.x 编写的插件需要以下修改:

- 针对 `net10.0-windows`(x64)并基于本仓库的 Interop 程序集重新编译。SMA 会拒绝目标 Interop 版本低于 3.0.0 的插件,并记录 `Outdated interop version`。
- 如需修改 WPF 应用程序(例如合并资源字典),请重写 `ConfigureApplication`。PluginHost 拥有进程中唯一的 `Application`。
- 跨进程的对象必须通过公开接口暴露,SMA 只为接口生成代理。
- `RemoteTask`、`RemoteCancellationToken` 和委托仍可使用。2.x 中使用 `ActionProxy` 的地方,请直接传普通委托。
- `RemotingException` 位于 `PluginManager.Remoting`。按值传递的数据必须标记 `[Serializable]`。
- 部分名称已更改:`RemotingServicesEx` 改为 `RpcServices`(`Connect`、`Serve`、`NewPipeName`);`IPluginBase.ChannelName` 改为 `PipeName`;`*17` 类型改为 `*20`,例如 `SM20`。完整列表见 [CHANGELOG](../CHANGELOG.zh-CN.md)。
- `AtFlags` 使用 SuperMemo 20 的取值。如果插件保存或比较这些值,请检查。
- `OnSMStarted` 和 `OnSMStopped` 调用各自的处理程序;在 2.x 中,它们调用的是 `OnSMStarting` 的处理程序。

## 命令面板中的命令

命令面板(Ctrl+Alt+Shift+P)让用户按名称查找并运行命令。

- 插件用 `RegisterGlobal` 注册的每个全局快捷键都会出现在面板中。标题是快捷键的描述,面板还显示插件名称和当前快捷键,不需要做其他事情。
- 不需要快捷键的功能,请在 `OnPluginInitialized` 或之后调用 `RegisterPaletteCommand(id, title, execute)`。标题请写成动作,例如“复制 API 令牌”。
- 作用域决定命令能在哪里运行:从其他程序打开面板时,面板隐藏 `SM` 命令;在元素窗口之外,面板隐藏 `SMBrowser` 命令。
- 面板关闭、焦点回到用户原来的窗口之后,SMA 才运行命令;调用发生在后台线程上,与快捷键相同。
- 插件停止时,SMA 删除它的命令。SMA 3.0 没有命令面板:在那里,这些调用只会记录日志,不起作用。

## 插件如何运行

- **插件 RPC**(`PluginManager.Remoting`):插件运行在独立的 `PluginHost` 进程中。基于命名管道的 RPC 让插件跨进程使用 `MarshalByRefObject` 对象、事件、委托和同步属性。
- **插件加载:** 每个插件目录都有自己的 `deps.json`,PluginHost 据此加载插件的托管和原生依赖。SMA 从三处查找插件:`SuperMemoAssistant.exe` 旁的内置插件、开发插件,以及插件源中的插件。
