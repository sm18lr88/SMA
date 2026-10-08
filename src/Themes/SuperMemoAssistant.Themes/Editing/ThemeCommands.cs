// The read-only theme commands of the card command line: browse the palette library and show what is inside sm20.exe now.
namespace SuperMemoAssistant.Themes.Editing;

internal static class ThemeCommands
{
  public static ThemeLibrary OpenLibrary(CardArguments a) => ThemeLibrary.Load(a.Value("--user-library") ?? SmaData.UserLibrary(SmaData.Folder()));

  public static void List(CardArguments a, TextWriter output)
  {
    var search  = a.Value("--search")?.ToLowerInvariant();
    var variant = a.Value("--variant");
    var source  = a.Value("--source")?.ToLowerInvariant();

    foreach (var e in OpenLibrary(a).Entries)
    {
      if ((search is not null && !$"{e.Name} {e.Id}".ToLowerInvariant().Contains(search))
          || (variant is not null && e.Variant != variant)
          || (source is not null && !e.Source.ToLowerInvariant().Contains(source)))
        continue;

      var name = e.Name.Length > 46 ? e.Name[..46] : e.Name;

      CardOutput.Line(output, $"{name,-46} {e.Variant,-5} bg {e.Roles["bg"]} fg {e.Roles["fg"]} accent {e.Roles["accent"]}  [{e.Source}]");
    }
  }

  public static void Show(CardArguments a, TextWriter output)
  {
    var e = OpenLibrary(a).Find(a.Positionals[0]);

    CardOutput.Line(output, $"{e.Name} ({e.Variant}, {e.Source}, id {e.Id})");

    foreach (var (role, color) in e.Roles)
      CardOutput.Line(output, $"  {role,-13} {color}");
  }

  public static void Status(CardArguments a, TextWriter output, SuperMemoInstall install)
  {
    var library = OpenLibrary(a);
    var status  = new ThemeInstaller(library).ReadStatus(install);

    CardOutput.Line(output, $"sm20.exe: {install.ExePath}");
    CardOutput.Line(output, $"dark mode: {(status.DarkMode ? "on" : "off")}");
    CardOutput.Line(output, $"active light style: {status.ActiveLight}");
    CardOutput.Line(output, $"active dark style: {status.ActiveDark}");
    CardOutput.Line(output, $"card area and status bar follow the window theme: {(status.ElementsThemed ? "yes" : "no")}");
    CardOutput.Line(output, $"live theme switching: {(status.LivePatched ? "on" : "off")} ({(status.LivePatchSupported ? "supported by this build" : "not supported by this build")})");
    CardOutput.Line(output, "styles:");

    foreach (var s in status.Styles.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
      CardOutput.Line(output, $"  {s.Name,-40} {(s.BuiltIn ? "built-in" : "added")}");

    CardOutput.Line(output, $"library themes that are in sm20.exe: {(status.InstalledThemeIds.Count == 0 ? "none" : string.Join(", ", status.InstalledThemeIds))}");
  }
}
