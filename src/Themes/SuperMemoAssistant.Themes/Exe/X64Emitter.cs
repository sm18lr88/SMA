// Appends x64 instructions at a known virtual address, so rel32 and RIP-relative operands can be computed.
using System.Buffers.Binary;
using System.Text;

namespace SuperMemoAssistant.Themes.Exe;

/// <summary>Appends instructions at a known virtual address, so rel32 operands can be computed.</summary>
internal sealed class X64Emitter(ulong baseVa)
{
  private readonly List<byte> _bytes = [];

  private ulong Va => baseVa + (ulong)_bytes.Count;

  public void Raw(params int[] bytes) => _bytes.AddRange(bytes.Select(b => (byte)b));

  public void Call(ulong target)
  {
    var rel = checked((int)((long)target - ((long)Va + 5)));

    _bytes.Add(0xE8);
    AddInt(rel);
  }

  /// <summary><c>mov rax, [rip+disp]</c> reading the 8 bytes at <paramref name="target" />.</summary>
  public void RipLoadRax(ulong target)
  {
    var rel = checked((int)((long)target - ((long)Va + 7)));

    Raw(0x48, 0x8B, 0x05);
    AddInt(rel);
  }

  public int LeaR8Placeholder()
  {
    Raw(0x4C, 0x8D, 0x05);
    AddInt(0);

    return _bytes.Count - 4;
  }

  public int ShortJumpIfNotEqualPlaceholder()
  {
    Raw(0x75, 0x00);

    return _bytes.Count - 1;
  }

  public void ResolveShortJump(int operandAt)
  {
    var distance = _bytes.Count - (operandAt + 1);

    _bytes[operandAt] = checked((byte)distance);
  }

  /// <summary>A Delphi UnicodeString constant (code page, element size, refcount -1 so it is never freed, length); returns the offset of the characters.</summary>
  public int Literal(string text)
  {
    AddShort(1200);
    AddShort(2);
    AddInt(-1);
    AddInt(text.Length);

    var at = _bytes.Count;

    _bytes.AddRange(Encoding.Unicode.GetBytes(text));
    AddShort(0);

    return at;
  }

  public void ResolveLea(int operandAt, int literalAt) => PatchInt(operandAt, literalAt - (operandAt + 4));

  public byte[] ToArray() => [.. _bytes];

  private void AddInt(int value)
  {
    Span<byte> four = stackalloc byte[4];

    BinaryPrimitives.WriteInt32LittleEndian(four, value);
    _bytes.AddRange(four.ToArray());
  }

  private void AddShort(ushort value) => _bytes.AddRange(BitConverter.GetBytes(value));

  private void PatchInt(int at, int value)
  {
    var four = BitConverter.GetBytes(value);

    for (var i = 0; i < 4; i++)
      _bytes[at + i] = four[i];
  }
}
