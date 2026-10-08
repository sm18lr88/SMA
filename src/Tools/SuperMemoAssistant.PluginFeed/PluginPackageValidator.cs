namespace SuperMemoAssistant.PluginFeed;

using System.Diagnostics;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

/// <summary>
///   Rejects packages that SMA would install but refuse to start. The checks repeat PluginManagerBase.StartPlugin and
///   PluginLoader, so a broken third-party package fails the feed build instead of failing on user machines.
/// </summary>
public static class PluginPackageValidator
{
  public const string InteropAssembly = "SuperMemoAssistant.Interop";

  /// <summary>Same value as SMAPluginManager.MinInteropVersion.</summary>
  public static readonly NuGetVersion MinInteropVersion = new(3, 0, 0);

  public static PackageIdentity Validate(string packagePath)
  {
    using var reader   = new PackageArchiveReader(packagePath);
    var       identity = reader.GetIdentity();

    // The feed only contains plugin packages, so a dependency on any other package could never be resolved.
    if (reader.NuspecReader.GetDependencyGroups().Any(g => g.Packages.Any()))
      throw new InvalidDataException($"{identity} declares NuGet dependencies. Feed packages must carry every dependency in lib/.");

    var groups  = reader.GetReferenceItems().ToList();
    var nearest = new FrameworkReducer().GetNearest(NuGetFramework.Parse(PluginPackageWriter.TargetFramework), groups.Select(g => g.TargetFramework));
    var items   = nearest == null ? [] : groups.First(g => g.TargetFramework.Equals(nearest)).Items.ToList();

    string? FindAssembly(string name) =>
      items.FirstOrDefault(i => string.Equals(Path.GetFileName(i), name + ".dll", StringComparison.OrdinalIgnoreCase));

    if (FindAssembly(identity.Id) == null)
      throw new InvalidDataException($"{identity} has no lib/<net10.0-windows>/{identity.Id}.dll, which PluginHost loads");

    var interop = FindAssembly(InteropAssembly)
      ?? throw new InvalidDataException($"{identity} does not contain {InteropAssembly}.dll, so SMA refuses to start it");

    var interopVersion = ReadProductVersion(reader, interop);
    if (interopVersion == null || interopVersion < MinInteropVersion)
      throw new InvalidDataException($"{identity} carries {InteropAssembly} {interopVersion}; SMA requires {MinInteropVersion} or later");

    return identity;
  }

  private static NuGetVersion? ReadProductVersion(PackageArchiveReader reader, string entry)
  {
    // FileVersionInfo reads only files on disk.
    var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");

    try
    {
      using (var source = reader.GetStream(entry))
      using (var target = File.Create(tempFile))
        source.CopyTo(target);

      return NuGetVersion.TryParse(FileVersionInfo.GetVersionInfo(tempFile).ProductVersion, out var version) ? version : null;
    }
    finally
    {
      File.Delete(tempFile);
    }
  }
}
