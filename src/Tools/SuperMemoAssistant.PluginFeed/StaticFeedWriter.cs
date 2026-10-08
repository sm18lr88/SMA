namespace SuperMemoAssistant.PluginFeed;

using System.Text.Json;
using System.Text.Json.Nodes;
using NuGet.Packaging;
using NuGet.Versioning;

/// <summary>
///   Writes a NuGet v3 feed and the SMA plugin catalog as plain files. A static host such as GitHub Pages ignores query
///   strings, so the search resource is one file that lists every package; SMA filters the result with the catalog.
/// </summary>
public static class StaticFeedWriter
{
  /// <summary>Catalog that UpdateCfg.PluginsUpdateUrl points to, relative to the feed root.</summary>
  public const string CatalogFile = "plugins.json";

  /// <summary>NuGet service index that UpdateCfg.PluginsUpdateNuGetUrls points to, relative to the feed root.</summary>
  public const string ServiceIndexFile = "nuget/index.json";

  private const string FlatContainerDir = "nuget/flatcontainer/";
  private const string RegistrationDir  = "nuget/registration/";
  private const string SearchFile       = "nuget/search/query.json";

  /// <param name="packagePaths">Plugin packages; each must pass <see cref="PluginPackageValidator" />.</param>
  /// <param name="catalog">Catalog entries; every entry needs a package and every package needs an entry.</param>
  /// <param name="baseUrl">Public URL of <paramref name="outputDir" />. NuGet requires absolute resource URLs.</param>
  /// <param name="outputDir">Empty or missing folder that receives the feed.</param>
  /// <param name="timestamp">Publication time written to the feed and to catalog entries without UpdatedAt.</param>
  public static void Write(IEnumerable<string> packagePaths, IEnumerable<CatalogEntry> catalog, Uri baseUrl, string outputDir, DateTime timestamp)
  {
    var root = baseUrl.AbsoluteUri.EndsWith('/') ? baseUrl.AbsoluteUri : baseUrl.AbsoluteUri + "/";

    if (Directory.Exists(outputDir) && Directory.EnumerateFileSystemEntries(outputDir).Any())
      throw new IOException($"{outputDir} is not empty");

    var packages = packagePaths.Select(p => (Path: p, Identity: PluginPackageValidator.Validate(p))).ToList();
    var entries  = catalog.ToList();

    foreach (var entry in entries.Where(e => packages.All(p => !p.Identity.Id.Equals(e.PackageName, StringComparison.OrdinalIgnoreCase))))
      throw new InvalidDataException($"Catalog entry {entry.PackageName} has no package");

    foreach (var package in packages)
      FeedCatalog.Find(entries, package.Identity.Id);

    var searchData = new JsonArray();

    foreach (var group in packages.GroupBy(p => p.Identity.Id.ToLowerInvariant()))
    {
      var id       = group.Key;
      var versions = group.OrderBy(p => p.Identity.Version).ToList();
      var latest   = versions[^1];
      var regIndex = $"{root}{RegistrationDir}{id}/index.json";
      var leaves   = new JsonArray();
      var search   = new JsonArray();

      using var latestReader = new PackageArchiveReader(latest.Path);
      var       nuspec       = latestReader.NuspecReader;

      foreach (var (path, identity) in versions)
      {
        var version  = identity.Version.ToNormalizedString();
        var lowerVer = version.ToLowerInvariant();
        var content  = $"{root}{FlatContainerDir}{id}/{lowerVer}/{id}.{lowerVer}.nupkg";
        var leafUrl  = $"{root}{RegistrationDir}{id}/{lowerVer}.json";

        File.Copy(path, OutputFile(outputDir, $"{FlatContainerDir}{id}/{lowerVer}/{id}.{lowerVer}.nupkg"));

        using (var reader = new PackageArchiveReader(path))
        using (var nuspecStream = reader.GetNuspec())
        using (var target = File.Create(OutputFile(outputDir, $"{FlatContainerDir}{id}/{lowerVer}/{id}.nuspec")))
          nuspecStream.CopyTo(target);

        leaves.Add(new JsonObject
        {
          ["@id"] = leafUrl,
          ["@type"] = "Package",
          ["catalogEntry"] = new JsonObject
          {
            ["@id"]          = leafUrl + "#catalogEntry",
            ["@type"]        = "PackageDetails",
            ["id"]           = identity.Id,
            ["version"]      = version,
            ["listed"]       = true,
            ["published"]    = timestamp.ToString("O"),
            ["description"]  = nuspec.GetDescription(),
            ["authors"]      = nuspec.GetAuthors(),
            ["title"]        = nuspec.GetTitle(),
            ["packageContent"] = content,
          },
          ["packageContent"] = content,
          ["registration"]   = regIndex,
        });

        search.Add(new JsonObject { ["version"] = version, ["downloads"] = 0, ["@id"] = leafUrl });
      }

      WriteJson(outputDir, $"{FlatContainerDir}{id}/index.json", new JsonObject
      {
        ["versions"] = new JsonArray(versions.Select(v => (JsonNode)v.Identity.Version.ToNormalizedString().ToLowerInvariant()).ToArray()),
      });

      WriteJson(outputDir, $"{RegistrationDir}{id}/index.json", new JsonObject
      {
        ["@id"]   = regIndex,
        ["count"] = 1,
        ["items"] = new JsonArray(new JsonObject
        {
          ["@id"]   = $"{regIndex}#page/{versions[0].Identity.Version.ToNormalizedString()}/{latest.Identity.Version.ToNormalizedString()}",
          ["count"] = versions.Count,
          ["lower"] = versions[0].Identity.Version.ToNormalizedString(),
          ["upper"] = latest.Identity.Version.ToNormalizedString(),
          ["items"] = leaves,
        }),
      });

      searchData.Add(new JsonObject
      {
        ["@id"]            = regIndex,
        ["@type"]          = "Package",
        ["registration"]   = regIndex,
        ["id"]             = latest.Identity.Id,
        ["version"]        = latest.Identity.Version.ToNormalizedString(),
        ["description"]    = nuspec.GetDescription(),
        ["title"]          = nuspec.GetTitle(),
        ["authors"]        = new JsonArray(nuspec.GetAuthors().Split(',').Select(a => (JsonNode)a.Trim()).ToArray()),
        ["totalDownloads"] = 0,
        ["versions"]       = search,
      });
    }

    WriteJson(outputDir, SearchFile, new JsonObject { ["totalHits"] = searchData.Count, ["data"] = searchData });
    WriteJson(outputDir, ServiceIndexFile, ServiceIndex(root));
    WriteCatalog(outputDir, entries, timestamp);
  }

