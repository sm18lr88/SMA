// An error the user can act on: a closed-SuperMemo requirement, an unsupported build, a missing backup.
namespace SuperMemoAssistant.Themes;

public sealed class ThemeException : Exception
{
  public ThemeException(string message) : base(message) { }

  public ThemeException(string message, Exception inner) : base(message, inner) { }
}
