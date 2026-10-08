namespace SuperMemoAssistant.PluginFeed;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   One plugin of the catalog. The public properties mirror SuperMemoAssistant.Plugins.Models.PluginMetadata, because SMA
///   deserializes the published plugins.json into that type (PluginRepositoryService).
/// </summary>
public sealed class CatalogEntry
{
  public string                PackageName { get; set; } = "";
  public string                DisplayName { get; set; } = "";
  public string                Description { get; set; } = "";
  public string                Author      { get; set; } = "";
  public string?               IconBase64  { get; set; }
  public IReadOnlyList<string> Labels      { get; set; } = [];
  public int                   Rating      { get; set; }
  public DateTime?             UpdatedAt   { get; set; }

  /// <summary>Repository-relative project to publish. Only build\pack-plugins.ps1 reads it; it is not published.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Project { get; set; }

  /// <summary>Download URL of a prebuilt third-party .nupkg. Only build\pack-plugins.ps1 reads it; it is not published.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? PackageUrl { get; set; }

  /// <summary>Expected SHA-256 of <see cref="PackageUrl" />.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Sha256 { get; set; }
}

public static class FeedCatalog
{
  internal static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented               = true,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling         = JsonCommentHandling.Skip,
    AllowTrailingCommas         = true,
  };

  public static IReadOnlyList<CatalogEntry> Load(string path)
  {
    var entries = JsonSerializer.Deserialize<List<CatalogEntry>>(File.ReadAllText(path), JsonOptions)
      ?? throw new InvalidDataException($"{path} is empty");

    var duplicate = entries.GroupBy(e => e.PackageName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
    if (duplicate != null)
      throw new InvalidDataException($"{path} lists {duplicate.Key} more than once");

    return entries;
  }

  public static CatalogEntry Find(IEnumerable<CatalogEntry> catalog, string packageName) =>
    catalog.FirstOrDefault(e => string.Equals(e.PackageName, packageName, StringComparison.OrdinalIgnoreCase))
    ?? throw new KeyNotFoundException($"{packageName} is not in the plugin catalog");
}
