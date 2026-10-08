// Works out the sm20.exe state the settings ask for: which window styles, whether elements are themed, whether the live patch is on.
using SuperMemoAssistant.Themes.Exe;
using SuperMemoAssistant.Themes.Styles;

namespace SuperMemoAssistant.Themes;

internal sealed record ExeTarget(ExeState State, IReadOnlyList<string> Managed, IReadOnlyDictionary<string, string> StyleNames);

internal static class ExePlan
{
  public static ExeTarget Build(
    ThemeSettings settings,
    ThemeLibrary library,
    AppliedState previous,
    IReadOnlyDictionary<string, byte[]> allStyles,
    ExeState current,
    CardSync.Active active,
    bool patchSupported,
    ReportBuilder report)
  {
    var builtin = allStyles.Where(kv => !kv.Key.StartsWith(ExeBuilder.CustomPrefix, StringComparison.Ordinal))
                           .Select(kv => VsfFile.StyleName(kv.Value))
                           .ToHashSet();
    var target  = current.Clone();
    var managed = new List<string>();
    var names   = new Dictionary<string, string>();

    foreach (var id in settings.InstalledThemeIds.Concat(settings.ActiveThemeId is { } a ? [a] : []).Distinct())
    {
      if (library.TryGet(id) is not { } entry)
      {
        report.Warn($"Theme '{id}' is not in the library and was skipped.");

        continue;
      }

      var resource = StyleNames.ResourceName(entry);
      var name     = StyleNames.WindowStyleName(entry, builtin);
      var blob     = StyleNames.BuildStyle(entry, allStyles, name);

      if (!target.Styles.TryGetValue(resource, out var old) || !VsfFile.SameContent(old, blob))
        target.Styles[resource] = blob;

      managed.Add(resource);
      names[id] = name;
    }

    var inUse = StylesInUseAfterThisRun(settings, library, previous, active, names);

    foreach (var resource in previous.ManagedStyleResources.Where(r => !managed.Contains(r) && target.Styles.ContainsKey(r)))
    {
      var name = VsfFile.StyleName(target.Styles[resource]);

      if (inUse.Contains(name))
      {
        report.Warn($"Style '{name}' is active in this collection, so it was kept. Pick another theme in Window > Themes first.");
        managed.Add(resource);
      }
      else
      {
        target.Styles.Remove(resource);
        report.Did($"Removed the window style '{name}'.");
      }
    }

    target.Elements = settings.ThemeElements;
    target.Patched  = settings.LiveSwitching && patchSupported;

    if (settings.LiveSwitching && !patchSupported)
      report.Warn("Live theme switching was skipped: this SuperMemo build differs from the one the patch was written for. Window styles still work.");

    return new ExeTarget(target, managed, names);
  }

  /// <summary>
  ///   The style names the collection will use once this run is done: today's two slots, except that a changed active
  ///   theme takes over its own slot in this same run.
  /// </summary>
  private static HashSet<string> StylesInUseAfterThisRun(ThemeSettings settings, ThemeLibrary library, AppliedState previous, CardSync.Active active, IReadOnlyDictionary<string, string> names)
  {
    string light = active.Light, dark = active.Dark;

    if (settings.ActiveThemeId is { } id && id != previous.ActiveThemeId && names.TryGetValue(id, out var newName) && library.TryGet(id) is { } entry)
    {
      if (entry.Variant == "dark")
        dark = newName;
      else
        light = newName;
    }

    return new HashSet<string>([light, dark], StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>True when the two states differ. Styles are compared by their inflated data, because compressors differ.</summary>
  public static bool Differs(ExeState a, ExeState b) =>
    a.Elements != b.Elements || a.Patched != b.Patched || a.Styles.Count != b.Styles.Count
    || a.Styles.Any(kv => !b.Styles.TryGetValue(kv.Key, out var other) || !VsfFile.SameContent(kv.Value, other));

  public static string Describe(ExeState from, ExeState to)
  {
    var parts = new List<string>();
    var added = to.Styles.Count(kv => !from.Styles.TryGetValue(kv.Key, out var o) || !VsfFile.SameContent(o, kv.Value));
    var gone  = from.Styles.Keys.Count(k => !to.Styles.ContainsKey(k));

    if (added + gone > 0)
      parts.Add($"{added} window style(s) added or updated, {gone} removed");

    if (from.Elements != to.Elements)
      parts.Add($"card area and status bar {(to.Elements ? "follow" : "ignore")} the theme");

    if (from.Patched != to.Patched)
      parts.Add($"live theme switching {(to.Patched ? "on" : "off")}");

    return string.Join("; ", parts);
  }
}
