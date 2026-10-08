# src/Services - reusable libraries for plugins

## OVERVIEW
Four small class libraries plugins reference to avoid re-implementing common features. All depend on Interop, libs/PluginManager.Interop, libs/Extensions.System.IO. Distinct domain from Interop: optional, plugin-side only.

## WHERE TO LOOK
| Project | Key types | Use for |
|---------|-----------|---------|
| Services.UI | CfgBase<TCfg>, CrudList (339 LOC), ConfigurationWindow, ElementPicker, HotKeyDataBinder | plugin settings windows (Forge.Forms), element picking, hotkey rebinding |
| Services.HTML | HtmlFilter/HtmlFilters/HtmlFilterBase, UrlPattern(+Type), *Ex | HTML cleanup filters and URL matching (HtmlAgilityPack) |
| Services.ToastNotifications | internal wrapper over Windows.UI.Notifications | toasts from plugins |
| Services.Medias.Images | PngChunkService (internal) | PNG chunk read/write; only non-WPF service |

## CONVENTIONS
- Layout: <Project>/Services/<Area>/... (HTML also has Models/, Extensions/, UI/) - namespace SuperMemoAssistant.Services.<Area>.
- Nullable OFF, ImplicitUsings OFF (explicit usings) - inherited defaults.
- Fody (Anotar.Serilog + PropertyChanged) with FodyWeavers.xml/.xsd per project; HTML and UI generate XML docs.
- Extension classes use the *Ex suffix; abstract bases use *Base.

## CONSUMERS
- Services.UI: nearly every plugin with settings (CfgBase<T>); LocalApi also uses ToastNotifications.
- PDF references ToastNotifications + UI - a good example of a multi-service plugin.
- Changing a public type here means rebuilding and re-publishing every consuming plugin to the feed.

## ADDING A SERVICE
- New project SuperMemoAssistant.Services.<Name>/ with Services/<Name>/ folder, FodyWeavers.xml, central package versions (no Version= in csproj).
- Add it to SuperMemoAssistant.slnx; reference from plugins via `$(RepoRoot)src\Services\...` paths.

## ANTI-PATTERNS
- Do not reference Core (SuperMemoAssistant.Core) from a service - services run inside PluginHost processes and see SMA only through Interop/Svc.
- Don't put plugin-specific logic here; a service needs at least two consumers.
