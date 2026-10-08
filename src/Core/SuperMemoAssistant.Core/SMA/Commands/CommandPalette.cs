namespace SuperMemoAssistant.SMA.Commands
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.InteropServices;
  using System.Threading;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Interop.SMA;
  using global::PluginManager.Remoting;
  using Plugins;
  using Services.IO.Keyboard;
  using SuperMemoAssistant.Extensions;
  using Sys.Windows;

  /// <summary>Opens the command palette and runs the command that the user chooses.</summary>
  public static partial class CommandPalette
  {
    /// <summary>The id of the hotkey that opens the palette. The palette does not list itself.</summary>
    public const string HotKeyId = "CommandPalette";

    private static readonly TimeSpan ForegroundWait = TimeSpan.FromMilliseconds(500);

    private static CommandPaletteWindow _window;

    /// <summary>Shows the palette, or activates it when it is already open. Call it on the UI thread.</summary>
    public static void ShowOrActivate()
    {
      if (_window != null)
      {
        _window.Activate();
        return;
      }

      var foreground = GetForegroundWindow();
      var registry   = Core.SMA.Commands;
      var model      = new CommandPaletteModel(Entries(registry), registry.LastUsed, ContextOf(foreground));

      _window = new CommandPaletteWindow(model);
      _window.Closed += (_, _) =>
      {
        var chosen = _window.Chosen;
        _window = null;

        if (chosen != null)
          Run(registry, chosen, foreground);
      };
      _window.ShowAndActivate();
    }

    private static IEnumerable<PaletteEntry> Entries(CommandRegistry registry) =>
      registry.Snapshot()
              .Where(e => e.Command.Key != PaletteCommand.SmaOwner + "/" + HotKeyId)
              .Concat(PluginSettingsEntries());

    /// <summary>Every running plugin with settings gets a command that opens them, as the tray menu does.</summary>
    private static IEnumerable<PaletteEntry> PluginSettingsEntries() =>
      SMAPluginManager.Instance.AllPlugins
                      .Where(p => p.HasSettings)
                      .Select(p => new PaletteEntry(
                                new PaletteCommand(PaletteCommand.SmaOwner, p.Metadata.DisplayName, "Settings:" + p.Package.Id,
                                                   $"Open the {p.Metadata.DisplayName} settings", null, HotKeyScopes.Global),
                                () => p.Plugin.ShowSettings()))
                      .ToList();

    private static PaletteContext ContextOf(IntPtr foreground)
    {
      var sm = Core.SM;
      if (sm == null || foreground == IntPtr.Zero || sm.ProcessId <= 0)
        return PaletteContext.OtherApplication;

      if (sm.UI.ElementWdw.IsAvailable && foreground == sm.UI.ElementWdw.Handle)
        return PaletteContext.ElementWindow;

      GetWindowThreadProcessId(foreground, out var processId);
      return processId == sm.ProcessId ? PaletteContext.SuperMemo : PaletteContext.OtherApplication;
    }

    private static void Run(CommandRegistry registry, PaletteEntry entry, IntPtr foreground)
    {
      var command = entry.Command;
      registry.MarkUsed(command.Key);

      // The palette had the focus, so SMA may give it back: commands act on the window that the user came from.
      if (foreground != IntPtr.Zero)
        SetForegroundWindow(foreground);

      Task.Run(() =>
      {
        WaitUntilForeground(foreground);

        try
        {
          entry.Execute();
        }
        catch (RemotingException ex)
        {
          registry.Unregister(command.Owner, command.Id);
          LogTo.Warning(ex, "Palette command {Key} is no longer available", command.Key);
          $"\"{command.Title}\" is no longer available, because {command.OwnerName} stopped.".ShowDesktopNotification();
        }
        catch (Exception ex)
        {
          // A command runs plugin code: SMA reports its failure and keeps running.
          LogTo.Error(ex, "Palette command {Key} failed", command.Key);
          $"\"{command.Title}\" failed: {ex.Message}".ShowDesktopNotification();
        }
      });
    }

    private static void WaitUntilForeground(IntPtr window)
    {
      var deadline = DateTime.UtcNow + ForegroundWait;
      while (window != IntPtr.Zero && GetForegroundWindow() != window && DateTime.UtcNow < deadline)
        Thread.Sleep(25);
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr window);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr window, out uint processId);
  }
}
