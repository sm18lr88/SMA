// Narrow x64 disassembly queries anchored on RTTI-located functions, used for symbols RTTI does not name.
namespace SuperMemoAssistant.Hooks.Symbols;

using Iced.Intel;

internal sealed class CodeShapes
{
  private const int MaxFunctionBytes = 0x2000;

  private readonly PeImage _pe;

  public CodeShapes(PeImage pe) => _pe = pe;

  /// <summary>Decodes a function from its entry point until its first top-level <c>ret</c>.</summary>
  public IReadOnlyList<Instruction> Function(ulong entryVa)
  {
    var offset = _pe.Offset(entryVa);
    if (offset < 0 || !_pe.IsCode(entryVa))
      throw new SymbolResolutionException($"Function entry 0x{entryVa:X} is not in .text.");

    var length  = (int)Math.Min(MaxFunctionBytes, _pe.Bytes.Length - offset);
    var decoder = Decoder.Create(64, new ByteArrayCodeReader(_pe.Bytes, (int)offset, length));
    decoder.IP  = entryVa;

    var result = new List<Instruction>();
    while (decoder.IP < entryVa + (ulong)length)
    {
      decoder.Decode(out var ins);
      if (ins.IsInvalid)
        break;

      result.Add(ins);
      if (ins.FlowControl == FlowControl.Return)
        break;
    }

    return result;
  }

  /// <summary>Direct near-call targets in program order.</summary>
  public static IEnumerable<(int Index, ulong Target)> Calls(IReadOnlyList<Instruction> body)
  {
    for (var i = 0; i < body.Count; i++)
      if (body[i].FlowControl == FlowControl.Call && body[i].Op0Kind == OpKind.NearBranch64)
        yield return (i, body[i].NearBranchTarget);
  }

  /// <summary>
  ///   Delphi x64 accesses unit globals through a pointer cell: <c>mov reg, [rip+cell]</c>. Returns the VA the
  ///   cell points to (the variable), or 0 when <paramref name="ins" /> is not such a load.
  /// </summary>
  public ulong GlobalFromCellLoad(in Instruction ins)
  {
    if (ins.Mnemonic != Mnemonic.Mov || !ins.IsIPRelativeMemoryOperand || ins.Op0Kind != OpKind.Register)
      return 0;

    return _pe.QwordAt(ins.IPRelativeMemoryAddress);
  }

  /// <summary>
  ///   Finds every direct call to <paramref name="target" /> in .text with a linear sweep, returning each call with the
  ///   <paramref name="context" /> instructions decoded before it (oldest first).
  /// </summary>
  public IEnumerable<(ulong Site, Instruction[] Before)> CallSites(ulong target, int context)
  {
    var text    = _pe.Text;
    var decoder = Decoder.Create(64, new ByteArrayCodeReader(_pe.Bytes, text.PointerToRawData, text.SizeOfRawData));
    decoder.IP  = _pe.TextStartVa;

    var ring  = new Instruction[context];
    var count = 0L;
    var end   = _pe.TextStartVa + (ulong)text.SizeOfRawData;

    while (decoder.IP < end)
    {
      decoder.Decode(out var ins);

      if (ins.FlowControl == FlowControl.Call && ins.Op0Kind == OpKind.NearBranch64 && ins.NearBranchTarget == target)
      {
        var n      = (int)Math.Min(count, context);
        var before = new Instruction[n];
        for (var i = 0; i < n; i++)
          before[i] = ring[(count - n + i) % context];

        yield return (ins.IP, before);
      }

      ring[count % context] = ins;
      count++;
    }
  }
}
