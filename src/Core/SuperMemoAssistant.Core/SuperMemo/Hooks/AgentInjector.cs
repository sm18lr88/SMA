// Loads the NativeAOT agent DLL into a (suspended) sm20.exe and starts it with the name of SMA's pipe.
namespace SuperMemoAssistant.SuperMemo.Hooks
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Reflection.PortableExecutable;
  using System.Threading.Tasks;

  internal static class AgentInjector
  {
    public const string EntryPoint = "SmaAgentStart";

    private static readonly TimeSpan LoadTimeout      = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    ///   Loads <paramref name="agentPath" /> with LoadLibraryW, then runs its <see cref="EntryPoint" /> export on a remote
    ///   thread. The returned task completes when the agent's handshake (driven by SMA's pipe) has finished.
    /// </summary>
    public static Task<uint> InjectAsync(NativeProcess.StartedProcess process, string agentPath, string pipeName)
    {
      agentPath = Path.GetFullPath(agentPath); // the loader reports module paths with backslashes; SMA paths may use '/'
      var exportRva = ExportRva(agentPath, EntryPoint);
      var path      = NativeProcess.WriteString(process.ProcessHandle, agentPath);

      try
      {
        if (NativeProcess.RunRemote(process.ProcessHandle, NativeProcess.Kernel32Export("LoadLibraryW"), path, LoadTimeout) == 0)
          throw new InvalidOperationException($"LoadLibraryW failed for {agentPath} inside SuperMemo.");
      }
      finally
      {
        NativeProcess.Free(process.ProcessHandle, path);
      }

      var agentBase = RemoteModuleBase(process.ProcessId, agentPath);
      var argument  = NativeProcess.WriteString(process.ProcessHandle, pipeName);

      return Task.Run(() =>
      {
        try
        {
          return NativeProcess.RunRemote(process.ProcessHandle, agentBase + (nint)exportRva, argument, HandshakeTimeout);
        }
        finally
        {
          NativeProcess.Free(process.ProcessHandle, argument);
        }
      });
    }

    private static IntPtr RemoteModuleBase(int processId, string modulePath)
    {
      using var process = Process.GetProcessById(processId);
      var module = process.Modules.Cast<ProcessModule>()
                          .FirstOrDefault(m => string.Equals(Path.GetFullPath(m.FileName), modulePath, StringComparison.OrdinalIgnoreCase));

      return module?.BaseAddress ?? throw new InvalidOperationException($"{Path.GetFileName(modulePath)} is not loaded in SuperMemo.");
    }

    /// <summary>RVA of a named export, read from the DLL file (identical to its in-memory layout).</summary>
    internal static uint ExportRva(string dllPath, string exportName)
    {
      using var reader = new PEReader(File.OpenRead(dllPath));
      var directory = reader.PEHeaders.PEHeader!.ExportTableDirectory;
      var image     = reader.GetEntireImage().GetContent().ToArray();

      int Offset(int rva)
      {
        var section = reader.PEHeaders.SectionHeaders.First(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.VirtualSize);
        return rva - section.VirtualAddress + section.PointerToRawData;
      }

      uint U32(int offset) => BitConverter.ToUInt32(image.AsSpan(offset, 4));

      var dir       = Offset(directory.RelativeVirtualAddress);
      var count     = (int)U32(dir + 24);
      var functions = Offset((int)U32(dir + 28));
      var names     = Offset((int)U32(dir + 32));
      var ordinals  = Offset((int)U32(dir + 36));

      for (var i = 0; i < count; i++)
      {
        var nameOffset = Offset((int)U32(names + i * 4));
        var end        = Array.IndexOf(image, (byte)0, nameOffset);
        if (System.Text.Encoding.ASCII.GetString(image, nameOffset, end - nameOffset) != exportName)
          continue;

        var ordinal = BitConverter.ToUInt16(image.AsSpan(ordinals + i * 2, 2));
        return U32(functions + ordinal * 4);
      }

      throw new InvalidOperationException($"{Path.GetFileName(dllPath)} does not export {exportName}.");
    }
  }
}
