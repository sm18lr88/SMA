// Read-only view of a PE32+ image file: VA/RVA translation and little-endian primitive reads.
namespace SuperMemoAssistant.Hooks.Symbols;

using System.Reflection.PortableExecutable;
using System.Text;

internal sealed class PeImage
{
  private readonly SectionHeader[] _sections;

  public PeImage(byte[] bytes)
  {
    Bytes = bytes;
    using var reader = new PEReader(new MemoryStream(bytes, writable: false));
    var headers = reader.PEHeaders;

    if (headers.CoffHeader.Machine != Machine.Amd64 || headers.PEHeader is null)
      throw new SymbolResolutionException("The executable is not a 64-bit (x64) PE image.");

    ImageBase  = headers.PEHeader.ImageBase;
    _sections  = headers.SectionHeaders.ToArray();
    BuildId    = $"x64-{(uint)headers.CoffHeader.TimeDateStamp:X8}-{(uint)headers.PEHeader.SizeOfCode:X8}";
    Text       = _sections.FirstOrDefault(s => s.Name == ".text");

    if (Text.SizeOfRawData == 0)
      throw new SymbolResolutionException("The executable has no .text section.");
  }

  public byte[] Bytes { get; }

  public ulong ImageBase { get; }

  /// <summary>Stable identity of a SuperMemo build: machine, link timestamp and code size. Resource-only patches keep it.</summary>
  public string BuildId { get; }

  public SectionHeader Text { get; }

  public IReadOnlyList<SectionHeader> Sections => _sections;

  public ulong TextStartVa => ImageBase + (ulong)Text.VirtualAddress;

  public ulong TextEndVa => TextStartVa + (ulong)Text.VirtualSize;

  public bool IsCode(ulong va) => va >= TextStartVa && va < TextEndVa;

  public long ToRva(ulong va) => (long)(va - ImageBase);

  /// <summary>Returns the file offset of <paramref name="va" />, or -1 when it is not backed by file data.</summary>
  public long Offset(ulong va)
  {
    if (va < ImageBase)
      return -1;

    var rva = va - ImageBase;

    foreach (var s in _sections)
    {
      var start = (ulong)s.VirtualAddress;
      var size  = (ulong)Math.Max(s.VirtualSize, s.SizeOfRawData);

      if (rva < start || rva >= start + size)
        continue;

      var delta = rva - start;
      return delta < (ulong)s.SizeOfRawData ? s.PointerToRawData + (long)delta : -1;
    }

    return -1;
  }

  public ushort U16(long offset) => BitConverter.ToUInt16(Bytes, (int)offset);

  public uint U32(long offset) => BitConverter.ToUInt32(Bytes, (int)offset);

  public ulong U64(long offset) => BitConverter.ToUInt64(Bytes, (int)offset);

  /// <summary>Reads the qword stored at <paramref name="va" />, or 0 when unmapped.</summary>
  public ulong QwordAt(ulong va)
  {
    var o = Offset(va);
    return o < 0 ? 0 : U64(o);
  }

  /// <summary>Reads a Delphi ShortString (length byte followed by UTF-8 bytes).</summary>
  public string ShortString(long offset) =>
    offset < 0 ? string.Empty : Encoding.UTF8.GetString(Bytes, (int)offset + 1, Bytes[offset]);

  public int ShortStringSize(long offset) => 1 + Bytes[offset];
}
