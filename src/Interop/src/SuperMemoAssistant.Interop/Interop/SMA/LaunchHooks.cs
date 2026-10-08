// Contract types of launch hooks: the SuperMemo start a hook runs for, and what a hook reports back.
namespace SuperMemoAssistant.Interop.SMA
{
  using System;

  /// <summary>The SuperMemo start that a launch hook runs for. It crosses the plugin boundary by value.</summary>
  [Serializable]
  public sealed class SMLaunchInfo
  {
    /// <summary>Creates the description of one SuperMemo start.</summary>
    public SMLaunchInfo(string superMemoExePath, string collectionFolder)
    {
      SuperMemoExePath = superMemoExePath;
      CollectionFolder = collectionFolder;
    }

    /// <summary>Full path of sm20.exe, the file that is about to start or has just exited.</summary>
    public string SuperMemoExePath { get; }

    /// <summary>Folder that holds the data of the selected collection (not the folder that holds the .kno file).</summary>
    public string CollectionFolder { get; }
  }

  /// <summary>What a launch hook did and what it could not do. SMA shows the warnings to the user.</summary>
  [Serializable]
  public sealed class LaunchHookResult
  {
    /// <summary>Creates a result.</summary>
    public LaunchHookResult(string[] actions, string[] warnings)
    {
      Actions  = actions ?? Array.Empty<string>();
      Warnings = warnings ?? Array.Empty<string>();
    }

    /// <summary>A result for a hook that had nothing to do.</summary>
    public static LaunchHookResult Nothing => new LaunchHookResult(Array.Empty<string>(), Array.Empty<string>());

    /// <summary>What the hook changed. SMA writes it to its log.</summary>
    public string[] Actions { get; }

    /// <summary>Problems the user should know about. SMA shows each one as a desktop notification.</summary>
    public string[] Warnings { get; }
  }
}