  private static JsonObject ServiceIndex(string root)
  {
    (string Path, string Type)[] resources =
    [
      (FlatContainerDir, "PackageBaseAddress/3.0.0"),
      (RegistrationDir, "RegistrationsBaseUrl/3.6.0"),
      (RegistrationDir, "RegistrationsBaseUrl"),
      (SearchFile, "SearchQueryService/3.4.0"),
      (SearchFile, "SearchQueryService/3.0.0-beta"),
    ];

    return new JsonObject
    {
      ["version"]   = "3.0.0",
      ["resources"] = new JsonArray(resources.Select(r => (JsonNode)new JsonObject { ["@id"] = root + r.Path, ["@type"] = r.Type }).ToArray()),
    };
  }

  private static void WriteCatalog(string outputDir, IEnumerable<CatalogEntry> entries, DateTime timestamp)
  {
    var published = entries.Select(e => new CatalogEntry
    {
      PackageName = e.PackageName,
      DisplayName = e.DisplayName,
      Description = e.Description,
      Author      = e.Author,
      IconBase64  = e.IconBase64,
      Labels      = e.Labels,
      Rating      = e.Rating,
      UpdatedAt   = e.UpdatedAt ?? timestamp,
    });

    File.WriteAllText(OutputFile(outputDir, CatalogFile), JsonSerializer.Serialize(published, FeedCatalog.JsonOptions));
  }

  private static void WriteJson(string outputDir, string relativePath, JsonNode node) =>
    File.WriteAllText(OutputFile(outputDir, relativePath), node.ToJsonString(FeedCatalog.JsonOptions));

  private static string OutputFile(string outputDir, string relativePath)
  {
    var path = Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    return path;
  }
}
