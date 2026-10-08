// Delphi x64 extended RTTI reader: class VMTs, methods with signatures, fields and type info.
namespace SuperMemoAssistant.Hooks.Symbols;

using System.Text;
using SuperMemoAssistant.SuperMemo;

/// <summary>A method found in a class VMT method table.</summary>
internal sealed record DelphiMethod(string ClassName, string Name, ulong CodeVa, NativeReturnKind ReturnKind);

/// <summary>Reads Delphi x64 (XE2+) class metadata. Offsets follow System.pas vmt* constants for CPU64.</summary>
internal sealed class DelphiRtti
{
  private const int VmtSelfPtr     = -200;
  private const int VmtFieldTable  = -160;
  private const int VmtMethodTable = -152;
  private const int VmtClassName   = -136;
  private const int VmtInstanceSize = -128;

  private const byte TkInteger     = 1;
  private const byte TkEnumeration = 3;
  private const byte TkClass       = 7;
  private const byte TkRecord      = 14;
  private const byte TkPointer     = 20;

  private readonly PeImage                          _pe;
  private readonly Dictionary<string, List<ulong>>  _vmts = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<ulong, string>        _classByVmt = new();

  public DelphiRtti(PeImage pe)
  {
    _pe = pe;
    IndexVmts();
  }

  public IReadOnlyDictionary<ulong, string> ClassByVmt => _classByVmt;

  /// <summary>All VMTs of classes named <paramref name="className" /> (unit names are not unique keys).</summary>
  public IReadOnlyList<ulong> Vmts(string className) =>
    _vmts.TryGetValue(className, out var list) ? list : Array.Empty<ulong>();

  public int InstanceSize(ulong vmt) => (int)_pe.U32(_pe.Offset((ulong)((long)vmt + VmtInstanceSize)));

  public IEnumerable<DelphiMethod> Methods(ulong vmt)
  {
    var className = _classByVmt[vmt];
    var table     = _pe.QwordAt((ulong)((long)vmt + VmtMethodTable));
    var o         = _pe.Offset(table);

    if (table == 0 || o < 0)
      yield break;

    int count = _pe.U16(o);
    o += 2;

    for (var i = 0; i < count; i++)
    {
      yield return ReadMethodEntry(className, o);
      o += _pe.U16(o);
    }

    int exCount = _pe.U16(o);
    o += 2;

    for (var i = 0; i < exCount; i++, o += 12)
    {
      var entry = _pe.Offset(_pe.U64(o));
      if (entry >= 0)
        yield return ReadMethodEntry(className, entry);
    }
  }

  /// <summary>Extended field RTTI: (name, offset) pairs, all visibilities.</summary>
  public IEnumerable<(string Name, int Offset)> Fields(ulong vmt)
  {
    var table = _pe.QwordAt((ulong)((long)vmt + VmtFieldTable));
    var o     = _pe.Offset(table);

    if (table == 0 || o < 0)
      yield break;

    int count = _pe.U16(o);
    o += 2 + 8; // Count + ClassTab pointer

    for (var i = 0; i < count; i++)
      o += 6 + _pe.ShortStringSize(o + 6);

    int exCount = _pe.U16(o);
    o += 2;

    for (var i = 0; i < exCount; i++)
    {
      // Flags:Byte, TypeRef:PPTypeInfo, Offset:Cardinal, Name:ShortString, AttrData
      var offset = (int)_pe.U32(o + 9);
      var name   = _pe.ShortString(o + 13);
      yield return (name, offset);

      o += 13 + _pe.ShortStringSize(o + 13);
      o += _pe.U16(o);
    }
  }

