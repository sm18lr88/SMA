// Width and signedness of a Delphi function result in RAX; the agent normalizes RAX with it.
namespace SuperMemoAssistant.SuperMemo
{
  /// <summary>
  ///   Delphi x64 only defines the low bits of RAX that the result type occupies (e.g. AL for Boolean). The agent masks
  ///   or sign-extends the raw register with this kind before returning it to SMA.
  /// </summary>
  public enum NativeReturnKind : byte
  {
    None,
    SByte,
    Byte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Pointer,
  }

  public static class NativeReturnKindEx
  {
    public static long Normalize(this NativeReturnKind kind, long rax) => kind switch
    {
      NativeReturnKind.SByte  => (sbyte)rax,
      NativeReturnKind.Byte   => (byte)rax,
      NativeReturnKind.Int16  => (short)rax,
      NativeReturnKind.UInt16 => (ushort)rax,
      NativeReturnKind.Int32  => (int)rax,
      NativeReturnKind.UInt32 => (uint)rax,
      NativeReturnKind.Pointer => rax,
      _ => 0,
    };
  }
}
