namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.IO.Compression;
  using System.Linq;
  using System.Text;
  using System.Xml;
  using System.Xml.Linq;

  /// <summary>Reads entries of an EPUB ZIP archive. XML is read without DTD processing or external resolution.</summary>
  internal sealed class EpubArchive
  {
    /// <summary>Entries larger than this are refused, which protects against ZIP bombs.</summary>
    private const long MaxEntryBytes = 64L * 1024 * 1024;

    private static readonly XmlReaderSettings XmlSettings = new()
    {
      DtdProcessing = DtdProcessing.Ignore,
      XmlResolver   = null,
    };

    private readonly Dictionary<string, ZipArchiveEntry> _exact;
    private readonly Dictionary<string, ZipArchiveEntry> _ignoreCase;

    public EpubArchive(ZipArchive zip)
    {
      var files = zip.Entries.Where(e => e.Name.Length > 0).ToList();

      _exact      = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
      _ignoreCase = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);

      foreach (var entry in files)
      {
        var name = entry.FullName.Replace('\\', '/');
        _exact.TryAdd(name, entry);
        _ignoreCase.TryAdd(name, entry);
      }
    }

    public byte[]? ReadBytes(string path)
    {
      if ((_exact.TryGetValue(path, out var entry) || _ignoreCase.TryGetValue(path, out entry)) == false)
        return null;

      if (entry.Length > MaxEntryBytes)
        throw new EpubFormatException($"The EPUB entry '{path}' is too large ({entry.Length / (1024 * 1024)} MB).");

      using var input  = entry.Open();
      using var output = new MemoryStream((int)entry.Length);
      input.CopyTo(output);

      return output.ToArray();
    }

    public string? ReadText(string path)
    {
      var data = ReadBytes(path);
      if (data == null)
        return null;

      using var reader = new StreamReader(new MemoryStream(data), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      return reader.ReadToEnd();
    }

    public XDocument? ReadXml(string path)
    {
      var data = ReadBytes(path);
      if (data == null)
        return null;

      using var reader = XmlReader.Create(new MemoryStream(data), XmlSettings);
      return XDocument.Load(reader);
    }
  }
}
