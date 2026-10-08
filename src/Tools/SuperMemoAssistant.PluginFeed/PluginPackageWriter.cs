namespace SuperMemoAssistant.PluginFeed;

using System.Diagnostics;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

/// <summary>Packs a published plugin folder into the package layout that SMA's PluginManager installs and loads.</summary>
public static class PluginPackageWriter
{
  /// <summary>Framework of SuperMemoAssistant.exe. PluginManager picks the lib group nearest to it.</summary>
  public const string TargetFramework = "net10.0-windows10.0.19041.0";

  /// <summary>
  ///   Writes <c>&lt;id&gt;.&lt;version&gt;.nupkg</c>. The whole folder goes under <c>lib/&lt;tfm&gt;/</c>: PluginHost loads
  ///   <c>&lt;id&gt;.dll</c> from there and resolves the rest through the plugin's deps.json, which uses paths relative
  ///   to that dll.
  /// </summary>
  /// <param name="entry">Catalog metadata of the plugin.</param>
  /// <param name="sourceDir">Published plugin folder.</param>
  /// <param name="outputDir">Folder that receives the package.</param>
  /// <param name="version">Package version; defaults to the product version of the plugin assembly.</param>
  /// <returns>The package path.</returns>
  public static string Write(CatalogEntry entry, string sourceDir, string outputDir, string? version = null)
  {
    var entryAssembly = Path.Combine(sourceDir, entry.PackageName + ".dll");
    if (!File.Exists(entryAssembly))
      throw new FileNotFoundException($"PluginHost loads {entry.PackageName}.dll, which is missing from {sourceDir}", entryAssembly);

    version ??= FileVersionInfo.GetVersionInfo(entryAssembly).ProductVersion
      ?? throw new InvalidDataException($"{entryAssembly} has no product version");

    var nuGetVersion = NuGetVersion.Parse(version);
    var framework    = NuGetFramework.Parse(TargetFramework);

    var builder = new PackageBuilder(deterministic: true)
    {
      Id          = entry.PackageName,
      Version     = nuGetVersion,
      Title       = entry.DisplayName,
      Description = entry.Description,
    };
    builder.Authors.Add(entry.Author);
    builder.Tags.Add("SuperMemoAssistant");
    builder.Tags.Add("plugin");
    builder.DependencyGroups.Add(new PackageDependencyGroup(framework, Array.Empty<PackageDependency>()));

    var libDir = $"lib/{framework.GetShortFolderName()}/";
    foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
      builder.Files.Add(new PhysicalPackageFile
      {
        SourcePath = file,
        TargetPath = libDir + Path.GetRelativePath(sourceDir, file).Replace('\\', '/'),
      });

    Directory.CreateDirectory(outputDir);
    var packagePath = Path.Combine(outputDir, $"{entry.PackageName}.{nuGetVersion.ToNormalizedString()}.nupkg");

    using (var stream = File.Create(packagePath))
      builder.Save(stream);

    PluginPackageValidator.Validate(packagePath);
    return packagePath;
  }
}
