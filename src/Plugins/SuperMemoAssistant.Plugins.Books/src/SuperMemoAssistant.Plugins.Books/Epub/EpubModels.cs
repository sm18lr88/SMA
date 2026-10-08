namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Collections.Generic;

  /// <summary>Package metadata read from the OPF file.</summary>
  public sealed record EpubMetadata(
    string                Title,
    IReadOnlyList<string> Creators,
    string?               Language,
    string?               Date,
    string?               Identifier,
    string?               Isbn);

  /// <summary>One spine document (a chapter file) with its title from the table of contents, if any.</summary>
  public sealed record EpubDocument(string Path, string? TocTitle, string Html);

  /// <summary>A binary resource of the package, for example an image.</summary>
  public sealed record EpubResource(string Path, string MediaType, byte[] Data);

  /// <summary>An EPUB 2 or EPUB 3 book: metadata, spine documents in reading order, and image resources.</summary>
  public sealed class EpubBook
  {
    private readonly IReadOnlyDictionary<string, EpubResource> _resources;

    /// <summary>Creates a book.</summary>
    public EpubBook(string                                    version,
                    EpubMetadata                              metadata,
                    IReadOnlyList<EpubDocument>               documents,
                    IReadOnlyDictionary<string, EpubResource> resources)
    {
      Version    = version;
      Metadata   = metadata;
      Documents  = documents;
      _resources = resources;
    }

    /// <summary>The package version attribute, for example "2.0" or "3.0".</summary>
    public string Version { get; }

    /// <summary>The package metadata.</summary>
    public EpubMetadata Metadata { get; }

    /// <summary>Spine documents in reading order. The navigation document is not included.</summary>
    public IReadOnlyList<EpubDocument> Documents { get; }

    /// <summary>Finds a resource by its normalized package path.</summary>
    public EpubResource? FindResource(string path) => _resources.TryGetValue(path, out var res) ? res : null;
  }

  /// <summary>The file is not a readable EPUB. The message is suitable for the user.</summary>
  public sealed class EpubFormatException : Exception
  {
    /// <summary>Creates the exception.</summary>
    public EpubFormatException(string message, Exception? inner = null) : base(message, inner) { }
  }
}