  /// <summary>Finds a record type by name and returns its size and field offsets.</summary>
  public (int Size, IReadOnlyDictionary<string, int> Fields)? Record(string name)
  {
    foreach (var o in FindTypeInfo(name, TkRecord))
    {
      var d       = o + 2 + name.Length;
      var size    = (int)_pe.U32(d);
      var managed = (int)_pe.U32(d + 4);
      d += 8 + managed * 16L;
      d += 1 + _pe.Bytes[d] * 8L; // NumOps + RecOps
      var count   = (int)_pe.U32(d);
      d += 4;

      var fields = new Dictionary<string, int>(StringComparer.Ordinal);
      for (var i = 0; i < count; i++)
      {
        // TypeRef:PPTypeInfo, FldOffset:NativeInt, Flags:Byte, Name:ShortString, AttrData
        fields[_pe.ShortString(d + 17)] = (int)_pe.U64(d + 8);
        d += 17 + _pe.ShortStringSize(d + 17);
        d += _pe.U16(d);
      }

      return (size, fields);
    }

    return null;
  }

  private IEnumerable<long> FindTypeInfo(string name, byte kind)
  {
    var needle = new byte[name.Length + 2];
    needle[0] = kind;
    needle[1] = (byte)name.Length;
    Encoding.ASCII.GetBytes(name, 0, name.Length, needle, 2);

    var pos = 0;
    while (true)
    {
      var hit = _pe.Bytes.AsSpan(pos).IndexOf(needle);
      if (hit < 0)
        yield break;

      pos += hit;
      yield return pos;
      pos += needle.Length;
    }
  }

  private DelphiMethod ReadMethodEntry(string className, long entry)
  {
    // Len:Word, CodeAddress:Pointer, Name:ShortString, [Tail]
    var len  = _pe.U16(entry);
    var code = _pe.U64(entry + 2);
    var name = _pe.ShortString(entry + 10);
    var tail = entry + 10 + _pe.ShortStringSize(entry + 10);
    var kind = NativeReturnKind.None;

    // Tail: Version:Byte, CC:Byte, ResultType:PPTypeInfo, ParOff:Word, ParamCount:Byte, Params...
    if (tail + 10 <= entry + len)
      kind = ReturnKindOf(_pe.U64(tail + 2));

    return new DelphiMethod(className, name, code, kind);
  }

  private NativeReturnKind ReturnKindOf(ulong typeRef)
  {
    if (typeRef == 0)
      return NativeReturnKind.None;

    var ti = _pe.Offset(_pe.QwordAt(typeRef));
    if (ti < 0)
      return NativeReturnKind.None;

    var kind = _pe.Bytes[ti];
    switch (kind)
    {
      case TkInteger:
      case TkEnumeration:
        // TTypeData.OrdType: otSByte, otUByte, otSWord, otUWord, otSLong, otULong
        return _pe.Bytes[ti + 1 + _pe.ShortStringSize(ti + 1)] switch
        {
          0 => NativeReturnKind.SByte,
          1 => NativeReturnKind.Byte,
          2 => NativeReturnKind.Int16,
          3 => NativeReturnKind.UInt16,
          4 => NativeReturnKind.Int32,
          _ => NativeReturnKind.UInt32,
        };

      case TkClass:
      case TkPointer:
        return NativeReturnKind.Pointer;

      default:
        // Managed results (strings, records, arrays) are returned through a hidden var parameter.
        return NativeReturnKind.None;
    }
  }

  private void IndexVmts()
  {
    foreach (var s in _pe.Sections)
    {
      if (s.SizeOfRawData < 8)
        continue;

      var start = (long)s.PointerToRawData;
      var end   = start + s.SizeOfRawData - 8;

      for (var o = start; o < end; o += 8)
      {
        var selfVa = _pe.ImageBase + (ulong)(s.VirtualAddress + (o - start));
        if (_pe.U64(o) != selfVa + (ulong)-VmtSelfPtr)
          continue;

        var vmt     = selfVa + (ulong)-VmtSelfPtr;
        var nameOff = _pe.Offset(_pe.QwordAt((ulong)((long)vmt + VmtClassName)));
        if (nameOff < 0 || _pe.Bytes[nameOff] == 0)
          continue;

        var name = _pe.ShortString(nameOff);
        _classByVmt[vmt] = name;

        if (!_vmts.TryGetValue(name, out var list))
          _vmts[name] = list = new List<ulong>();
        list.Add(vmt);
      }
    }
  }
}
