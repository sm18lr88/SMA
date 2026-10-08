using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>A local image to copy next to the document, and its path relative to the document.</summary>
  public sealed record ImageCopy(string SourcePath, string RelativePath);

  /// <summary>
  ///   Maps local image sources (absolute paths or file URIs) to unique files in the image folder of the document.
  ///   Web, data, and relative sources stay unchanged.
  /// </summary>
  public sealed class ImageLinker(string folderName)
  {
    private readonly Dictionary<string, string> _bySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ImageCopy>            _copies   = [];
    private readonly HashSet<string>            _names    = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The folder name for images: "&lt;document name&gt;_files".</summary>
    public static string FolderNameFor(string documentPath)
    {
      return Path.GetFileNameWithoutExtension(documentPath) + "_files";
    }

    /// <summary>The images to copy, in the order of their first use.</summary>
    public IReadOnlyList<ImageCopy> Copies => _copies;

    /// <summary>Returns the source to write for an image source found in the content.</summary>
    public string Link(string source)
    {
      if (TryGetLocalPath(source, out var path) == false)
        return source;

      if (_bySource.TryGetValue(path, out var link))
        return link;

      var fileName = UniqueName(Path.GetFileName(path));
      link = folderName + "/" + Uri.EscapeDataString(fileName);

      _bySource[path] = link;
      _copies.Add(new ImageCopy(path, Path.Combine(folderName, fileName)));

      return link;
    }

    private static bool TryGetLocalPath(string source, out string path)
    {
      path = string.Empty;

      if (Uri.TryCreate(source.Trim(), UriKind.Absolute, out var uri) == false || uri.IsFile == false)
        return false;

      path = uri.LocalPath;

      return Path.GetFileName(path).Length > 0;
    }

    private string UniqueName(string fileName)
    {
      var stem      = Path.GetFileNameWithoutExtension(fileName);
      var extension = Path.GetExtension(fileName);

      var candidate = Enumerable.Range(1, int.MaxValue)
                                .Select(i => i == 1 ? fileName : $"{stem}-{i}{extension}")
                                .First(name => _names.Contains(name) == false);

      _names.Add(candidate);

      return candidate;
    }
  }
}
