// Wire protocol between SMA and the injected agent, return-value normalisation, and the injector's PE export parser.
namespace SuperMemoAssistant.Tests.Agent;

using SuperMemoAssistant.Hooks.Agent;
using SuperMemoAssistant.SuperMemo;
using SuperMemoAssistant.SuperMemo.Hooks;
using Xunit;

public class AgentProtocolTests
{
  private static AgentMessage RoundTrip(AgentMessage message)
  {
    using var stream = new MemoryStream(AgentProtocol.Encode(message));
    return AgentProtocol.Read(stream)!;
  }

  [Fact]
  public void ConfigureAgent_RoundTrips()
  {
    var original = new ConfigureAgent(
      1234,
      [new NativeFunction(NativeMethod.ElWdw_GoToElement, 0xEE9A70 - 0x400000, NativeReturnKind.None),
       new NativeFunction(NativeMethod.FileSpace_IsSlotOccupied, 0x1000, NativeReturnKind.Byte)],
      0x1AB0, 0x28, [@"s:\collection\elementinfo.dat", "ünï.dat"]);

    var copy = Assert.IsType<ConfigureAgent>(RoundTrip(original));

    Assert.Equal(original.MainThreadId, copy.MainThreadId);
    Assert.Equal(original.Functions, copy.Functions);
    Assert.Equal(original.ElWindComponentDataOffset, copy.ElWindComponentDataOffset);
    Assert.Equal(original.QueueSizeOffset, copy.QueueSizeOffset);
    Assert.Equal(original.WatchedFilePaths, copy.WatchedFilePaths);
  }

  [Fact]
  public void ExecuteNative_PreservesIntegerAndTextArguments()
  {
    var original = new ExecuteNative(7, NativeMethod.TRegistry_ImportFile,
                                     [NativeArg.Of(0x7FF6_1234_5678L), NativeArg.Of("C:\\a b\\ö.png"), NativeArg.Of(-1)]);

    var copy = Assert.IsType<ExecuteNative>(RoundTrip(original));

    Assert.Equal(7u, copy.CallId);
    Assert.Equal(original.Method, copy.Method);
    Assert.Equal(original.Args, copy.Args);
    Assert.True(copy.Args[1].IsText);
    Assert.False(copy.Args[0].IsText);
  }

  [Fact]
  public void AgentToSmaMessages_RoundTrip()
  {
    Assert.Equal(new AgentReady(42, 0x7FF700000000), RoundTrip(new AgentReady(42, 0x7FF700000000)));
    Assert.Equal(new AgentConfigured(false, "boom"), RoundTrip(new AgentConfigured(false, "boom")));
    Assert.Equal(new AgentConfigured(true, null), RoundTrip(new AgentConfigured(true, null)));
    Assert.Equal(new NativeResult(3, true, -5, null), RoundTrip(new NativeResult(3, true, -5, null)));
    Assert.Equal(new FileCreated("x.dat", 0x1F4), RoundTrip(new FileCreated("x.dat", 0x1F4)));
    Assert.Equal(new FileSeeked(0x1F4, 4096), RoundTrip(new FileSeeked(0x1F4, 4096)));
    Assert.Equal(new FileClosed(0x1F4), RoundTrip(new FileClosed(0x1F4)));
    Assert.Equal(new AgentLog(AgentLogLevel.Warning, "careful"), RoundTrip(new AgentLog(AgentLogLevel.Warning, "careful")));
    Assert.IsType<ShutdownAgent>(RoundTrip(new ShutdownAgent()));

    var written = Assert.IsType<FileWritten>(RoundTrip(new FileWritten(9, [1, 2, 3, 255])));
    Assert.Equal(new byte[] { 1, 2, 3, 255 }, written.Data);
  }

  [Fact]
  public void Read_ReturnsNullOnCleanEndOfStream()
  {
    Assert.Null(AgentProtocol.Read(new MemoryStream()));
  }

  [Fact]
  public void Read_RejectsOversizedAndTruncatedFrames()
  {
    Assert.Throws<InvalidDataException>(() => AgentProtocol.Read(new MemoryStream(BitConverter.GetBytes(AgentProtocol.MaxFrameLength + 1))));

    var truncated = AgentProtocol.Encode(new FileClosed(1))[..^2];
    Assert.Throws<EndOfStreamException>(() => AgentProtocol.Read(new MemoryStream(truncated)));
  }

  [Fact]
  public void Read_RejectsUnknownTags()
  {
    var frame = BitConverter.GetBytes(1).Concat(new byte[] { 0xEE }).ToArray();

    Assert.Throws<InvalidDataException>(() => AgentProtocol.Read(new MemoryStream(frame)));
  }

  [Theory]
  [InlineData(NativeReturnKind.Byte, 0xDEADBE01L, 1L)]
  [InlineData(NativeReturnKind.SByte, 0x1FFL, -1L)]
  [InlineData(NativeReturnKind.Int16, 0x18000L, -32768L)]
  [InlineData(NativeReturnKind.UInt16, -1L, 65535L)]
  [InlineData(NativeReturnKind.Int32, 0xFFFFFFFFL, -1L)]
  [InlineData(NativeReturnKind.UInt32, -1L, 4294967295L)]
  [InlineData(NativeReturnKind.Pointer, 0x7FF612345678L, 0x7FF612345678L)]
  [InlineData(NativeReturnKind.None, 0x1234L, 0L)]
  public void NativeReturnKind_NormalizesGarbageInUpperRaxBits(NativeReturnKind kind, long rax, long expected)
  {
    Assert.Equal(expected, kind.Normalize(rax));
  }

  [Fact]
  public void ExportRva_FindsTheAgentEntryPoint_InThePublishedDll()
  {
    var dll = Path.Combine(RepoRoot(), "artifacts", "app-dev", "SuperMemoAssistant.Hooks.Agent.dll");
    Assert.SkipUnless(File.Exists(dll), "The published agent DLL is not built (build SuperMemoAssistant first).");

    Assert.True(AgentInjector.ExportRva(dll, AgentInjector.EntryPoint) > 0);
    Assert.Throws<InvalidOperationException>(() => AgentInjector.ExportRva(dll, "NoSuchExport"));
  }

  private static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
      dir = dir.Parent;
    return dir?.FullName ?? AppContext.BaseDirectory;
  }
}
