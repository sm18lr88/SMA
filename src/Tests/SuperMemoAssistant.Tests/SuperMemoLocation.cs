// Finds the SuperMemo folder and the sm20.exe files that the opt-in tests need. Nothing is assumed about where SuperMemo is.
namespace SuperMemoAssistant.Tests;

using SuperMemoAssistant.Themes;
using SuperMemoAssistant.Themes.Editing;

/// <summary>
///   The folder comes from SMA_SM_ROOT, else from the same lookup as sma-cards (SMCARDS_ROOT, the sm20.exe that SMA is set
///   up with, the .kno association). SMA_SM20_EXE and SMA_SM20_ORIGINAL name the exes directly. The untouched exe is the
///   backup that the Themes engine keeps in the SuperMemo folder.
/// </summary>
internal static class SuperMemoLocation
{
  public static string? Root { get; } = FindRoot();

  public static string? Exe { get; } = Existing(Environment.GetEnvironmentVariable("SMA_SM20_EXE"), InRoot("sm20.exe"));

  public static string? OriginalExe { get; } =
    Existing(Environment.GetEnvironmentVariable("SMA_SM20_ORIGINAL"), InRoot(Path.Combine("smcards-backups", "exe", "sm20.exe.original")));

  private static string? InRoot(string relative) => Root is null ? null : Path.Combine(Root, relative);

  private static string? Existing(params string?[] candidates) => candidates.FirstOrDefault(File.Exists);

  private static string? FindRoot()
  {
    if (Environment.GetEnvironmentVariable("SMA_SM_ROOT") is { Length: > 0 } configured)
      return configured;

    try
    {
      return SuperMemoLocator.FindRoot(null);
    }
    catch (ThemeException)
    {
      return null;
    }
  }
}
