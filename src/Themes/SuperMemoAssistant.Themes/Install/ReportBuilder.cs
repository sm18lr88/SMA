// Collects what an installer run did and what it could not do, then builds the public report.
namespace SuperMemoAssistant.Themes;

internal sealed class ReportBuilder
{
  private readonly List<string> _actions  = [];
  private readonly List<string> _warnings = [];

  public void Did(string action) => _actions.Add(action);

  public void Warn(string warning) => _warnings.Add(warning);

  public ThemeReport Build(AppliedState state) => new(_actions, _warnings, state);
}
