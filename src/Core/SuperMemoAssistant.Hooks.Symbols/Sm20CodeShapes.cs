// Code-shape rules for SuperMemo 20 symbols that RTTI does not name: unit globals and two unnamed TElWind methods.
// Each rule is anchored on an RTTI-named function and documents the instruction shape it expects.
namespace SuperMemoAssistant.Hooks.Symbols;

using Iced.Intel;
using SuperMemoAssistant.SuperMemo;

internal sealed class Sm20CodeShapes(DelphiRtti rtti, CodeShapes code, Func<string, string, ulong> method)
{
  /// <summary>
  ///   Program body: <c>Application.CreateForm(TSMMain, SMMain)</c> etc. compile to
  ///   <c>mov rax,[cell(Application)]; mov rcx,[rax]; mov rdx,[cell(VMT)]; mov r8,[cell(var)]; call CreateForm</c>.
  /// </summary>
  public IReadOnlyDictionary<NativePointer, ulong> FormGlobals()
  {
    var wanted = new Dictionary<string, NativePointer>
    {
      ["TSMMain"] = NativePointer.SMMain_InstancePtr,
      ["TElWind"] = NativePointer.ElWdw_InstancePtr,
    };
    var found       = new Dictionary<NativePointer, HashSet<ulong>>();
    var application = new HashSet<ulong>();

    foreach (var (_, before) in code.CallSites(method("TApplication", "CreateForm"), context: 8))
    {
      ulong vmt = 0, variable = 0;
      for (var i = 0; i < before.Length; i++)
      {
        var ins = before[i];
        if (ins.Op0Kind != OpKind.Register) continue;

        if (ins.Op0Register == Register.RDX) vmt      = code.GlobalFromCellLoad(ins);
        if (ins.Op0Register == Register.R8)  variable = code.GlobalFromCellLoad(ins);

        if (ins.Op0Register == Register.RAX && i + 1 < before.Length && IsLoadThrough(before[i + 1], Register.RCX, Register.RAX))
          application.Add(code.GlobalFromCellLoad(ins));
      }

      if (vmt != 0 && variable != 0 && rtti.ClassByVmt.TryGetValue(vmt, out var cls) && wanted.TryGetValue(cls, out var key))
        (found.TryGetValue(key, out var set) ? set : found[key] = new HashSet<ulong>()).Add(variable);
    }

    application.Remove(0);
    var result = found.ToDictionary(kv => kv.Key, kv => Single(kv.Value, kv.Key.ToString()));
    result[NativePointer.Application_InstancePtr] = Single(application, "Application");
    return result;
  }

  /// <summary>TSMMain.FormCreate: <c>call TDatabase.Create; mov rcx,[cell(Database)]; mov [rcx],rax</c>.</summary>
  public ulong Database()
  {
    var body   = code.Function(method("TSMMain", "FormCreate"));
    var create = method("TDatabase", "Create");

    foreach (var (i, target) in CodeShapes.Calls(body))
      if (target == create && i + 2 < body.Count && body[i + 2].Mnemonic == Mnemonic.Mov && body[i + 2].Op1Register == Register.RAX)
        return Required(code.GlobalFromCellLoad(body[i + 1]), "Database");

    throw new SymbolResolutionException("Database: TDatabase.Create result store not found in TSMMain.FormCreate.");
  }

  /// <summary>TSMMain.SelectDefaultConcept copies the concept into the current TConcept record: <c>mov rdi,[cell]; ...; rep movsq</c>.</summary>
  public ulong CurrentConceptRecord()
  {
    var body = code.Function(method("TSMMain", "SelectDefaultConcept"));
    var rep  = Index(body, ins => ins.Mnemonic == Mnemonic.Movsq && ins.HasRepPrefix, "rep movsq");

    for (var i = rep - 1; i >= 0; i--)
      if (body[i].Op0Kind == OpKind.Register && body[i].Op0Register == Register.RDI)
        return Required(code.GlobalFromCellLoad(body[i]), "TConcept record");

    throw new SymbolResolutionException("TConcept record: rdi load not found in TSMMain.SelectDefaultConcept.");
  }

