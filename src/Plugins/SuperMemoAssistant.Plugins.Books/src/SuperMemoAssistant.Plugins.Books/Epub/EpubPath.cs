namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Collections.Generic;

  /// <summary>Resolves relative references inside an EPUB package to normalized package paths.</summary>
  public static class EpubPath
  {
    /// <summary>The folder part of a package path, with a trailing slash, or an empty string.</summary>
    public static string Directory(string path)
    {
      var idx = path.LastIndexOf('/');
      return idx < 0 ? string.Empty : path[..(idx + 1)];
    }

    /// <summary>Splits a reference into its path and fragment parts. The query part is dropped.</summary>
    public static (string Path, string? Fragment) Split(string reference)
    {
      string? fragment = null;
      var     hash     = reference.IndexOf('#');

      if (hash >= 0)
      {
        fragment  = reference[(hash + 1)..];
        reference = reference[..hash];
      }

      var query = reference.IndexOf('?');
      if (query >= 0)
        reference = reference[..query];

      return (reference, string.IsNullOrEmpty(fragment) ? null : fragment);
    }

    /// <summary>Whether the reference has a URI scheme (for example http:, mailto:, data:).</summary>
    public static bool HasScheme(string reference)
    {
      var colon = reference.IndexOf(':');
      if (colon <= 0)
        return false;

      var slash = reference.IndexOfAny(['/', '#', '?']);
      return slash < 0 || colon < slash;
    }

    /// <summary>
    ///   Resolves <paramref name="href" /> against the folder <paramref name="baseDirectory" />. The fragment is removed.
    ///   Returns <see langword="null" /> for absolute URIs or references that leave the package.
    /// </summary>
    public static string? Resolve(string baseDirectory, string href)
    {
      var (path, _) = Split(href.Trim());

      if (path.Length == 0 || HasScheme(path))
        return null;

      path = Uri.UnescapeDataString(path).Replace('\\', '/');

      var combined = path.StartsWith('/') ? path[1..] : baseDirectory + path;
      var segments = new List<string>();

      foreach (var segment in combined.Split('/'))
        switch (segment)
        {
          case "":
          case ".":
            continue;

          case "..":
            if (segments.Count == 0)
              return null;

            segments.RemoveAt(segments.Count - 1);
            break;

          default:
            segments.Add(segment);
            break;
        }

      return segments.Count == 0 ? null : string.Join('/', segments);
    }
  }
}
