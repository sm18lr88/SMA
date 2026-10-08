namespace SuperMemoAssistant.Plugins.Books
{
  using System;
  using System.Collections.Generic;

  /// <summary>
  ///   Per-collection plugin state: the hashes of the Kindle clippings that were imported into this collection. A
  ///   re-import of the same clippings file skips them.
  /// </summary>
  public class BooksCollectionCfg
  {
    /// <summary>Hashes from <see cref="Kindle.HighlightHasher" />.</summary>
    public HashSet<string> ImportedHighlightHashes { get; set; } = new(StringComparer.Ordinal);
  }
}
