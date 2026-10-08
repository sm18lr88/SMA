// Command-line entry used by build\pack-plugins.ps1.
//   pack --catalog <catalog.json> --id <package id> --source <published folder> --out <folder> [--version <version>]
//   feed --catalog <catalog.json> --packages <folder of .nupkg> --base-url <public URL> --out <empty folder>
using SuperMemoAssistant.PluginFeed;

try
{
  if (args.Length == 0 || args.Length % 2 == 0)
    return Usage();

  var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
  for (var i = 1; i < args.Length; i += 2)
    options[args[i].TrimStart('-')] = args[i + 1];

  string Required(string name) => options.TryGetValue(name, out var value) ? value : throw new ArgumentException($"--{name} is required");

  var catalog = FeedCatalog.Load(Required("catalog"));

  switch (args[0])
  {
    case "pack":
      var entry = FeedCatalog.Find(catalog, Required("id"));
      Console.WriteLine(PluginPackageWriter.Write(entry, Required("source"), Required("out"), options.GetValueOrDefault("version")));
      return 0;

    case "feed":
      var packages = Directory.GetFiles(Required("packages"), "*.nupkg");
      StaticFeedWriter.Write(packages, catalog, new Uri(Required("base-url")), Required("out"), DateTime.UtcNow);
      Console.WriteLine($"Wrote a feed with {packages.Length} packages to {Required("out")}");
      return 0;

    default:
      return Usage();
  }
}
catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or KeyNotFoundException or UriFormatException)
{
  Console.Error.WriteLine(ex.Message);
  return 1;
}

static int Usage()
{
  Console.Error.WriteLine("usage: pack|feed --catalog <catalog.json> [options]; see build\\pack-plugins.ps1");
  return 2;
}
