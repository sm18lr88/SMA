// A SuperMemo registry (a .mem table of 30-byte members and the .rtx text they point into), and the slot-to-file layout of elements.
using System.Buffers.Binary;
using System.Text;

namespace SuperMemoAssistant.Themes.Editing;

internal sealed class TextRegistry
{
  private const int MemberSize = 30;

  private readonly byte[]   _rtx;
  private readonly uint[][] _members;
  private readonly Encoding _ansi;

  public TextRegistry(string folder, string name, Encoding ansi)
  {
    _ansi = ansi;

    var mem = Path.Combine(folder, name + ".mem");
    var rtx = Path.Combine(folder, name + ".rtx");
    var raw = File.Exists(mem) ? File.ReadAllBytes(mem) : [];

    if (raw.Length % MemberSize != 0)
      throw new ThemeException($"{mem} is damaged: its size is not a multiple of {MemberSize} bytes");

    _rtx     = File.Exists(rtx) ? File.ReadAllBytes(rtx) : [];
    _members = Enumerable.Range(0, raw.Length / MemberSize).Select(i => Unpack(raw.AsSpan(i * MemberSize, MemberSize))).ToArray();
  }

  public int Count => _members.Length;

  /// <summary>Fields: use, link, unknown, rtx offset (1-based), rtx length, unknown, slot, unknown.</summary>
  private static uint[] Unpack(ReadOnlySpan<byte> member) =>
  [
    BinaryPrimitives.ReadUInt32LittleEndian(member), BinaryPrimitives.ReadUInt16LittleEndian(member[4..]), BinaryPrimitives.ReadUInt32LittleEndian(member[6..]),
    BinaryPrimitives.ReadUInt32LittleEndian(member[10..]), BinaryPrimitives.ReadUInt32LittleEndian(member[14..]), BinaryPrimitives.ReadUInt32LittleEndian(member[18..]),
    BinaryPrimitives.ReadUInt32LittleEndian(member[22..]), BinaryPrimitives.ReadUInt32LittleEndian(member[26..]),
  ];

  public string Text(long memberId)
  {
    if (memberId is <= 0 || memberId > _members.Length)
      return "";

    var member = _members[memberId - 1];
    var start  = (int)Math.Min(member[3] - 1, (uint)_rtx.Length);
    var length = (int)Math.Min(member[4], (uint)(_rtx.Length - start));
    var raw    = _rtx.AsSpan(start, length);
    var nul    = raw.IndexOf((byte)0);

    raw = nul >= 0 ? raw[..nul] : raw;

    try
    {
      return new UTF8Encoding(false, true).GetString(raw);
    }
    catch (DecoderFallbackException)
    {
      return _ansi.GetString(raw);
    }
  }

  /// <summary>The slot of a member: a file number for HTML, or a compon.dat position for a template. Null when the member does not exist.</summary>
  public long? Slot(long memberId) => memberId > 0 && memberId <= _members.Length ? _members[memberId - 1][6] : null;
}

internal static class ElementFiles
{
  private static readonly long[] Base  = [10, 300, 9000, 270000, 8100000];
  private static readonly long[] Limit = [10, 310, 9310, 279310, 8379310];

  /// <summary>SuperMemo's slot to elements\d\d\slot.ext layout, with a search fallback when that file is missing.</summary>
  public static string For(string collection, long slot, string extension = "HTM")
  {
    var level = 0;

    while (level < Limit.Length && slot > Limit[level])
      level++;

    var dirs = new List<string>();

    if (level > 0)
    {
      var rem = slot - Limit[level - 1];

      for (var j = level; j > 0; j--)
      {
        var digit = (rem - 1) / Base[j - 1] + 1;

        rem -= Base[j - 1] * (digit - 1);
        dirs.Add(digit.ToString(System.Globalization.CultureInfo.InvariantCulture));
      }
    }

    var file = $"{slot}.{extension}";
    var path = Path.Combine([collection, "elements", .. dirs, file]);

    if (File.Exists(path))
      return path;

    var elements = Path.Combine(collection, "elements");
    var found    = Directory.Exists(elements) ? Directory.EnumerateFiles(elements, file, SearchOption.AllDirectories).FirstOrDefault() : null;

    return found ?? path;
  }
}
