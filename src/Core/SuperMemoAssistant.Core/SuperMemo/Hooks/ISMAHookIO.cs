// Receives SuperMemo's writes to collection files, as forwarded by the agent's IAT hooks.
namespace SuperMemoAssistant.SuperMemo.Hooks
{
  using System;
  using System.Collections.Generic;

  public interface ISMAHookIO
  {
    /// <summary>Lower-cased full paths of the collection files this sink tracks.</summary>
    IEnumerable<string> GetTargetFilePaths();

    void OnFileCreate(string filePath, IntPtr fileHandle);

    void OnFileSeek(IntPtr fileHandle, uint position);

    void OnFileWrite(IntPtr fileHandle, byte[] buffer, uint count);

    void OnFileClose(IntPtr fileHandle);
  }
}
