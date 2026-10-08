using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SuperMemoAssistant.Plugins.Writing.Export
{
  /// <summary>The outcome of writing a document: copied images, and the image files that did not exist.</summary>
  public sealed record WriteResult(int ImagesCopied, IReadOnlyList<string> MissingImages);

  /// <summary>Compiles, renders, and writes a branch, then copies its local images next to the output file.</summary>
  public static class DocumentWriter
  {
    /// <summary>Compiles the branch under <paramref name="rootId" /> and writes it to <paramref name="outputPath" />.</summary>
    public static (BranchDocument Document, WriteResult Result) CompileToFile(
      ITreeSource                 source,
      int                         rootId,
      CompileOptions              options,
      string                      outputPath,
      IProgress<CompileProgress>? progress,
      CancellationToken           cancellationToken)
    {
      var document = new BranchCompiler(source).Compile(rootId, options, progress, cancellationToken);
      var images   = new ImageLinker(ImageLinker.FolderNameFor(outputPath));
      var text     = DocumentRenderer.For(options.Format).Render(document, options.IncludeTitlePage, images);

      cancellationToken.ThrowIfCancellationRequested();

      return (document, Write(outputPath, text, images.Copies));
    }

    /// <summary>Writes UTF-8 text (without a byte order mark) and copies the images. Existing files are replaced.</summary>
    public static WriteResult Write(string outputPath, string text, IReadOnlyList<ImageCopy> images)
    {
      var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath))
        ?? throw new ArgumentException($"The output path \"{outputPath}\" has no folder.", nameof(outputPath));

      File.WriteAllText(outputPath, text, new UTF8Encoding(false));

      var missing = new List<string>();

      foreach (var image in images)
      {
        if (File.Exists(image.SourcePath) == false)
        {
          missing.Add(image.SourcePath);
          continue;
        }

        Directory.CreateDirectory(Path.Combine(directory, Path.GetDirectoryName(image.RelativePath) ?? string.Empty));
        File.Copy(image.SourcePath, Path.Combine(directory, image.RelativePath), true);
      }

      return new WriteResult(images.Count - missing.Count, missing);
    }
  }
}
