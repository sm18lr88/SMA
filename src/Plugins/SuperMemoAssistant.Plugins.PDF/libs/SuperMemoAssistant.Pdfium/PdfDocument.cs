namespace SuperMemoAssistant.Pdfium;

using System;
using System.IO;
using System.Runtime.InteropServices;
using Native;

/// <summary>An open PDF document. PDFium reads the source stream on demand, so the stream stays open until disposal.</summary>
public sealed unsafe class PdfDocument : IDisposable
{
  private readonly DocumentSource _source;
  private PdfBookmarkCollection? _bookmarks;

  private PdfDocument(IntPtr handle, DocumentSource source, PdfForms? forms)
  {
    Handle = handle;
    _source = source;
    Pages = new PdfPageCollection(this);
    NamedDestinations = new PdfDestinationCollection(this);
    FormFill = forms;
    forms?.Attach(this);
  }

  public IntPtr Handle { get; private set; }

  public PdfPageCollection Pages { get; }

  public PdfBookmarkCollection Bookmarks => _bookmarks ??= new PdfBookmarkCollection(this, null);

  public PdfDestinationCollection NamedDestinations { get; }

  /// <summary>The interactive form engine of this document, or null when the document was loaded without one.</summary>
  public PdfForms? FormFill { get; }

  public string Title => GetMetaText("Title");

  public string Author => GetMetaText("Author");

  /// <summary>The raw creation date, in the PDF date format (for example, "D:20240131093000+01'00'").</summary>
  public string CreationDate => GetMetaText("CreationDate");

  public static PdfDocument Load(string path, PdfForms? forms = null, string? password = null) =>
    Load(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), forms, password);

  public static PdfDocument Load(byte[] data, PdfForms? forms = null, string? password = null) =>
    Load(new MemoryStream(data, false), forms, password);

  /// <summary>Opens a document from a seekable stream. The document owns the stream and disposes it.</summary>
  public static PdfDocument Load(Stream stream, PdfForms? forms = null, string? password = null)
  {
    ArgumentNullException.ThrowIfNull(stream);
    if (!stream.CanSeek || !stream.CanRead)
      throw new ArgumentException("The stream must support reading and seeking.", nameof(stream));
    if (stream.Length > uint.MaxValue)
      throw new NotSupportedException("PDFium cannot read documents larger than 4 GB.");

    if (forms is { IsAttached: true })
      throw new InvalidOperationException("This form engine already serves another open document.");

    PdfLibrary.Initialize();

    var source = new DocumentSource(stream);
    var handle = NativeMethods.FPDF_LoadCustomDocument(source.Callbacks, password);

    if (handle != IntPtr.Zero)
      return new PdfDocument(handle, source, forms);

    var error = (PdfiumError)NativeMethods.FPDF_GetLastError();
    source.Dispose();

    throw error == PdfiumError.Password ? new InvalidPasswordException() : new PdfiumException(error);
  }

  public void Dispose()
  {
    if (Handle == IntPtr.Zero)
      return;

    Pages.CloseAll();
    FormFill?.Detach(this);
    NativeMethods.FPDF_CloseDocument(Handle);
    Handle = IntPtr.Zero;
    _source.Dispose();
  }

  private string GetMetaText(string tag)
  {
    var length = NativeMethods.FPDF_GetMetaText(Handle, tag, null, 0);
    if (length <= 2)
      return string.Empty;

    var buffer = new char[length / 2];
    fixed (char* text = buffer)
      NativeMethods.FPDF_GetMetaText(Handle, tag, text, length);

    return new string(buffer, 0, buffer.Length - 1);
  }

  /// <summary>Feeds the stream to PDFium through FPDF_FILEACCESS. The native struct must outlive the document.</summary>
  private sealed class DocumentSource : IDisposable
  {
    private readonly Stream _stream;
    private GCHandle _self;

    public DocumentSource(Stream stream)
    {
      _stream = stream;
      _self = GCHandle.Alloc(this);

      Callbacks = (FileAccessCallbacks*)NativeMemory.AllocZeroed((nuint)sizeof(FileAccessCallbacks));
      Callbacks->FileLength = (uint)stream.Length;
      Callbacks->GetBlock = &GetBlock;
      Callbacks->Param = GCHandle.ToIntPtr(_self);
    }

    public FileAccessCallbacks* Callbacks { get; private set; }

    public void Dispose()
    {
      if (Callbacks == null)
        return;

      NativeMemory.Free(Callbacks);
      Callbacks = null;
      _self.Free();
      _stream.Dispose();
    }

    [UnmanagedCallersOnly]
    private static int GetBlock(IntPtr param, uint position, byte* buffer, uint size)
    {
      try
      {
        var source = (DocumentSource)GCHandle.FromIntPtr(param).Target!;
        source._stream.Position = position;
        source._stream.ReadExactly(new Span<byte>(buffer, checked((int)size)));
        return 1;
      }
      catch (Exception ex) when (ex is IOException or ObjectDisposedException or NotSupportedException)
      {
        return 0;
      }
    }
  }
}
