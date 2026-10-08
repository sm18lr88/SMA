// Reads the binary Delphi form format (TPF0) into an object tree that remembers where each property value sits.
using System.Buffers.Binary;
using System.Text;

namespace SuperMemoAssistant.Themes.Forms;

internal sealed class DfmObject
{
  public DfmObject(string cls, string name, int propsEnd)
  {
    Class    = cls;
    Name     = name;
    PropsEnd = propsEnd;
  }

  public string Class { get; }

  public string Name { get; }

  /// <summary>Offset of the 0x00 that ends the property list of this object.</summary>
  public int PropsEnd { get; }

  /// <summary>Property name to the byte span of its value.</summary>
  public Dictionary<string, (int Start, int End)> Spans { get; } = [];

  public Dictionary<string, object?> Values { get; } = [];

  public List<DfmObject> Kids { get; } = [];

  public DfmObject? Find(string name)
  {
    if (Name == name)
      return this;

    foreach (var kid in Kids)
    {
      if (kid.Find(name) is { } hit)
        return hit;
    }

    return null;
  }
}

internal static class DfmReader
{
  public static DfmObject Read(byte[] form) => new Reader(form).Object();

  private sealed class Reader(byte[] data)
  {
    private int _p = 4; // skip "TPF0"

    private byte U8() => data[_p++];

    private byte[] Take(int n)
    {
      var chunk = data.AsSpan(_p, n).ToArray();

      _p += n;

      return chunk;
    }

    private string Short() => Encoding.Latin1.GetString(Take(U8()));

    private uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));

    public object? Value()
    {
      var t = U8();

      switch (t)
      {
        case 2:
          return (long)(sbyte)Take(1)[0];
        case 3:
          return (long)BinaryPrimitives.ReadInt16LittleEndian(Take(2));
        case 4:
          return (long)BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        case 5:
          return Take(10);
        case 15:
          return Take(4);
        case 17 or 19 or 21:
          return Take(8);
        case 6 or 7:
          return Short();
        case 0 or 13:
          return null;
        case 8:
          return false;
        case 9:
          return true;
        case 10 or 12 or 20:
          return Take((int)U32());
        case 18:
          return Encoding.Unicode.GetString(Take(2 * (int)U32()));
        case 1:
          return ReadList();
        case 11:
          return ReadSet();
        case 14:
          SkipCollection();

          return null;
        default:
          throw new InvalidDataException($"unsupported DFM value type {t} at {_p - 1}");
      }
    }

    private List<object?> ReadList()
    {
      var items = new List<object?>();

      while (data[_p] != 0)
        items.Add(Value());

      _p++;

      return items;
    }

    private HashSet<string> ReadSet()
    {
      var names = new HashSet<string>();

      while (Short() is { Length: > 0 } name)
        names.Add(name);

      return names;
    }

    private void SkipCollection()
    {
      while (data[_p] != 0)
      {
        if (data[_p] is 2 or 3 or 4)
          Value();

        while (Short().Length > 0)
          Value();
      }

      _p++;
    }

    public DfmObject Object()
    {
      if ((data[_p] & 0xF0) == 0xF0)
      {
        var flags = U8();

        if ((flags & 2) != 0)
          Value();
      }

      var cls  = Short();
      var name = Short();
      var spans  = new Dictionary<string, (int, int)>();
      var values = new Dictionary<string, object?>();
      int propsEnd;

      while (true)
      {
        var start = _p;
        var prop  = Short();

        if (prop.Length == 0)
        {
          propsEnd = start;

          break;
        }

        var valueStart = _p;

        values[prop] = Value();
        spans[prop]  = (valueStart, _p);
      }

      var node = new DfmObject(cls, name, propsEnd);

      foreach (var (k, v) in spans)
        node.Spans[k] = v;

      foreach (var (k, v) in values)
        node.Values[k] = v;

      while (data[_p] != 0)
        node.Kids.Add(Object());

      _p++;

      return node;
    }
  }
}
