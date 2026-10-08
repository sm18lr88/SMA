namespace SuperMemoAssistant.SMA
{
  using System;
  using Anotar.Serilog;
  using Commands;
  using Interop.SMA;

  public partial class SMA
  {
    /// <summary>The commands of the command palette.</summary>
    public CommandRegistry Commands { get; } = new();

    /// <inheritdoc />
    public void RegisterCommand(PaletteCommand command, Action execute)
    {
      Commands.Register(command, execute);
      LogTo.Debug("Palette command {Key} registered ({Title})", command.Key, command.Title);
    }

    /// <inheritdoc />
    public void UnregisterCommand(string owner, string id) => Commands.Unregister(owner, id);
  }
}
