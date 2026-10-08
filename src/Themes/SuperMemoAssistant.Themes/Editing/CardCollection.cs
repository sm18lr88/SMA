// Reads a SuperMemo 18-20 collection from disk: elements, templates, components, and the HTML files they show. Read-only.
using System.Buffers.Binary;
using System.Text;

namespace SuperMemoAssistant.Themes.Editing;

internal sealed class CardCollection
{
  private const int  ElementRecord  = 118;
  private const ushort Signature    = 0xD431;

  private static readonly Dictionary<int, string> ElementKinds = new() { [0] = "topic", [1] = "item", [2] = "task", [3] = "template", [4] = "concept" };

  private static readonly Dictionary<int, string> ComponentKinds = new()
  {
    [0x00] = "text", [0x01] = "spelling", [0x02] = "image", [0x03] = "sound", [0x04] = "video", [0x05] = "ellipse",
    [0x06] = "rectangle", [0x07] = "rounded-rect", [0x0C] = "rtf", [0x0D] = "html", [0x10] = "webview",
  };

  private static readonly HashSet<string> TextRegistryKinds = ["html", "webview", "text", "spelling"];

  private readonly TextRegistry _texts;

  private CardCollection(string path, byte[] compon, TextRegistry texts, Encoding ansi)
  {
    Path   = path;
    Compon = compon;
    _texts = texts;

    Elements  = ReadElements();
    Templates = ReadTemplates(ansi);
  }

  public string Path { get; }

  public string ComponPath => System.IO.Path.Combine(Path, "info", "compon.dat");

  /// <summary>The bytes of compon.dat as read. Edits are made on a copy.</summary>
  public byte[] Compon { get; }

  public IReadOnlyList<CardRecord> Elements { get; }

  public IReadOnlyList<CardRecord> Templates { get; }

  public static CardCollection Open(string path, Encoding ansi)
  {
    var compon = System.IO.Path.Combine(path, "info", "compon.dat");

    if (!File.Exists(compon))
      throw new ThemeException($"{path} is not a SuperMemo collection (no info/compon.dat)");

    return new CardCollection(path, File.ReadAllBytes(compon), new TextRegistry(System.IO.Path.Combine(path, "registry"), "text", ansi), ansi);
  }

  public string TemplateName(int templateId) => Templates.FirstOrDefault(t => t.Number == templateId)?.Title ?? "";

  private CardRecord ReadRecord(int number, string kind, string title, int position)
  {
    var record = new CardRecord(number, kind, title, position);

    if (position < 0)
      return record;

    if (position + 11 > Compon.Length)
      throw new ThemeException($"compon.dat is too short for {kind} {number} (record at {position})");

    var span = Compon.AsSpan();

    if (BinaryPrimitives.ReadUInt16LittleEndian(span[position..]) != Signature)
      throw new ThemeException($"compon.dat: bad record signature at {position} for {kind} {number}");

    var length    = BinaryPrimitives.ReadUInt16LittleEndian(span[(position + 2)..]);
    var count     = span[position + 8];
    var headerLen = BinaryPrimitives.ReadUInt16LittleEndian(span[(position + 9)..]);

    record.End   = position + 4 + length;
    record.Color = ReadUInt(position + 11, kind, number);

    var p = position + 11 + headerLen;

    for (var i = 1; i <= count; i++)
    {
      Require(p + 3, kind, number);

      var code = span[p];
      var size = BinaryPrimitives.ReadUInt16LittleEndian(span[(p + 1)..]);

      Require(p + 3 + size, kind, number);

      var body   = span.Slice(p + 3, size);
      var ckind  = ComponentKinds.TryGetValue(code, out var known) ? known : $"type0x{code:x2}";
      var reg    = size >= 21 ? BinaryPrimitives.ReadUInt32LittleEndian(body[17..]) : (uint?)null;
      var color  = ckind == "text" ? ReadFrom(body, 22, kind, number) : (uint?)null;
      string? file   = null;
      string? inline = null;

      if (TextRegistryKinds.Contains(ckind) && reg is { } id and not 0)
      {
        if (_texts.Slot(id) is { } slot && slot != 0 && ckind is "html" or "webview")
          file = ElementFiles.For(Path, slot);
        else
          inline = _texts.Text(id);
      }

      record.Components.Add(new CardComponent(i, ckind, p + 3, size > 8 ? body[8] : 0, reg, color, size, file, inline));
      p += 3 + size;
    }

    return record;
  }

  private void Require(int end, string kind, int number)
  {
    if (end > Compon.Length)
      throw new ThemeException($"compon.dat is too short for {kind} {number}");
  }

  private uint ReadUInt(int at, string kind, int number)
  {
    Require(at + 4, kind, number);

    return BinaryPrimitives.ReadUInt32LittleEndian(Compon.AsSpan(at));
  }

  private static uint ReadFrom(ReadOnlySpan<byte> body, int at, string kind, int number) =>
    at + 4 <= body.Length ? BinaryPrimitives.ReadUInt32LittleEndian(body[at..]) : throw new ThemeException($"a text component of {kind} {number} is too short");

  private List<CardRecord> ReadElements()
  {
    var data   = File.ReadAllBytes(System.IO.Path.Combine(Path, "info", "ElementInfo.dat"));
    var output = new List<CardRecord>();

    for (var i = 0; i < data.Length / ElementRecord; i++)
    {
      var row     = data.AsSpan(i * ElementRecord, ElementRecord);
      var kind    = row[0];
      var titleId = BinaryPrimitives.ReadInt32LittleEndian(row[2..]);
      var pos     = BinaryPrimitives.ReadInt32LittleEndian(row[6..]);

      if (titleId < 0)
        continue; // a deleted or empty slot

      var record = ReadRecord(i + 1, ElementKinds.TryGetValue(kind, out var name) ? name : $"kind{kind}", _texts.Text(titleId), pos);

      record.TemplateId = BinaryPrimitives.ReadInt32LittleEndian(row[61..]);
      output.Add(record);
    }

    return output;
  }

  private List<CardRecord> ReadTemplates(Encoding ansi)
  {
    var registry = new TextRegistry(System.IO.Path.Combine(Path, "registry"), "template", ansi);
    var output   = new List<CardRecord>();

    for (var id = 1; id <= registry.Count; id++)
    {
      if (registry.Slot(id) is { } pos && pos + 4 <= Compon.Length)
        output.Add(ReadRecord(id, "template", registry.Text(id), (int)pos));
    }

    return output;
  }
}
