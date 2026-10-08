// Verifies the SuperMemo 20 symbol resolver against the real sm20.exe when it is available on this machine.
namespace SuperMemoAssistant.Tests.Symbols;

using SuperMemoAssistant.Hooks.Symbols;
using SuperMemoAssistant.SuperMemo;
using Xunit;

public class SymbolResolverTests
{
  private const string PinnedBuild = "x64-6A2496CE-00C63200";

  private static string? Sm20Path => SuperMemoLocation.Exe;

  [Fact]
  public void PinnedTable_CoversEverySymbolSmaUses()
  {
    var pinned = SymbolResolver.Pinned(PinnedBuild);

    Assert.NotNull(pinned);
    var composites = new[]
    {
      NativeMethod.ElWdw_SetText, NativeMethod.Queue_Last, NativeMethod.AppendAndAddElementFromText,
      NativeMethod.PostponeRepetition, NativeMethod.ForceRepetitionAndResume,
    };
    Assert.Empty(Enum.GetValues<NativeMethod>().Except(composites).Except(pinned.Methods.Keys));
    Assert.Empty(Enum.GetValues<NativePointer>().Except(pinned.Pointers.Keys));
  }

  [Fact]
  public void PinnedTable_RoundTripsThroughJson()
  {
    var pinned = SymbolResolver.Pinned(PinnedBuild)!;

    Assert.Empty(SymbolTable.FromJson(pinned.ToJson()).Diff(pinned));
  }

  [Fact]
  public void Resolve_Sm20_MatchesPinnedTable()
  {
    Assert.SkipWhen(Sm20Path is null, "sm20.exe is not available (set SMA_SM20_EXE).");

    var resolved = SymbolResolver.Resolve(Sm20Path!);

    Assert.Equal(PinnedBuild, resolved.BuildId);
    Assert.Empty(resolved.Diff(SymbolResolver.Pinned(PinnedBuild)!));
  }

  [Fact]
  public void Resolve_Sm20_KnownLayouts()
  {
    Assert.SkipWhen(Sm20Path is null, "sm20.exe is not available (set SMA_SM20_EXE).");

    var table = SymbolResolver.Resolve(Sm20Path!);

    // Values cross-checked by hand against the disassembly of the 2026-06-06 build.
    Assert.Equal(0x1BB9, table.Offset(NativePointer.ElWdw_ElementIdPtr));
    Assert.Equal(17, table.Offset(NativePointer.ElWdw_ComponentData_ComponentDataArrItemLength));
    Assert.Equal(0x20, table.Pointers[NativePointer.Globals_CurrentConceptIdPtr] - table.Pointers[NativePointer.Globals_CurrentConceptGroupIdPtr]);
    Assert.Equal(NativeReturnKind.Byte, table.Methods[NativeMethod.FileSpace_IsSlotOccupied].ReturnKind);
    Assert.Equal((IntPtr)(0x10000 + 0xEAEB90), table.GlobalAddress(NativePointer.ElWdw_InstancePtr, 0x10000));
  }

  [Fact]
  public void Resolve_RejectsX86Images()
  {
    var x86 = Path.Combine(AppContext.BaseDirectory, "x86-stub.bin");
    File.WriteAllBytes(x86, MinimalPe(machine: 0x014C));

    var ex = Assert.Throws<SymbolResolutionException>(() => SymbolResolver.Resolve(x86));
    Assert.Contains("64-bit", ex.Message);
  }

  private static byte[] MinimalPe(ushort machine)
  {
    var pe = new byte[0x200];
    pe[0] = (byte)'M'; pe[1] = (byte)'Z';
    BitConverter.TryWriteBytes(pe.AsSpan(0x3C), 0x40);
    pe[0x40] = (byte)'P'; pe[0x41] = (byte)'E';
    BitConverter.TryWriteBytes(pe.AsSpan(0x44), machine);
    BitConverter.TryWriteBytes(pe.AsSpan(0x54), (ushort)0xE0);  // SizeOfOptionalHeader
    BitConverter.TryWriteBytes(pe.AsSpan(0x58), (ushort)0x10B); // PE32 magic
    return pe;
  }
}
