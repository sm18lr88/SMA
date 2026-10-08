// Publishes plugin packages as a static feed, serves it over loopback HTTP, then lists and installs the plugins through
// the PluginManager code that SMA uses for "Browse plugins" (catalog fetch, NuGet search, NuGet install).
namespace SuperMemoAssistant.Tests.Plugins;

using System.Diagnostics;
using NuGet.Configuration;
using NuGet.Versioning;
using PluginManager.PackageManager;
using PluginManager.PackageManager.Models;
using PluginManager.PackageManager.NuGet;
using PluginManager.Services;
using SuperMemoAssistant.PluginFeed;
using SuperMemoAssistant.Plugins.Models;
using SuperMemoAssistant.SMA.Configs;
using Xunit;

public sealed class PluginFeedTests : IDisposable
{
  private const string TestPluginId = "SmaFeedTest.Plugin";

  /// <summary>Folder of a feed built by build\pack-plugins.ps1 (artifacts\feed). Enables <see cref="InstallsEveryPublishedPlugin" />.</summary>
  private const string PublishedFeedVariable = "SMA_PLUGIN_FEED";

  // Nothing listens on port 9 (discard), so the connection is refused at once, as for a user who is offline.
  private const string UnreachableSource = "http://127.0.0.1:9/nuget/index.json";

  private static readonly TimeSpan ServerLifetime = TimeSpan.FromMinutes(5);

  private readonly string _root = Path.Combine(Path.GetTempPath(), "sma-plugin-feed-" + Guid.NewGuid().ToString("N"));

  public void Dispose()
  {
    if (Directory.Exists(_root))
      Directory.Delete(_root, recursive: true);
  }

  [Fact]
  public void DefaultUrlsPointAtThePublishedFeedLayout()
  {
    var cfg = new UpdateCfg();

    Assert.Equal(UpdateCfg.PluginsFeedBaseUrl + StaticFeedWriter.CatalogFile, cfg.PluginsUpdateUrl);
    Assert.Equal(UpdateCfg.PluginsFeedBaseUrl + StaticFeedWriter.ServiceIndexFile, Assert.Single(cfg.EffectivePluginsNuGetUrls));
    Assert.True(cfg.HasPluginCatalog);
  }

  [Theory]
  [InlineData("")]
  [InlineData("https://releases.supermemo.wiki/plugins")]
  public void LegacyCatalogUrlsFallBackToTheFeed(string configured)
  {
    var cfg = new UpdateCfg { PluginsUpdateUrl = configured };

    Assert.Equal(UpdateCfg.PluginsDefaultCatalogUrl, cfg.PluginsUpdateUrl);
  }

  [Fact]
  public void LegacyNuGetSourcesAreDropped()
  {
    var legacy = new UpdateCfg
    {
      PluginsUpdateNuGetUrls =
      [
        "https://api.nuget.org/v3/index.json",
        "https://pkgs.dev.azure.com/accounts0054/SuperMemoAssistant/_packaging/SuperMemoAssistant-Alpha/nuget/v3/index.json",
      ],
    };
    var custom = new UpdateCfg { PluginsUpdateNuGetUrls = ["https://example.org/feed/index.json", "https://api.nuget.org/v3/index.json"] };

    Assert.Equal([UpdateCfg.PluginsDefaultRepositoryUrl], legacy.EffectivePluginsNuGetUrls);
    Assert.Equal(["https://example.org/feed/index.json"], custom.EffectivePluginsNuGetUrls);
  }

