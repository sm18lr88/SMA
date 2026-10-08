using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Forge.Forms;
using Microsoft.Win32;
using SuperMemoAssistant.Plugins.Writing.Import;
using SuperMemoAssistant.Plugins.Writing.SuperMemo;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.Writing.UI
{
  /// <summary>"Import a Markdown outline": file, priority, topic creation with progress, and a report.</summary>
  internal sealed class ImportCommand(WritingCfg config)
  {
    public const string Title = "Import a Markdown outline";

    public async Task RunAsync()
    {
      var parent = Svc.SM.UI.ElementWdw.CurrentElement;

      if (parent == null)
      {
        await Dialogs.AlertAsync("No element is shown in the element window. Go to the parent of the new topics first.", Title)
                     .ConfigureAwait(true);
        return;
      }

      var parentId    = parent.Id;
      var parentTitle = parent.Title ?? string.Empty;

      if (AskMarkdownFile() is not { } path)
        return;

      var outline = OutlineParser.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(true));

      if (await AskPriorityAsync() is not { } priority)
        return;

      var plan = ImportPlan.Create(outline, path, priority);

      if (plan.TopicCount == 0)
      {
        await Dialogs.AlertAsync($"\"{plan.SourceName}\" has no headings and no text. No topic was created.", Title).ConfigureAwait(true);
        return;
      }

      using var cancellation = new CancellationTokenSource();
      var       window       = new ProgressWindow(Title, cancellation);
      var       progress     = new Progress<int>(n => window.Report($"Creating topics: {n} of {plan.TopicCount}.", n, plan.TopicCount));

      window.Show();

      ImportResult result;

      try
      {
        result = await Task.Run(
          () => new OutlineImporter(new SuperMemoElementCreator(plan.DocumentTitle, plan.SourceName))
            .Import(parentId, parentTitle, plan.Topics, progress, cancellation.Token),
          CancellationToken.None).ConfigureAwait(true);
      }
      finally
      {
        window.Close();
      }

      await Dialogs.AlertAsync(Report(result, plan, parentTitle), Title).ConfigureAwait(true);
    }

    private static string? AskMarkdownFile()
    {
      var dialog = new OpenFileDialog
      {
        Title           = Title,
        Filter          = "Markdown (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*",
        CheckFileExists = true,
      };

      return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private async Task<double?> AskPriorityAsync()
    {
      var answer = await Show.Window()
                             .For(new Prompt<double>
                             {
                               Title   = Title,
                               Message = "Priority of the first topic, from 0 (highest) to 100 (lowest). The next topics get ascending values.",
                               Value   = config.DefaultImportPriority,
                             })
                             .ConfigureAwait(true);

      if (answer.Model.Confirmed == false)
        return null;

      if (answer.Model.Value is >= 0 and <= 100)
        return answer.Model.Value;

      await Dialogs.AlertAsync("The priority must be a value between 0 and 100. No topic was created.", Title).ConfigureAwait(true);

      return null;
    }

    private static string Report(ImportResult result, ImportPlan plan, string parentTitle)
    {
      var parts = result.PartsCreated == 0
        ? string.Empty
        : $" {result.PartsCreated} part topics were added because SuperMemo limits the number of children per element.";

      return result.Cancelled
        ? $"The import was cancelled. {result.TopicsCreated} of {plan.TopicCount} topics were created under \"{parentTitle}\".{parts}"
        : $"{result.TopicsCreated} topics were created under \"{parentTitle}\" from \"{plan.SourceName}\".{parts}";
    }
  }
}
