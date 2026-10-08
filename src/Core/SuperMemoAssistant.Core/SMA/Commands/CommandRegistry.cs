namespace SuperMemoAssistant.SMA.Commands
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using Interop.SMA;

  /// <summary>A palette command and the delegate that runs it (a proxy into the plugin process for plugin commands).</summary>
  public sealed record PaletteEntry(PaletteCommand Command, Action Execute);

  /// <summary>The commands of the palette, from SMA and from every running plugin. Thread-safe.</summary>
  public sealed class CommandRegistry
  {
    private readonly ConcurrentDictionary<string, PaletteEntry> _entries  = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long>         _lastUsed = new(StringComparer.OrdinalIgnoreCase);
    private          long                                       _useCount;

    /// <summary>Adds a command, or replaces the command with the same owner and id.</summary>
    public void Register(PaletteCommand command, Action execute)
    {
      if (command == null)
        throw new ArgumentNullException(nameof(command));

      _entries[command.Key] = new PaletteEntry(command, execute ?? throw new ArgumentNullException(nameof(execute)));
    }

    public void Unregister(string owner, string id) => _entries.TryRemove(owner + "/" + id, out _);

    /// <summary>Removes every command of a plugin that stopped, and returns how many were removed.</summary>
    public int RemoveOwner(string owner)
    {
      var removed = 0;

      foreach (var key in _entries.Where(e => string.Equals(e.Value.Command.Owner, owner, StringComparison.OrdinalIgnoreCase))
                                  .Select(e => e.Key)
                                  .ToList())
        if (_entries.TryRemove(key, out _))
          removed++;

      return removed;
    }

    public IReadOnlyList<PaletteEntry> Snapshot() => _entries.Values.ToList();

    /// <summary>Records that the user ran a command, so the palette lists recent commands first.</summary>
    public void MarkUsed(string key) => _lastUsed[key] = Interlocked.Increment(ref _useCount);

    /// <summary>Returns a number that grows with each use, or 0 for a command that was not used in this session.</summary>
    public long LastUsed(string key) => _lastUsed.TryGetValue(key, out var order) ? order : 0;
  }
}
