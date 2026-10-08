// Type name encoding and allowlisted resolution for values crossing the pipe.
namespace PluginManager.Remoting;

using System.Collections.Concurrent;
using System.Reflection;

public static class TypeNames
{
  private static readonly ConcurrentDictionary<string, Type> Cache = new(StringComparer.Ordinal);

  /// <summary>
  ///   Decides which assemblies a remote peer may name. Defaults to framework, SMA and plugin-manager assemblies plus any
  ///   assembly already loaded in this process. Hosts may widen it (e.g. plugin assemblies) before connecting.
  /// </summary>
  public static Func<AssemblyName, bool> AllowAssembly { get; set; } = DefaultAllowAssembly;

  public static string Encode(Type type) => type.AssemblyQualifiedName ?? throw new ArgumentException($"Type {type} has no assembly-qualified name.");

  public static Type Resolve(string name) => Cache.GetOrAdd(name, static n =>
    Type.GetType(n, ResolveAssembly, typeResolver: null, throwOnError: false)
    ?? throw new RemotingException($"Remote type '{n}' is not available or not allowed in this process."));

  private static Assembly? ResolveAssembly(AssemblyName name)
  {
    foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
      if (AssemblyName.ReferenceMatchesDefinition(new AssemblyName(name.Name!), loaded.GetName()))
        return loaded;

    if (!AllowAssembly(name))
      return null;

    try
    {
      return Assembly.Load(new AssemblyName(name.Name!));
    }
    catch (FileNotFoundException)
    {
      return null;
    }
  }

  private static bool DefaultAllowAssembly(AssemblyName name)
  {
    var n = name.Name ?? string.Empty;
    return n == "mscorlib" || n == "netstandard" || n.StartsWith("System", StringComparison.Ordinal)
      || n.StartsWith("SuperMemoAssistant", StringComparison.Ordinal) || n.StartsWith("PluginManager", StringComparison.Ordinal)
      || n.StartsWith("Extensions.System", StringComparison.Ordinal);
  }
}
