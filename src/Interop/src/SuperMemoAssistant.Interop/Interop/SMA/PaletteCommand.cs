namespace SuperMemoAssistant.Interop.SMA
{
  using System;
  using Services.IO.HotKeys;
  using Services.IO.Keyboard;

  /// <summary>A command of the SMA command palette (Ctrl+Alt+Shift+P). It crosses the plugin boundary by value.</summary>
  [Serializable]
  public sealed class PaletteCommand
  {
    /// <summary>The owner of the commands that SMA itself provides.</summary>
    public const string SmaOwner = "SuperMemoAssistant";

    /// <summary>Creates a command description.</summary>
    /// <param name="owner">The assembly name of the plugin. SMA removes the commands of a plugin when the plugin stops.</param>
    /// <param name="ownerName">The name that the palette shows next to the command, usually the plugin name.</param>
    /// <param name="id">The identifier of the command, unique for its owner.</param>
    /// <param name="title">What the command does, as the user searches for it.</param>
    /// <param name="hotKey">The hotkey that also runs the command, or null.</param>
    /// <param name="scopes">Where the command can run. The palette hides a command whose scope does not match.</param>
    public PaletteCommand(string owner, string ownerName, string id, string title, string hotKey, HotKeyScopes scopes)
    {
      Owner     = owner ?? throw new ArgumentNullException(nameof(owner));
      OwnerName = ownerName ?? owner;
      Id        = id ?? throw new ArgumentNullException(nameof(id));
      Title     = string.IsNullOrWhiteSpace(title) ? id : title;
      HotKey    = hotKey;
      Scopes    = scopes;
    }

    /// <summary>The assembly name of the plugin that provides the command.</summary>
    public string Owner { get; }

    /// <summary>The name that the palette shows next to the command.</summary>
    public string OwnerName { get; }

    /// <summary>The identifier of the command, unique for its owner.</summary>
    public string Id { get; }

    /// <summary>What the command does.</summary>
    public string Title { get; }

    /// <summary>The hotkey that also runs the command, as text, or null.</summary>
    public string HotKey { get; }

    /// <summary>Where the command can run.</summary>
    public HotKeyScopes Scopes { get; }

    /// <summary>The identifier of the command across all owners.</summary>
    public string Key => Owner + "/" + Id;

    /// <summary>Describes a global hotkey as a palette command.</summary>
    public static PaletteCommand FromHotKey(HotKeyData hotKey, string owner, string ownerName)
    {
      if (hotKey == null)
        throw new ArgumentNullException(nameof(hotKey));

      return new PaletteCommand(owner, ownerName, hotKey.Id, hotKey.Description, hotKey.ActualHotKey?.ToString(),
                                hotKey.Scopes ?? HotKeyScopes.SM);
    }
  }
}