  [Fact]
  public async Task ListsAndInstallsAPluginFromTheStaticFeed()
  {
    var ct     = TestContext.Current.CancellationToken;
    var source = Directory.CreateDirectory(Path.Combine(_root, "plugin")).FullName;

    // The plugin assembly is never loaded here, so placeholder bytes are enough. The Interop copy is real, because the
    // install is checked against the same version gate that PluginManagerBase.StartPlugin applies.
    File.WriteAllText(Path.Combine(source, TestPluginId + ".dll"), "placeholder");
    File.WriteAllText(Path.Combine(source, TestPluginId + ".deps.json"), "{}");
    File.Copy(Path.Combine(AppContext.BaseDirectory, PluginPackageValidator.InteropAssembly + ".dll"),
              Path.Combine(source, PluginPackageValidator.InteropAssembly + ".dll"));
    Directory.CreateDirectory(Path.Combine(source, "runtimes", "win-x64", "native"));
    File.WriteAllText(Path.Combine(source, "runtimes", "win-x64", "native", "native.dll"), "placeholder");

    var entry = new CatalogEntry
    {
      PackageName = TestPluginId,
      DisplayName = "Feed test plugin",
      Description = "Package built by PluginFeedTests.",
      Author      = "SMA tests",
      Labels      = [PluginMetadata.OfficialLabel],
    };
    var package = PluginPackageWriter.Write(entry, source, Path.Combine(_root, "packages"), "1.2.3");

    await using var server = await ServeFeedAsync([package], [entry]);
    var (repository, packageManager) = await CreateClientAsync(server);

    var catalog = await repository.FetchPluginMetadataList(ct);
    Assert.Equal("Feed test plugin", Assert.Single(catalog!).DisplayName);

    var found  = await repository.SearchPlugins("SmaFeedTest", false, packageManager, ct);
    var online = Assert.IsType<OnlinePluginPackage<PluginMetadata>>(Assert.Single(found!));
    Assert.Equal(NuGetVersion.Parse("1.2.3"), online.LatestOnlineVersion);
    Assert.True(online.Metadata.IsOfficial);

    Assert.True(await packageManager.InstallAsync(online, online.LatestOnlineVersion, null, ct));

    var entryAssembly = AssertInstalledAndStartable(packageManager, TestPluginId);
    Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(entryAssembly)!, "runtimes", "win-x64", "native", "native.dll")));
    Assert.StartsWith(Path.Combine(_root, "Plugins", "Packages"), entryAssembly, StringComparison.OrdinalIgnoreCase);
    Assert.Contains(server.RequestedPaths, p => p == "/nuget/search/query.json");
  }

  [Fact]
  public void ConfiguredNuGetSourcesReplaceTheDefault()
  {
    var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateCfg>("{\"PluginsUpdateNuGetUrls\":[\"https://example.org/feed/index.json\"]}")!;

    Assert.Equal(["https://example.org/feed/index.json"], cfg.EffectivePluginsNuGetUrls);
  }

  [Fact]
  public async Task SearchSkipsAnUnreachableSource()
  {
    var ct      = TestContext.Current.CancellationToken;
    var entry   = new CatalogEntry { PackageName = TestPluginId, DisplayName = "Feed test plugin", Description = "Test.", Author = "SMA tests" };
    var package = PluginPackageWriter.Write(entry, CreatePluginFolder(), Path.Combine(_root, "packages"), "1.0.0");

    await using var server = await ServeFeedAsync([package], [entry]);
    var (repository, packageManager) = await CreateClientAsync(server, UnreachableSource);

    var found = await repository.SearchPlugins("SmaFeedTest", false, packageManager, ct);
    Assert.Equal(TestPluginId, Assert.Single(found!).Id);

    var unreachable = packageManager.SourceRepositories.CreateRepository(new PackageSource(UnreachableSource));
    Assert.Null(await packageManager.Search("SmaFeedTest", [unreachable], cancellationToken: ct));
  }

  /// <summary>A plugin folder with the files that the package checks need. The assembly is never loaded.</summary>
  private string CreatePluginFolder()
  {
    var source = Directory.CreateDirectory(Path.Combine(_root, "plugin")).FullName;
    File.WriteAllText(Path.Combine(source, TestPluginId + ".dll"), "placeholder");
    File.WriteAllText(Path.Combine(source, TestPluginId + ".deps.json"), "{}");
    File.Copy(Path.Combine(AppContext.BaseDirectory, PluginPackageValidator.InteropAssembly + ".dll"),
              Path.Combine(source, PluginPackageValidator.InteropAssembly + ".dll"), overwrite: true);
    return source;
  }

  [Fact]
  public async Task InstallsEveryPublishedPlugin()
  {
    var published = Environment.GetEnvironmentVariable(PublishedFeedVariable);
    Assert.SkipWhen(string.IsNullOrEmpty(published), $"Set {PublishedFeedVariable} to the artifacts\\feed folder of build\\pack-plugins.ps1.");

    var ct       = TestContext.Current.CancellationToken;
    var catalog  = FeedCatalog.Load(Path.Combine(published!, StaticFeedWriter.CatalogFile));
    var packages = Directory.GetFiles(Path.Combine(published!, "nuget", "flatcontainer"), "*.nupkg", SearchOption.AllDirectories);

    // The published feed names its public URL in every resource, so it is rebuilt for the loopback server.
    await using var server = await ServeFeedAsync(packages, catalog);
    var (repository, packageManager) = await CreateClientAsync(server);

    var found = (await repository.SearchPlugins("SuperMemoAssistant.Plugins", false, packageManager, ct))!.ToList();
    Assert.Equal(catalog.Select(e => e.PackageName).Order(), found.Select(p => p.Id).Order());

    foreach (var online in found.Cast<OnlinePluginPackage<PluginMetadata>>())
    {
      Assert.True(await packageManager.InstallAsync(online, online.LatestOnlineVersion, null, ct), online.Id);
      AssertInstalledAndStartable(packageManager, online.Id);
    }
  }

  private async Task<LoopbackFeedServer> ServeFeedAsync(IEnumerable<string> packages, IEnumerable<CatalogEntry> catalog)
  {
    var feedDir = Path.Combine(_root, "feed");
    Directory.CreateDirectory(feedDir);

    var server = LoopbackFeedServer.Create(feedDir, ServerLifetime);
    try
    {
      StaticFeedWriter.Write(packages, catalog, server.BaseUrl, feedDir, DateTime.UtcNow);
      return server;
    }
    catch
    {
      await server.DisposeAsync();
      throw;
    }
  }

  private async Task<(CatalogRepositoryService, PluginPackageManager<PluginMetadata>)> CreateClientAsync(LoopbackFeedServer server,
                                                                                                       params string[] extraSources)
  {
    var cfg = new UpdateCfg
    {
      PluginsUpdateUrl       = new Uri(server.BaseUrl, StaticFeedWriter.CatalogFile).AbsoluteUri,
      PluginsUpdateNuGetUrls = [new Uri(server.BaseUrl, StaticFeedWriter.ServiceIndexFile).AbsoluteUri, "https://api.nuget.org/v3/index.json", ..extraSources],
    };

    var pluginDir = Directory.CreateDirectory(Path.Combine(_root, "Plugins")).FullName;

    // NuGet extracts downloads into the global packages folder; this nuget.config keeps them inside the test folder.
    File.WriteAllText(Path.Combine(pluginDir, "nuget.config"),
                      $"<configuration><config><add key=\"globalPackagesFolder\" value=\"{Path.Combine(_root, "global-packages")}\" /></config></configuration>");

    var packageManager = await PluginPackageManager<PluginMetadata>.Create(
      pluginDir,
      Path.Combine(pluginDir, "Home"),
      Path.Combine(pluginDir, "Packages"),
      Path.Combine(pluginDir, "plugins.json"),
      settings => new SourceRepositoryProvider(settings, cfg.EffectivePluginsNuGetUrls));

    return (new CatalogRepositoryService(cfg.PluginsUpdateUrl), packageManager);
  }

  /// <summary>Repeats the checks of PluginManagerBase.StartPlugin and PluginLoader on the installed files.</summary>
  /// <returns>The path of the plugin assembly that PluginHost loads.</returns>
  private static string AssertInstalledAndStartable(PluginPackageManager<PluginMetadata> packageManager, string id)
  {
    var installed = packageManager.FindInstalledPluginById(id);
    Assert.NotNull(installed);

    packageManager.GetInstalledPluginAssembliesFilePath(installed.Identity, out var pluginAssemblies, out var dependencyAssemblies);
    var assemblies = pluginAssemblies.Concat(dependencyAssemblies).Select(f => f.FullPath).ToList();

    var entryAssembly = Path.GetFullPath(Assert.Single(assemblies, a => Path.GetFileNameWithoutExtension(a) == id));
    Assert.True(File.Exists(entryAssembly));
    Assert.True(File.Exists(Path.ChangeExtension(entryAssembly, ".deps.json")), $"{id} has no deps.json next to {entryAssembly}");

    var interop = Assert.Single(assemblies, a => Path.GetFileNameWithoutExtension(a) == PluginPackageValidator.InteropAssembly);
    Assert.True(NuGetVersion.Parse(FileVersionInfo.GetVersionInfo(interop).ProductVersion!) >= PluginPackageValidator.MinInteropVersion);

    return entryAssembly;
  }

  /// <summary>The PluginManager catalog client with the package ID mapping of SMA's PluginRepositoryService.</summary>
  private sealed class CatalogRepositoryService(string updateUrl) : DefaultPluginRepositoryService<PluginMetadata>
  {
    public override string UpdateUrl     => updateUrl;
    public override bool   UpdateEnabled => true;

    protected override string GetPackageIdFromMetadata(PluginMetadata metadata) => metadata.PackageName;
  }
}
