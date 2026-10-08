namespace SuperMemoAssistant.Plugins.Books.UI
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows.Input;
  using Anotar.Serilog;
  using Epub;
  using Import;
  using Microsoft.Win32;
  using Planning;
  using SuperMemoAssistant.Services;
  using SuperMemoAssistant.Sys.Windows.Input;

  /// <summary>The state and commands of the import dialog. Long work runs on background threads and can be cancelled.</summary>
  public sealed class BookImportViewModel : INotifyPropertyChanged
  {
    private const string FileFilter = "Books and Kindle clippings (*.epub;*.txt)|*.epub;*.txt|EPUB books (*.epub)|*.epub|Kindle clippings (*.txt)|*.txt";

    private readonly BooksCfg                 _config;
    private          CancellationTokenSource? _cts;
    private          ImportSource?            _source;
    private          BooksCollectionCfg       _collectionCfg = new();

    /// <summary>Creates the view model.</summary>
    public BookImportViewModel(BooksCfg config)
    {
      _config  = config;
      Priority = config.DefaultPriority;

      BrowseCommand     = new AsyncRelayCommand(BrowseAsync, () => IsBusy == false);
      ImportCommand     = new AsyncRelayCommand(ImportAsync, () => IsBusy == false && _source != null);
      CancelCommand     = new RelayCommand(Cancel, () => IsBusy);
      SelectAllCommand  = new RelayCommand(() => SetSelection(true), () => IsBusy == false);
      SelectNoneCommand = new RelayCommand(() => SetSelection(false), () => IsBusy == false);
    }

    /// <summary>The chosen file.</summary>
    public string? FilePath { get; private set; }

    /// <summary>A description of the loaded file.</summary>
    public string? Summary { get; private set; }

    /// <summary>The last status or error message.</summary>
    public string? Status { get; private set; } = "Choose an EPUB book or a Kindle \"My Clippings.txt\" file.";

    /// <summary>Whether <see cref="Status" /> is an error.</summary>
    public bool IsError { get; private set; }

    /// <summary>Whether a file is loading or an import runs.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>The number of created elements.</summary>
    public int Progress { get; private set; }

    /// <summary>The number of planned elements.</summary>
    public int ProgressMaximum { get; private set; } = 1;

    /// <summary>The priority of the book topic, 0..100.</summary>
    public double Priority { get; set; }

    /// <summary>Whether to import under the displayed element; otherwise under the collection root.</summary>
    public bool UseCurrentElement { get; set; } = true;

    /// <summary>The inverse of <see cref="UseCurrentElement" />, for the second option button.</summary>
    public bool UseCollectionRoot
    {
      get => UseCurrentElement == false;
      set => UseCurrentElement = value == false;
    }

    /// <summary>The preview rows.</summary>
    public IReadOnlyList<PreviewItem> Items { get; private set; } = [];

    public ICommand BrowseCommand     { get; }
    public ICommand ImportCommand     { get; }
    public ICommand CancelCommand     { get; }
    public ICommand SelectAllCommand  { get; }
    public ICommand SelectNoneCommand { get; }

    /// <summary>Requests cancellation of the running work.</summary>
    public void Cancel() => _cts?.Cancel();

    private async Task BrowseAsync()
    {
      var dialog = new OpenFileDialog { Filter = FileFilter, Title = "Import a book or Kindle highlights" };
      if (dialog.ShowDialog() != true)
        return;

      var path      = dialog.FileName;
      var threshold = _config.MergeThreshold;

      await RunBusyAsync(async ct =>
      {
        _source        = null;
        Items          = [];
        Summary        = null;
        FilePath       = path;
        _collectionCfg = Svc.CollectionConfiguration.Load<BooksCollectionCfg>() ?? new BooksCollectionCfg();

        var hashes = _collectionCfg.ImportedHighlightHashes;
        var source = await Task.Run(() => ImportSourceLoader.Load(path, threshold, hashes, ct), ct);

        _source = source;
        Items   = source.Items;
        Summary = source.Summary;
        SetStatus("Select the rows to import, set the priority and the parent, then click Import.", false);
      }, "Loading was cancelled.");
    }

    private async Task ImportAsync()
    {
      if (_source == null)
        return;

      var source = _source;

      await RunBusyAsync(async ct =>
      {
        var limit = Math.Max(2, (int)Svc.SM.UI.ElementWdw.LimitChildrenCount);
        var roots = source.Plan(new PlanOptions(Priority, _config.ChapterPriorityStep, limit));

        if (roots.Count == 0)
        {
          SetStatus("Select at least one row to import.", true);
          return;
        }

        var parentId = UseCurrentElement ? Svc.SM.UI.ElementWdw.CurrentElementId : Svc.SM.Registry.Element.Root.Id;
        if (parentId <= 0)
        {
          SetStatus("No element is displayed in SuperMemo. Display an element, or choose the collection root.", true);
          return;
        }

        Progress        = 0;
        ProgressMaximum = roots.Sum(r => r.Count);

        var progress = new Progress<int>(n => Progress = n);
        var outcome  = await Task.Run(() => ElementTreeImporter.Import(roots, parentId, progress, ct), CancellationToken.None);

        if (outcome.Created > 0)
          _source = null;

        var saved = true;
        if (outcome.CreatedHashes.Count > 0)
        {
          _collectionCfg.ImportedHighlightHashes.UnionWith(outcome.CreatedHashes);
          saved = await Svc.CollectionConfiguration.SaveAsync(_collectionCfg);
        }

        if (outcome.Error != null || saved == false)
          LogTo.Warning("Books: {Summary} Hashes saved: {Saved}", outcome.Summary, saved);

        SetStatus(saved ? outcome.Summary : outcome.Summary + " The list of imported highlights could not be saved, so a new import can create duplicates.",
                  outcome.Error != null || saved == false);
      }, "The import was cancelled.");
    }

    private async Task RunBusyAsync(Func<CancellationToken, Task> work, string cancelledMessage)
    {
      using var cts = new CancellationTokenSource();
      _cts   = cts;
      IsBusy = true;
      SetStatus("Working...", false);
      CommandManager.InvalidateRequerySuggested();

      try
      {
        await work(cts.Token);
      }
      catch (OperationCanceledException)
      {
        SetStatus(cancelledMessage, false);
      }
      catch (Exception ex) when (ex is EpubFormatException or InvalidDataException or IOException or UnauthorizedAccessException)
      {
        LogTo.Warning(ex, "Books: the file {FilePath} could not be read", FilePath);
        SetStatus(ex.Message, true);
      }
      catch (Exception ex) when (ex is not OutOfMemoryException)
      {
        LogTo.Error(ex, "Books: unexpected error");
        SetStatus($"An unexpected error occurred: {ex.Message} The details are in the SMA log.", true);
      }
      finally
      {
        _cts   = null;
        IsBusy = false;
        CommandManager.InvalidateRequerySuggested();
      }
    }

    private void SetSelection(bool selected)
    {
      foreach (var item in Items)
        item.IsSelected = selected;
    }

    private void SetStatus(string message, bool isError)
    {
      Status  = message;
      IsError = isError;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
