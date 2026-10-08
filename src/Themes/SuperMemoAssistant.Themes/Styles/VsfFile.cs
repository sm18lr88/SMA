// Writes VCL style (.vsf) files ("VCL_STYLE 2.0" plus a zlib stream) and compares and names style blobs.
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace SuperMemoAssistant.Themes.Styles;

internal static partial class VsfFile
{
  private static readonly byte[] Magic = "VCL_STYLE 2.0"u8.ToArray();

  private static readonly UTF8Encoding Utf8  = new(false, true);
  private static readonly UnicodeEncoding Utf16 = new(false, false, true);

  public static bool HasMagic(ReadOnlySpan<byte> blob) => blob.StartsWith(Magic);

  public static byte[] Inflate(ReadOnlySpan<byte> blob, int limit = int.MaxValue)
  {
    if (!HasMagic(blob))
      throw new InvalidDataException("not a VCL style (missing VCL_STYLE 2.0 header)");

    using var input  = new MemoryStream(blob[Magic.Length..].ToArray());
    using var zlib   = new ZLibStream(input, CompressionMode.Decompress);
    using var output = new MemoryStream();
    var       buffer = new byte[Math.Min(81920, limit)];
    int       read;

    while (output.Length < limit && (read = zlib.Read(buffer, 0, (int)Math.Min(buffer.Length, limit - output.Length))) > 0)
      output.Write(buffer, 0, read);

    return output.ToArray();
  }

  /// <summary>The style name stored at the start of the data (SuperMemo selects styles by this name).</summary>
  public static string StyleName(byte[] blob)
  {
    var head = Inflate(blob, 512);
    var n    = (int)BinaryPrimitives.ReadUInt32LittleEndian(head);

    return Utf16.GetString(head, 4, 2 * n);
  }

  private static void WriteString(MemoryStream ms, string s)
  {
    Span<byte> len = stackalloc byte[4];

    BinaryPrimitives.WriteUInt32LittleEndian(len, (uint)s.Length);
    ms.Write(len);
    ms.Write(Utf16.GetBytes(s));
  }

  public static byte[] DumpRaw(Vsf v)
  {
    using var ms = new MemoryStream();

    foreach (var m in v.Meta)
      WriteString(ms, m);

    ms.Write(v.Pre);

    Span<byte> eight = stackalloc byte[8];

    BinaryPrimitives.WriteInt64LittleEndian(eight, v.Tag.Length);
    ms.Write(eight);
    ms.Write(v.Tag);
    WriteUInt(ms, v.Bitmaps.Count);

    foreach (var b in v.Bitmaps)
    {
      WriteString(ms, b.Name);
      WriteUInt(ms, b.Width);
      WriteUInt(ms, b.Height);
      ms.Write(b.Pixels);
      ms.Write(b.Trailer);
    }

    WriteUInt(ms, v.Objects.Count);

    foreach (var (cls, data) in v.Objects)
    {
      WriteString(ms, cls);
      WriteUInt(ms, data.Length);
      ms.Write(data);
    }

    foreach (var item in v.Tail)
    {
      if (item.IsPair)
      {
        WriteString(ms, item.Key!);
        WriteString(ms, ":");
        WriteString(ms, item.Value!);
      }
      else
      {
        ms.Write(item.Raw!);
      }
    }

    return ms.ToArray();
  }

  private static void WriteUInt(MemoryStream ms, int value)
  {
    Span<byte> four = stackalloc byte[4];

    BinaryPrimitives.WriteUInt32LittleEndian(four, (uint)value);
    ms.Write(four);
  }

  /// <summary>Magic plus the zlib stream (best compression).</summary>
  public static byte[] Dump(Vsf v)
  {
    using var output = new MemoryStream();

    output.Write(Magic);

    using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
      zlib.Write(DumpRaw(v));

    return output.ToArray();
  }

  /// <summary>True when two style blobs hold the same data. Compressors differ between zlib builds, so compare the inflated bytes.</summary>
  public static bool SameContent(byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b) || Inflate(a).AsSpan().SequenceEqual(Inflate(b));
}
