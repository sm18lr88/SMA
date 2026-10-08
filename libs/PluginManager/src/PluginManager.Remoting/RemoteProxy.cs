// Client-side stand-ins for remote objects: interface proxies and delegate forwarders.
namespace PluginManager.Remoting;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

/// <summary>Base type of every interface proxy. Use <see cref="RemoteObjects.IsRemote" /> rather than this type directly.</summary>
public class RemoteProxy : DispatchProxy
{
  internal RpcConnection? Connection { get; set; }

  internal long Id { get; set; }

  protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
    Connection!.Invoke(Id, targetMethod ?? throw new ArgumentNullException(nameof(targetMethod)), args ?? []);

  ~RemoteProxy() => Connection?.ReleaseImport(Id);
}

public static class RemoteObjects
{
  /// <summary>True when <paramref name="value" /> is a proxy or delegate standing in for an object in another process.</summary>
  public static bool IsRemote(object? value) =>
    value is RemoteProxy || (value is Delegate d && d.Target is RemoteDelegateTarget);
}

/// <summary>Target of delegates that forward invocations to a remote delegate.</summary>
public sealed class RemoteDelegateTarget
{
  internal RemoteDelegateTarget(RpcConnection connection, long id, Type delegateType)
  {
    Connection   = connection;
    Id           = id;
    DelegateType = delegateType;
  }

  internal RpcConnection Connection { get; }

  internal long Id { get; }

  internal Type DelegateType { get; }

  public object? Invoke(object?[] args) => Connection.InvokeDelegate(Id, DelegateType, args);

  ~RemoteDelegateTarget() => Connection.ReleaseImport(Id);
}

internal static class ProxyFactory
{
  private static readonly ConcurrentDictionary<string, Type> Composites = new(StringComparer.Ordinal);
  private static readonly ModuleBuilder Module =
    AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PluginManager.Remoting.Composites"), AssemblyBuilderAccess.Run)
                   .DefineDynamicModule("Composites");

  public static object CreateProxy(RpcConnection connection, long id, IReadOnlyList<Type> interfaces, Type declaredType)
  {
    var candidates = interfaces.Where(i => i.IsInterface && i.IsVisible).ToList();
    if (declaredType.IsInterface && !candidates.Contains(declaredType))
      candidates.Insert(0, declaredType);

    if (candidates.Count == 0)
      throw new RemotingException($"Cannot proxy a remote object as '{declaredType.FullName}': expose it through a public interface.");

    var proxyInterface = candidates.Count == 1 ? candidates[0] : Composite(candidates);
    var proxy          = (RemoteProxy)DispatchProxy.Create(proxyInterface, typeof(RemoteProxy));
    proxy.Connection = connection;
    proxy.Id         = id;
    return proxy;
  }

  public static Delegate CreateDelegate(RpcConnection connection, long id, Type delegateType)
  {
    var invoke = delegateType.GetMethod("Invoke") ?? throw new RemotingException($"{delegateType} is not a delegate type.");
    var target = new RemoteDelegateTarget(connection, id, delegateType);

    var parameters = invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
    Expression body = Expression.Call(
      Expression.Constant(target),
      typeof(RemoteDelegateTarget).GetMethod(nameof(RemoteDelegateTarget.Invoke))!,
      Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object)))));

    if (invoke.ReturnType != typeof(void))
      body = Expression.Convert(body, invoke.ReturnType);

    return Expression.Lambda(delegateType, body, parameters).Compile();
  }

  /// <summary>Emits (once per interface set) an empty interface inheriting all of them, so one proxy can be cast to each.</summary>
  private static Type Composite(IReadOnlyList<Type> interfaces)
  {
    var key = string.Join("|", interfaces.Select(i => i.AssemblyQualifiedName).Order(StringComparer.Ordinal));
    return Composites.GetOrAdd(key, _ =>
    {
      lock (Module)
      {
        var builder = Module.DefineType($"Composite{Composites.Count}_{Guid.NewGuid():N}",
                                        TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        foreach (var i in interfaces)
          builder.AddInterfaceImplementation(i);
        return builder.CreateType();
      }
    });
  }
}