  /// <summary>TContents.ArrangeFolders reads the children limit directly: <c>movzx r32, word ptr [rip+LimitChildrenCount]</c>.</summary>
  public ulong LimitChildrenCount()
  {
    var body = code.Function(method("TContents", "ArrangeFolders"));
    var i    = Index(body, ins => ins.Mnemonic == Mnemonic.Movzx && ins.IsIPRelativeMemoryOperand && ins.MemorySize == MemorySize.UInt16,
                     "movzx word [rip]");
    return body[i].IPRelativeMemoryAddress;
  }

  /// <summary>TContents.DeleteCurrentElement suppresses confirmation: <c>mov rax,[cell]; mov byte [rax],1; ...; call TDatabase.DeleteNode</c>.</summary>
  public ulong IgnoreUserConfirmation()
  {
    var body       = code.Function(method("TContents", "DeleteCurrentElement"));
    var deleteNode = method("TDatabase", "DeleteNode");

    foreach (var (call, target) in CodeShapes.Calls(body))
    {
      if (target != deleteNode) continue;

      for (var i = call - 1; i > 0 && i >= call - 8; i--)
        if (body[i].Mnemonic == Mnemonic.Mov && body[i].Op0Kind == OpKind.Memory && body[i].MemoryBase == Register.RAX
          && body[i].MemorySize == MemorySize.UInt8 && body[i].Immediate8 == 1)
          return Required(code.GlobalFromCellLoad(body[i - 1]), "IgnoreUserConfirmation");
    }

    throw new SymbolResolutionException("IgnoreUserConfirmation: flag store before TDatabase.DeleteNode not found.");
  }

  /// <summary>TElWind.DoneClick asks for confirmation, then calls the unnamed "Done" worker as its last call.</summary>
  public ulong Done() => LastCall(method("TElWind", "DoneClick"), "ElWdw_Done");

  /// <summary>MIForgetClick and MIJumpIntervalClick both end by restoring the learning mode through the same unnamed method.</summary>
  public ulong RestoreLearningMode()
  {
    var a = LastCall(method("TElWind", "MIForgetClick"), "RestoreLearningMode (MIForgetClick)");
    var b = LastCall(method("TElWind", "MIJumpIntervalClick"), "RestoreLearningMode (MIJumpIntervalClick)");

    return a == b ? a : throw new SymbolResolutionException($"RestoreLearningMode: candidates differ (0x{a:X} vs 0x{b:X}).");
  }

  private ulong LastCall(ulong function, string what)
  {
    var calls = CodeShapes.Calls(code.Function(function)).ToList();
    return calls.Count > 0 ? calls[^1].Target : throw new SymbolResolutionException($"{what}: no call found.");
  }

  private static bool IsLoadThrough(in Instruction ins, Register destination, Register baseRegister) =>
    ins.Mnemonic == Mnemonic.Mov && ins.Op0Kind == OpKind.Register && ins.Op0Register == destination
    && ins.Op1Kind == OpKind.Memory && ins.MemoryBase == baseRegister && ins.MemoryDisplacement64 == 0;

  private static int Index(IReadOnlyList<Instruction> body, Func<Instruction, bool> predicate, string what)
  {
    for (var i = 0; i < body.Count; i++)
      if (predicate(body[i]))
        return i;

    throw new SymbolResolutionException($"Instruction shape '{what}' not found.");
  }

  private static ulong Single(HashSet<ulong> candidates, string what) =>
    candidates.Count == 1 ? candidates.First()
      : throw new SymbolResolutionException($"{what}: expected one candidate, found {candidates.Count}.");

  private static ulong Required(ulong va, string what) =>
    va != 0 ? va : throw new SymbolResolutionException($"{what}: pointer cell is empty.");
}
