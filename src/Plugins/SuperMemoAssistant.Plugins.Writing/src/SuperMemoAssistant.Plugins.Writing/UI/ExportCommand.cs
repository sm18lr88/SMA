using System;
using System.Threading;
using System.Threading.Tasks;
using Forge.Forms;
using Microsoft.Win32;
using SuperMemoAssistant.Plugins.Writing.Export;
using SuperMemoAssistant.Plugins.Writing.SuperMemo;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.Writing.UI
{
  /// <summary>"Compile the current branch": options, save dialog, compilation with progress, and a summary.</summary>
  internal sealed class ExportCommand(WritingCfg config)
  {
    public const string Title = "Compile the current branch";

    public async Task RunAsync()
    {
      var root = Svc.SM.UI.ElementWdw.CurrentElement;

      if (root == null)
      {
        await Dialogs.AlertAsync("No element is shown in the element window. Go to the root of the branch first.", Title)
                     .ConfigureAwait(true);
        return;
      }

      var rootId    = root.Id;
      var rootTitle = root.Title ?? string.Empty;
      var choice    = await Show.Window().For(new ExportOptionsForm(config)).ConfigureAwait(true);

      if (choice.Action is not ExportOptionsForm.CompileAction)
        return;

      var options = choice.Model.ToOptions();

      if (AskOutputPath(rootTitle, options.Format) is not { } outputPath)
        return;

      using var cancellation = new CancellationTokenSource();
      var       window       = new ProgressWindow(Title, cancellation);
      var       progress     = new Progress<CompileProgress>(p => window.Report(Describe(p), p.Done, p.Total));

      window.Show();

      string summary;

      try
      {
        var (document, result) = await Task.Run(
          () => DocumentWriter.CompileToFile(new SuperMemoTreeSource(), rootId, options, outputPath, progress, cancellation.Token),
          cancellation.Token).ConfigureAwait(true);

        summary = Summarize(rootTitle, outputPath, document, result);
      }
      catch (OperationCanceledException)
      {
        summary = "The compilation was cancelled. No file was written.";
      }
      finally
      {
        window.Close();
      }

      await Dialogs.AlertAsync(summary, Title).ConfigureAwait(true);
    }

    private static string Summarize(string rootTitle, string outputPath, BranchDocument document, WriteResult result)
    {
      var missing = result.MissingImages.Count == 0
        ? string.Empty
        : $"\n{result.MissingImages.Count} image files did not exist, for example \"{result.MissingImages[0]}\".";

      return $"\"{rootTitle}\" was compiled to \"{outputPath}\": {document.Sections.Count} sections, "
        + $"{result.ImagesCopied} images copied.{missing}";
    }

    private static string? AskOutputPath(string rootTitle, OutputFormat format)
    {
      var (extension, filter) = format == OutputFormat.Html
        ? (".html", "HTML document (*.html)|*.html")
        : (".md", "Markdown document (*.md)|*.md");

      var dialog = new SaveFileDialog
      {
        Title           = Title,
        FileName        = Dialogs.SafeFileName(rootTitle) + extension,
        DefaultExt      = extension,
        Filter          = filter,
        AddExtension    = true,
        OverwritePrompt = true,
      };

      return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string Describe(CompileProgress progress)
    {
      return progress.Stage == CompileStage.ReadingTree
        ? $"Reading the branch structure: {progress.Done} elements read."
        : $"Reading the content: {progress.Done} of {progress.Total} elements.";
    }
  }
}
