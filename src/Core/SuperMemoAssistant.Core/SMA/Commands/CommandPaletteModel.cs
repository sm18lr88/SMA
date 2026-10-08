namespace SuperMemoAssistant.SMA.Commands
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using Services.IO.Keyboard;

  /// <summary>Where the user opened the palette. It decides which commands can run.</summary>
  public enum PaletteContext
  {
    /// <summary>The SuperMemo element window was in the foreground.</summary>
    ElementWindow,

    /// <summary>Another SuperMemo window was in the foreground.</summary>
    SuperMemo,

    /// <summary>Another application was in the foreground.</summary>
    OtherApplication,
  }

  /// <summary>The state of the palette window: the query, the ranked commands, and the selected command.</summary>
  public sealed class CommandPaletteModel : INotifyPropertyChanged
  {
    private readonly IReadOnlyList<PaletteEntry> _entries;
    private readonly Func<string, long>          _lastUsed;

    public CommandPaletteModel(IEnumerable<PaletteEntry> entries, Func<string, long> lastUsed, PaletteContext context)
    {
      _lastUsed = lastUsed ?? throw new ArgumentNullException(nameof(lastUsed));
      _entries  = (entries ?? throw new ArgumentNullException(nameof(entries))).Where(e => AppliesTo(e.Command.Scopes, context))
                                                                                .ToList();
      Results = Rank(Query);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public string Query { get; set; } = string.Empty;

    public IReadOnlyList<PaletteEntry> Results { get; private set; }

    public int SelectedIndex { get; set; }

    public PaletteEntry Selected => SelectedIndex >= 0 && SelectedIndex < Results.Count ? Results[SelectedIndex] : null;

    /// <summary>Moves the selection by <paramref name="delta" /> rows and stops at the first and the last command.</summary>
    public void MoveSelection(int delta)
    {
      if (Results.Count > 0)
        SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, Results.Count - 1);
    }

    /// <summary>A command that needs SuperMemo in the foreground cannot run from another application.</summary>
    public static bool AppliesTo(HotKeyScopes scopes, PaletteContext context) => context switch
    {
      PaletteContext.ElementWindow => true,
      PaletteContext.SuperMemo     => scopes != HotKeyScopes.SMBrowser,
      _                            => scopes == HotKeyScopes.Global,
    };

    /// <summary>Called by PropertyChanged.Fody after <see cref="Query" /> changes.</summary>
    private void OnQueryChanged() => Results = Rank(Query);

    private IReadOnlyList<PaletteEntry> Rank(string query)
    {
      var ranked = string.IsNullOrWhiteSpace(query)
        ? _entries.OrderByDescending(e => _lastUsed(e.Command.Key))
                  .ThenBy(e => e.Command.OwnerName, StringComparer.CurrentCultureIgnoreCase)
                  .ThenBy(e => e.Command.Title, StringComparer.CurrentCultureIgnoreCase)
                  .ToList()
        : _entries.Select(e => (Entry: e, Score: CommandMatcher.Score(query, e.Command.Title + " " + e.Command.OwnerName)))
                  .Where(r => r.Score.HasValue)
                  .OrderByDescending(r => r.Score)
                  .ThenByDescending(r => _lastUsed(r.Entry.Command.Key))
                  .ThenBy(r => r.Entry.Command.Title, StringComparer.CurrentCultureIgnoreCase)
                  .Select(r => r.Entry)
                  .ToList();

      SelectedIndex = ranked.Count > 0 ? 0 : -1;
      return ranked;
    }
  }
}
