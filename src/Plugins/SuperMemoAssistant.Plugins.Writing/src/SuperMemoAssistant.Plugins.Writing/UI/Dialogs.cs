using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Anotar.Serilog;
using Forge.Forms;

namespace SuperMemoAssistant.Plugins.Writing.UI
{
  /// <summary>Message boxes and the error boundary of the plugin commands.</summary>
  internal static class Dialogs
  {
    public static Task AlertAsync(string message, string title)
    {
      return Show.Window().For(new Alert(message, title));
    }

    /// <summary>Runs a command on the UI thread. An unexpected error is logged and shown to the user.</summary>
    public static void RunOnUiThread(Func<Task> command, string title)
    {
      Application.Current.Dispatcher.InvokeAsync(async () =>
      {
        try
        {
          await command().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
          LogTo.Warning(ex, "{Title} failed", title);
          await AlertAsync(ex.Message, title).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
          LogTo.Error(ex, "{Title} failed", title);
          await AlertAsync($"An unexpected error occurred: {ex.Message}\nThe SMA log has the details.", title).ConfigureAwait(true);
        }
      });
    }

    /// <summary>A file name made from an element title: invalid characters become "_".</summary>
    public static string SafeFileName(string title)
    {
      var invalid = Path.GetInvalidFileNameChars();
      var name    = new string(title.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());

      return name.Length > 0 ? name : "Branch";
    }
  }
}
