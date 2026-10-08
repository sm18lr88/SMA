// One color theme: an id, a display name, light or dark, where it came from, and its named color roles (hex).
namespace SuperMemoAssistant.Themes;

/// <param name="Id">Stable key, the slug of the name.</param>
/// <param name="Name">Display name, also the name SuperMemo shows in Window > Themes.</param>
/// <param name="Variant">"dark" or "light".</param>
/// <param name="Source">Origin of the palette, for example "tinted-theming base16".</param>
/// <param name="Roles">Role name (bg, fg, accent, ...) to "#RRGGBB".</param>
public sealed record ThemeEntry(string Id, string Name, string Variant, string Source, IReadOnlyDictionary<string, string> Roles);
