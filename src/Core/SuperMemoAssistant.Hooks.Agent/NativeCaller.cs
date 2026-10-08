// Calls SuperMemo's Delphi functions on the current thread. Delphi x64 uses the Windows x64 ABI: Self in RCX, then RDX, R8, R9, stack.
namespace SuperMemoAssistant.Hooks.Agent;

using System.Runtime.InteropServices;
using System.Text;
using SuperMemoAssistant.SuperMemo;

internal sealed unsafe class NativeCaller(nint moduleBase, ConfigureAgent config)
{
  private const int MaxArgs = 6;

  private readonly Dictionary<NativeMethod, NativeFunction> _functions = config.Functions.ToDictionary(f => f.Method);

  public long Execute(NativeMethod method, IReadOnlyList<NativeArg> args)
  {
    var strings = new List<nint>();
    try
    {
      var values = args.Select(a => a.IsText ? Track(strings, DelphiString.Allocate(a.Text!)) : (nint)a.Value).ToArray();
      return Dispatch(method, values);
    }
    finally
    {
      foreach (var s in strings)
        DelphiString.Free(s);
    }
  }

  private long Dispatch(NativeMethod method, nint[] a)
  {
    switch (method)
    {
      case NativeMethod.AppendAndAddElementFromText:
        // AppendElement(elWdw, elType, automatic: false) then AddElementFromText(elWdw, description); both on one main-thread turn.
        var elementId = Call(NativeMethod.ElWdw_AppendElement, a[0], a[1], 0);
        if (elementId <= 0)
          return -1;

        Call(NativeMethod.ElWdw_AddElementFromText, a[0], a[2]);
        return elementId;

      case NativeMethod.PostponeRepetition:
        Call(NativeMethod.ElWdw_ExecuteUncommittedRepetition, a[0], 1, 0);
        Call(NativeMethod.ElWdw_ScheduleInInterval, a[0], a[1]);
        Call(NativeMethod.ElWdw_NextElementInLearningQueue, a[0]);
        return 1;

      case NativeMethod.ForceRepetitionAndResume:
        Call(NativeMethod.ElWdw_ForceRepetitionExt, a[0], a[1], a[2]);
        Call(NativeMethod.ElWdw_NextElementInLearningQueue, a[0]);
        Call(NativeMethod.ElWdw_RestoreLearningMode, a[0]);
        return 1;

      case NativeMethod.ElWdw_SetText:
        // TElWind.SetText(CompNo, Html) was Self.ComponentData.SetText(CompNo, Html); SM20's linker removed the wrapper.
        var componentData = *(nint*)(a[0] + config.ElWindComponentDataOffset);
        Call(NativeMethod.TCompData_SetText, componentData, a[1], a[2]);
        return 1;

      case NativeMethod.ElWdw_EnterUpdateLock:
        // EnterUpdateLock(Lock: Boolean; var WasSwapped: Boolean): return WasSwapped for the matching QuitUpdateLock.
        byte wasSwapped = 0;
        Call(NativeMethod.ElWdw_EnterUpdateLock, a[0], a[1], (nint)(&wasSwapped));
        return wasSwapped;

      case NativeMethod.Queue_Last:
        // TQueue.Last = if Size = 0 then 0 else GetItem(Size).
        var size = *(int*)(a[0] + config.QueueSizeOffset);
        return size == 0 ? 0 : Call(NativeMethod.Queue_GetItem, a[0], size);

      default:
        return Call(method, a);
    }
  }

  private long Call(NativeMethod method, params nint[] args)
  {
    if (!_functions.TryGetValue(method, out var function))
      throw new InvalidOperationException($"{method} is not available in this SuperMemo build.");
    if (args.Length > MaxArgs)
      throw new ArgumentException($"{method}: at most {MaxArgs} arguments are supported.", nameof(args));

    var p = new nint[MaxArgs];
    args.CopyTo(p, 0);

    // Extra arguments are harmless under the x64 ABI: the caller owns the shadow/stack space.
    var target = (delegate* unmanaged<nint, nint, nint, nint, nint, nint, nint>)(moduleBase + (nint)function.Rva);
    long rax   = target(p[0], p[1], p[2], p[3], p[4], p[5]);
    return function.ReturnKind.Normalize(rax);
  }

  private static nint Track(List<nint> list, nint value)
  {
    list.Add(value);
    return value;
  }
}

/// <summary>Delphi x64 UnicodeString with reference count -1 (a constant): Delphi copies it on assignment and never frees it.</summary>
internal static unsafe class DelphiString
{
  private const int HeaderSize = 16; // padding:int32, codePage:uint16, elemSize:uint16, refCnt:int32, length:int32

  public static nint Allocate(string text)
  {
    var bytes = Encoding.Unicode.GetByteCount(text);
    var block = (byte*)NativeMemory.Alloc((nuint)(HeaderSize + bytes + 2));

    *(int*)(block + 0)     = 0;
    *(ushort*)(block + 4)  = 1200; // UTF-16LE
    *(ushort*)(block + 6)  = 2;
    *(int*)(block + 8)     = -1;
    *(int*)(block + 12)    = text.Length;
    fixed (char* chars = text)
      Buffer.MemoryCopy(chars, block + HeaderSize, bytes, bytes);
    *(char*)(block + HeaderSize + bytes) = '\0';

    return (nint)(block + HeaderSize);
  }

  public static void Free(nint chars) => NativeMemory.Free((byte*)chars - HeaderSize);
}
