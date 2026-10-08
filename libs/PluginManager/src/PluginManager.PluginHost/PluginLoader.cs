// Loads a plugin's assemblies into this PluginHost process and instantiates the plugin's PluginHost entry type.
namespace PluginHost
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Runtime.InteropServices;
  using System.Runtime.Loader;
  using System.Windows;

  public static class PluginLoader
  {
    /// <summary>
    ///   Makes the plugin's and its dependencies' assemblies resolvable, then creates the PluginHost type. Each plugin runs in
    ///   its own PluginHost process, which is the isolation boundary that AppDomains used to provide.
    /// </summary>
    /// <param name="packageRootFolder">Root folder under which all package assemblies are located.</param>
    /// <param name="pluginAndDependenciesAssembliesPath">
    ///   Plugin and dependency assembly paths relative to <paramref name="packageRootFolder" />, separated by
    ///   <see cref="PluginHostConst.PluginAndDependenciesAssembliesSeparator" />.
    /// </param>
    /// <param name="pluginHostTypeAssemblyName">Assembly that defines the PluginHost type.</param>
    /// <param name="pluginHostTypeQualifiedName">Namespace-qualified PluginHost type name.</param>
    /// <param name="pluginPackageName">The plugin's package name.</param>
    /// <param name="pluginHomeDir">The plugin's home directory.</param>
    /// <param name="sessionGuid">Session guid used to authenticate with the plugin manager.</param>
    /// <param name="mgrChannelName">The plugin manager's pipe name.</param>
    /// <param name="mgrProcess">The plugin manager's process.</param>
    /// <param name="isDev">Whether the plugin is a development plugin (assemblies live in its home directory).</param>
    public static IDisposable Create(string  packageRootFolder,
                                     string  pluginAndDependenciesAssembliesPath,
                                     string  pluginHostTypeAssemblyName,
                                     string  pluginHostTypeQualifiedName,
                                     string  pluginPackageName,
                                     string  pluginHomeDir,
                                     Guid    sessionGuid,
                                     string  mgrChannelName,
                                     Process mgrProcess,
                                     bool    isDev)
    {
      var assemblies = isDev
        ? Directory.EnumerateFiles(pluginHomeDir, "*.dll").ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
        : PackageAssemblies(packageRootFolder, pluginAndDependenciesAssembliesPath);

      if (!assemblies.TryGetValue(pluginPackageName, out var pluginEntryAssemblyFilePath))
      {
        Console.Error.WriteLine($"Unable to find {pluginPackageName} assembly file path in assembly list {pluginAndDependenciesAssembliesPath}");
        Application.Current.Shutdown(PluginHostConst.ExitCouldNotFindPluginAssembly);
        return null;
      }

      // The plugin's deps.json (EnableDynamicLoading) locates RID-specific managed and native assets, e.g. runtimes\win-x64\native.
      var resolver = new AssemblyDependencyResolver(pluginEntryAssemblyFilePath);

      AssemblyLoadContext.Default.Resolving += (context, name) =>
      {
        var path = resolver.ResolveAssemblyToPath(name);
        if (path is null && name.Name is not null && assemblies.TryGetValue(name.Name, out var mapped) && File.Exists(mapped))
          path = mapped;

        // Bundled plugins drop assemblies identical to the application's own copies (build\pack.ps1); use those.
        var shared = Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
        if (path is null && File.Exists(shared))
          path = shared;

        return path is null ? null : context.LoadFromAssemblyPath(path);
      };

      AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, libraryName) =>
        resolver.ResolveUnmanagedDllToPath(libraryName) is { } nativePath ? NativeLibrary.Load(nativePath) : IntPtr.Zero;

      var hostType = AssemblyLoadContext.Default
                                        .LoadFromAssemblyName(new AssemblyName(pluginHostTypeAssemblyName))
                                        .GetType(pluginHostTypeQualifiedName, throwOnError: true);

      return (IDisposable)Activator.CreateInstance(hostType,
                                                   pluginEntryAssemblyFilePath,
                                                   sessionGuid,
                                                   mgrChannelName,
                                                   mgrProcess,
                                                   isDev);
    }

    private static Dictionary<string, string> PackageAssemblies(string packageRootFolder, string pluginAndDependenciesAssembliesPath)
    {
      var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      var paths = pluginAndDependenciesAssembliesPath.Split(new[] { PluginHostConst.PluginAndDependenciesAssembliesSeparator },
                                                            StringSplitOptions.RemoveEmptyEntries);

      foreach (var relativePath in paths)
        map[Path.GetFileNameWithoutExtension(relativePath)] = packageRootFolder + relativePath;

      return map;
    }
  }
}
