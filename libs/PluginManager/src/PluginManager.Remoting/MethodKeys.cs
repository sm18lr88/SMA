// Stable string identity for methods invoked over the wire, resolved back to MethodInfo on the receiving side.
namespace PluginManager.Remoting;

using System.Collections.Concurrent;
using System.Reflection;

internal static class MethodKeys
{
  public const string DelegateInvoke = "$invoke";

  private static readonly ConcurrentDictionary<string, MethodInfo> Cache = new(StringComparer.Ordinal);

  public static string Encode(MethodInfo method)
  {
    var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
    var parameters = string.Join(",", definition.GetParameters().Select(p => p.ParameterType.ToString()));
    return $"{TypeNames.Encode(method.DeclaringType!)}\n{method.Name}\n{parameters}\n{definition.GetGenericArguments().Length}";
  }

  public static MethodInfo Resolve(string key, IReadOnlyList<Type> genericArguments)
  {
    var definition = Cache.GetOrAdd(key, static k =>
    {
      var parts = k.Split('\n');
      var type  = TypeNames.Resolve(parts[0]);
      var arity = int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);

      return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 .FirstOrDefault(m => m.Name == parts[1]
                                   && m.GetGenericArguments().Length == arity
                                   && string.Join(",", m.GetParameters().Select(p => p.ParameterType.ToString())) == parts[2])
             ?? throw new RemotingException($"Method '{parts[1]}({parts[2]})' not found on '{type.FullName}'.");
    });

    return genericArguments.Count == 0 ? definition : definition.MakeGenericMethod(genericArguments.ToArray());
  }
}
