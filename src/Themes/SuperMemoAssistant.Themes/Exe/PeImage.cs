// Minimal PE editing: list sections, read and patch bytes at virtual addresses, append one section.
using System.Buffers.Binary;
using System.Text;

namespace SuperMemoAssistant.Themes.Exe;

internal sealed record PeSection(string Name, uint VirtualSize, uint VirtualAddress, uint RawSize, uint RawPointer);

internal sealed class PeImage
{
  /// <summary>Code, executable, readable.</summary>
  public const uint SectionCodeExecRead = 0x60000020;

  private readonly int _pe;
  private readonly int _opt;
  private readonly int _table;
  private readonly uint _sectionAlign;
  private readonly uint _fileAlign;
  private int _count;

  public PeImage(byte[] data)
  {
    Bytes          = [.. data];
    _pe            = BinaryPrimitives.ReadInt32LittleEndian(Bytes.AsSpan(0x3C));
    _count         = BinaryPrimitives.ReadUInt16LittleEndian(Bytes.AsSpan(_pe + 6));
    _opt           = _pe + 24;
    var optSize    = BinaryPrimitives.ReadUInt16LittleEndian(Bytes.AsSpan(_pe + 20));
    ImageBase      = BinaryPrimitives.ReadUInt64LittleEndian(Bytes.AsSpan(_opt + 24));
    _sectionAlign  = BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(_opt + 32));
    _fileAlign     = BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(_opt + 36));
    _table         = _opt + optSize;
  }

  public byte[] Bytes { get; private set; }

  public ulong ImageBase { get; }

  public IReadOnlyList<PeSection> Sections()
  {
    var sections = new List<PeSection>();

    for (var i = 0; i < _count; i++)
    {
      var at   = _table + 40 * i;
      var name = Encoding.ASCII.GetString(Bytes, at, 8).TrimEnd('\0');

      sections.Add(new PeSection(
        name,
        BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(at + 8)),
        BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(at + 12)),
        BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(at + 16)),
        BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(at + 20))));
    }

    return sections;
  }

  public bool HasSection(string name) => Sections().Any(s => s.Name == name);

  private static ulong Align(ulong value, ulong to) => (value + to - 1) / to * to;

  public int FileOffset(ulong va)
  {
    var rva = va - ImageBase;

    foreach (var s in Sections())
    {
      if (s.VirtualAddress <= rva && rva < (ulong)s.VirtualAddress + Math.Max(s.VirtualSize, s.RawSize))
        return (int)(s.RawPointer + rva - s.VirtualAddress);
    }

    throw new InvalidOperationException($"VA {va:X} is not inside a section");
  }

  public byte[] Read(ulong va, int size) => Bytes.AsSpan(FileOffset(va), size).ToArray();

  public void Write(ulong va, ReadOnlySpan<byte> data) => data.CopyTo(Bytes.AsSpan(FileOffset(va)));

  /// <summary>Virtual address where the next appended section starts.</summary>
  public ulong NextVa()
  {
    var last = Sections().MaxBy(s => s.VirtualAddress)!;

    return ImageBase + Align(last.VirtualAddress + last.VirtualSize, _sectionAlign);
  }

  /// <summary>Appends a section after the last one and returns its virtual address.</summary>
  public ulong AddSection(string name, byte[] data, uint characteristics = SectionCodeExecRead)
  {
    var firstRaw = Sections().Where(s => s.RawPointer != 0).Min(s => s.RawPointer);

    if (_table + 40 * (_count + 1) > firstRaw)
      throw new InvalidOperationException("no room for another section header");

    var va   = NextVa();
    var rptr = (int)Align((ulong)Bytes.Length, _fileAlign);
    var rawSize = (uint)Align((ulong)data.Length, _fileAlign);
    var bytes = new byte[rptr + rawSize];

    Bytes.CopyTo(bytes, 0);
    data.CopyTo(bytes, rptr);
    Bytes = bytes;

    var header = new byte[40];

    Encoding.ASCII.GetBytes(name).CopyTo(header, 0);
    BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8), (uint)data.Length);
    BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)(va - ImageBase));
    BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), rawSize);
    BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(20), (uint)rptr);
    BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(36), characteristics);
    header.CopyTo(Bytes, _table + 40 * _count);

    _count++;
    BinaryPrimitives.WriteUInt16LittleEndian(Bytes.AsSpan(_pe + 6), (ushort)_count);

    var lastEnd = va - ImageBase + Align((ulong)data.Length, _sectionAlign);

    BinaryPrimitives.WriteUInt32LittleEndian(Bytes.AsSpan(_opt + 56), (uint)lastEnd); // SizeOfImage
    BinaryPrimitives.WriteUInt32LittleEndian(Bytes.AsSpan(_opt + 64), 0);             // CheckSum: not validated for user-mode exes

    return va;
  }
}
