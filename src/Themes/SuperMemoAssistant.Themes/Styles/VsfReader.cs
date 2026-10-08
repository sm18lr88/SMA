// Reads VCL style (.vsf) files: header strings, bitmaps, objects, and the key/value color tail.
using System.Buffers.Binary;

namespace SuperMemoAssistant.Themes.Styles;

internal static partial class VsfFile
{
  public static Vsf Load(byte[] blob)
  {
    var raw  = Inflate(blob);
    var meta = new string[5];
    var p    = 0;

    for (var i = 0; i < 5; i++)
      (meta[i], p) = ReadString(raw, p);

    var pre = Slice(raw, p, 6);

    p += 6;

    var n   = BinaryPrimitives.ReadInt64LittleEndian(raw.AsSpan(p));
    var vsf = new Vsf(meta, pre, Slice(raw, p + 8, (int)n));

    p += 8 + (int)n;

    var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(p));

    p += 4;

    for (var i = 0; i < count; i++)
    {
      string name;

      (name, p) = ReadString(raw, p);

      var w = (int)BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(p));
      var h = (int)BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(p + 4));

      p += 8;
      vsf.Bitmaps.Add(new Bitmap(name, w, h, Slice(raw, p, w * h * 4), Slice(raw, p + w * h * 4, 2)));
      p += w * h * 4 + 2;
    }

    count = (int)BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(p));
    p += 4;

    for (var i = 0; i < count; i++)
    {
      string cls;

      (cls, p) = ReadString(raw, p);

      var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(p));

      vsf.Objects.Add((cls, Slice(raw, p + 4, size)));
      p += 4 + size;
    }

    ReadTail(raw, p, vsf);

    return vsf;
  }

  /// <summary>The tail holds "key : value" string triples. Bytes that do not parse as one are kept as raw runs.</summary>
  private static void ReadTail(byte[] raw, int p, Vsf vsf)
  {
    var run = new List<byte>();

    void FlushRun()
    {
      if (run.Count == 0)
        return;

      vsf.Tail.Add(TailItem.Bytes([.. run]));
      run.Clear();
    }

    while (p < raw.Length)
    {
      if (TryReadTriple(raw, p, out var key, out var value, out var next))
      {
        FlushRun();
        vsf.Tail.Add(TailItem.Pair(key, value));
        p = next;

        continue;
      }

      run.Add(raw[p]);
      p++;
    }

    FlushRun();
  }

  private static bool TryReadTriple(byte[] raw, int p, out string key, out string value, out int next)
  {
    key   = value = string.Empty;
    next  = p;

    try
    {
      (key, var q)       = ReadString(raw, p);
      var (colon, q2)    = ReadString(raw, q);
      (value, var q3)    = ReadString(raw, q2);

      if (colon != ":" || !IsIdentifier(key))
        return false;

      next = q3;

      return true;
    }
    catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
    {
      return false;
    }
  }

  private static bool IsIdentifier(string s) =>
    s.Length > 0 && (char.IsLetter(s[0]) || s[0] == '_') && s.All(c => char.IsLetterOrDigit(c) || c == '_');

  /// <summary>A length-prefixed UTF-16 string. A length past the end reads what is there, as the reference does.</summary>
  private static (string Text, int Next) ReadString(byte[] raw, int p)
  {
    var n = (long)BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(p));
    var take = (int)Math.Max(0, Math.Min(2 * n, raw.Length - (p + 4)));

    return (Utf16.GetString(raw, p + 4, take), (int)Math.Min(int.MaxValue, p + 4 + 2 * n));
  }

  private static byte[] Slice(byte[] raw, int start, int length)
  {
    if (start >= raw.Length || length <= 0)
      return [];

    return raw.AsSpan(start, Math.Min(length, raw.Length - start)).ToArray();
  }
}
