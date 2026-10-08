// Model of a VCL style (.vsf) file: header strings, bitmaps, objects and the style color tail.
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace SuperMemoAssistant.Themes.Styles;

internal sealed record Bitmap(string Name, int Width, int Height, byte[] Pixels, byte[] Trailer);

/// <summary>One item after the object list: either a "key : value" style color or raw bytes the parser did not understand.</summary>
internal sealed record TailItem(string? Key, string? Value, byte[]? Raw)
{
  public bool IsPair => Key is not null;

  public static TailItem Pair(string key, string value) => new(key, value, null);

  public static TailItem Bytes(byte[] raw) => new(null, null, raw);
}

internal sealed class Vsf
{
  public Vsf(string[] meta, byte[] pre, byte[] tag)
  {
    Meta = meta;
    Pre  = pre;
    Tag  = tag;
  }

  /// <summary>Name, version, author, author e-mail, url.</summary>
  public string[] Meta { get; }

  public byte[] Pre { get; }

  public byte[] Tag { get; }

  public List<Bitmap> Bitmaps { get; } = [];

  public List<(string Class, byte[] Data)> Objects { get; } = [];

  public List<TailItem> Tail { get; } = [];

  public string Name => Meta[0];
}
