// Messages exchanged between SMA and the agent injected into sm20.exe.
namespace SuperMemoAssistant.Hooks.Agent
{
  using System.Collections.Generic;
  using SuperMemoAssistant.SuperMemo;

  public abstract record AgentMessage;

  /// <summary>An argument for a native call: either an integer/pointer or a string marshalled as a Delphi UnicodeString.</summary>
  public readonly record struct NativeArg(long Value, string? Text)
  {
    public bool IsText => Text is not null;

    public static NativeArg Of(long value) => new(value, null);

    public static NativeArg Of(string text) => new(0, text);
  }

  public readonly record struct NativeFunction(NativeMethod Method, long Rva, NativeReturnKind ReturnKind);

  // SMA -> agent

  /// <summary>Function table and the field offsets that composite calls need. Sent once after <see cref="AgentReady" />.</summary>
  public sealed record ConfigureAgent(
    int                           MainThreadId,
    IReadOnlyList<NativeFunction> Functions,
    int                           ElWindComponentDataOffset,
    int                           QueueSizeOffset,
    IReadOnlyList<string>         WatchedFilePaths) : AgentMessage;

  public sealed record ExecuteNative(uint CallId, NativeMethod Method, IReadOnlyList<NativeArg> Args) : AgentMessage;

  public sealed record ShutdownAgent : AgentMessage;

  // agent -> SMA

  public sealed record AgentReady(int ProcessId, long ModuleBase) : AgentMessage;

  public sealed record AgentConfigured(bool Success, string? Error) : AgentMessage;

  public sealed record NativeResult(uint CallId, bool Success, long Value, string? Error) : AgentMessage;

  public sealed record FileCreated(string Path, long Handle) : AgentMessage;

  public sealed record FileSeeked(long Handle, long Position) : AgentMessage;

  public sealed record FileWritten(long Handle, byte[] Data) : AgentMessage;

  public sealed record FileClosed(long Handle) : AgentMessage;

  public sealed record AgentLog(AgentLogLevel Level, string Message) : AgentMessage;

  public enum AgentLogLevel : byte
  {
    Debug,
    Information,
    Warning,
    Error,
  }
}
