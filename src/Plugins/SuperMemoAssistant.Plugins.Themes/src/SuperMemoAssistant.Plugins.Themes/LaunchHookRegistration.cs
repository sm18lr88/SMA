// Registers the launch hook with SMA and survives an SMA version that does not have launch hooks yet.
namespace SuperMemoAssistant.Plugins.Themes
{
  using System;
  using Anotar.Serilog;
  using SuperMemoAssistant.Interop.SMA;

  internal static class LaunchHookRegistration
  {
    public const string HookName = "Themes";

    public const string UnsupportedMessage =
      "This SuperMemo Assistant is too old to apply themes before SuperMemo starts. Update SMA, then restart it.";

    /// <summary>Returns true when SMA accepted the hook. Otherwise <paramref name="warn" /> gets a message the user can act on.</summary>
    public static bool TryRegister(ISuperMemoAssistant sma, ThemesLaunchHook hook, Action<string> warn)
    {
      try
      {
        sma.RegisterLaunchHook(HookName, hook.BeforeLaunch, hook.AfterExit);

        return true;
      }
      catch (Exception ex)
      {
        LogTo.Warning(ex, "SMA refused the launch hook; it probably predates launch hooks");
        warn(UnsupportedMessage);

        return false;
      }
    }
  }
}
