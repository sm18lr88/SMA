// Removes shortcuts that an installer test creates in the real Start Menu and Desktop folders; pre-existing files stay.
namespace SuperMemoAssistant.Tests.E2E;

internal sealed class ShortcutGuard : IDisposable
{
  private static readonly string[] Folders =
  [
    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
  ];

  private readonly HashSet<string> _before = Snapshot();

  public List<string> Removed { get; } = [];

  public void Dispose()
  {
    foreach (var path in Snapshot().Except(_before, StringComparer.OrdinalIgnoreCase))
    {
      File.Delete(path);
      Removed.Add(path);
    }
  }

  private static HashSet<string> Snapshot() =>
    new(Folders.Where(Directory.Exists)
               .SelectMany(f => Directory.EnumerateFiles(f, "*.lnk", SearchOption.AllDirectories)),
        StringComparer.OrdinalIgnoreCase);
}
