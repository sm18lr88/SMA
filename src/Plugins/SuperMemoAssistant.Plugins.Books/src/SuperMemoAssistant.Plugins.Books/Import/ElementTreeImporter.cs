namespace SuperMemoAssistant.Plugins.Books.Import
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Threading;
  using Anotar.Serilog;
  using Planning;
  using SuperMemoAssistant.Extensions;
  using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
  using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
  using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
  using SuperMemoAssistant.Services;

  /// <summary>The result of an import.</summary>
  /// <param name="Created">The number of created elements.</param>
  /// <param name="Planned">The number of planned elements.</param>
  /// <param name="Error">A user-facing error, or <see langword="null" />.</param>
  /// <param name="Cancelled">Whether the user cancelled the import.</param>
  /// <param name="CreatedHashes">Kindle clipping hashes of the created elements.</param>
  public sealed record ImportOutcome(int Created, int Planned, string? Error, bool Cancelled, IReadOnlyList<string> CreatedHashes)
  {
    /// <summary>A summary for the user, which always states how many elements were created.</summary>
    public string Summary =>
      Error != null
        ? string.Create(CultureInfo.InvariantCulture, $"The import stopped: {Error} {Created} of {Planned} elements were created.")
        : Cancelled
          ? string.Create(CultureInfo.InvariantCulture, $"The import was cancelled. {Created} of {Planned} elements were created.")
          : string.Create(CultureInfo.InvariantCulture, $"The import is complete. {Created} elements were created.");
  }

  /// <summary>
  ///   Creates a planned tree in SuperMemo. This is the only class that creates elements. A parent is created before
  ///   its children, and siblings are created in order, in small batches so that the user can cancel.
  /// </summary>
  internal static class ElementTreeImporter
  {
    private const int BatchSize = 20;

    /// <summary>Creates the trees under <paramref name="parentId" />. Call this method from a background thread.</summary>
    public static ImportOutcome Import(IReadOnlyList<ImportNode> roots, int parentId, IProgress<int> progress, CancellationToken ct)
    {
      var planned = roots.Sum(r => r.Count);
      var created = 0;
      var hashes  = new List<string>();
      var queue   = new Queue<(int ParentId, IReadOnlyList<ImportNode> Siblings)>();

      ImportOutcome Stop(string? error) => new(created, planned, error, error == null, hashes);

      var parent = Svc.SM.Registry.Element[parentId];
      if (parent == null)
        return Stop("The parent element does not exist.");

      var limit = Svc.SM.UI.ElementWdw.LimitChildrenCount;
      if (parent.ChildrenCount + roots.Count > limit)
        return Stop(string.Create(CultureInfo.InvariantCulture,
                                  $"The parent element has {parent.ChildrenCount} children, and SuperMemo allows {limit}. Choose another parent."));

      queue.Enqueue((parentId, roots));

      try
      {
        while (queue.TryDequeue(out var group))
          foreach (var batch in group.Siblings.Chunk(BatchSize))
          {
            if (ct.IsCancellationRequested)
              return Stop(null);

            var builders = batch.Select(n => ToBuilder(n, group.ParentId)).ToArray();
            Svc.SM.Registry.Element.Add(out var results, ElemCreationFlags.None, builders);

            for (var i = 0; i < batch.Length; i++)
            {
              var result = results?.ElementAtOrDefault(i);
              if (result is not { Success: true, ElementId: > 0 })
                return Stop(results?.GetErrorString() ?? $"SuperMemo did not create the element \"{batch[i].Title}\".");

              created++;
              hashes.AddRange(batch[i].HighlightHashes);

              if (batch[i].Children.Count > 0)
                queue.Enqueue((result.ElementId, batch[i].Children));
            }

            progress.Report(created);
          }
      }
      catch (Exception ex) when (ex is not OutOfMemoryException)
      {
        LogTo.Error(ex, "Books: element creation failed after {Created} of {Planned} elements", created, planned);
        return Stop($"SuperMemo reported an error: {ex.Message}");
      }

      return new ImportOutcome(created, planned, null, false, hashes);
    }

    private static ElementBuilder ToBuilder(ImportNode node, int parentId)
    {
      var refs = node.References;

      return new ElementBuilder(ElementType.Topic, new TextContent(true, node.Html))
             .WithParent(parentId)
             .WithTitle(node.Title)
             .WithPriority(node.Priority)
             .WithReference(r => r.WithTitle(refs.Title)
                                  .WithAuthor(refs.Author)
                                  .WithSource(refs.Source)
                                  .WithDate(EscapeFormat(refs.Date)))
             .DoNotDisplay();
    }

    /// <summary>References format dates with string.Format, so braces in a date text must be escaped.</summary>
    private static string? EscapeFormat(string? text) =>
      text?.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
  }
}
