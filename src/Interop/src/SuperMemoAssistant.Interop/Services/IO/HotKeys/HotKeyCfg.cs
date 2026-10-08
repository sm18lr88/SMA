using System.Collections.Generic;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Services.IO.HotKeys
{
  /// <summary>
  /// Config file which holds defaults or user-defined hotkey bindings
  /// </summary>
  public class HotKeyCfg
  {
    /// <summary>
    /// Maps actions to hotkeys
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
    public Dictionary<string, HotKey> HotKeyMap { get; set; } = new Dictionary<string, HotKey>();
  }
}
