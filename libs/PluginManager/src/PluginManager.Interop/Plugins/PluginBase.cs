#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   2020/03/29 00:20
// Modified On:  2020/10/25 08:25
// Modified By:  Alexis

#endregion




namespace PluginManager.Interop.Plugins
{
  using System;
  using System.Collections.Concurrent;
  using System.Threading.Tasks;
  using Contracts;
  using Extensions;
  using PluginManager.Remoting;
  using Sys;

  public abstract class PluginBase<TPlugin, IPlugin, ICore> : PerpetualMarshalByRefObject, IPluginBase
    where TPlugin : PluginBase<TPlugin, IPlugin, ICore>, IPlugin
    where IPlugin : class
  {
    #region Properties & Fields - Non-Public

    private RpcServer PluginServer { get; }

    private ConcurrentDictionary<string, (RpcServer server, IDisposable disposable)> RegisteredServicesMap { get; } =
      new ConcurrentDictionary<string, (RpcServer, IDisposable)>();
    private ConcurrentDictionary<string, object> ConsumedServiceMap { get; } = new ConcurrentDictionary<string, object>();

    #endregion




    #region Constructors

    protected PluginBase(string pipeName)
    {
      PipeName  = pipeName;
      PluginServer = RpcEndpoint.Serve(PipeName, this);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
      // The plugin manager calls Dispose remotely: end the process only after it has received the reply.
      RpcConnection.RunAfterReply(() =>
      {
        PluginServer.Dispose();
        Environment.Exit(0);
      });
    }

    #endregion




    #region Properties & Fields - Public

    public ICore                 Service             { get; set; }
    public IPluginManager<ICore> PluginMgr           { get; set; }
    public Guid                  SessionGuid         { get; set; }
    public bool                  IsDevelopmentPlugin { get; set; }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public string AssemblyName => GetType().GetAssemblyName();
    /// <inheritdoc />
    public string AssemblyVersion => GetType().GetAssemblyVersion();
    /// <inheritdoc />
    public string PipeName { get; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public virtual void OnInjected()
    {
      if (SessionGuid == Guid.Empty)
        throw new NullReferenceException($"{nameof(SessionGuid)} is empty");

      if (Service == null)
        throw new NullReferenceException($"{nameof(Service)} is null");

      if (PluginMgr == null)
        throw new NullReferenceException($"{nameof(PluginMgr)} is null");

      OnPluginInitialized();
    }

    /// <inheritdoc />
    public virtual void OnServicePublished(string interfaceTypeName)
    {
      // Ignored -- override for desired behavior
    }

    /// <inheritdoc />
    public virtual void OnServiceRevoked(string interfaceTypeName)
    {
      ConsumedServiceMap.TryRemove(interfaceTypeName, out _);
    }

    #endregion




    #region Methods

    /// <summary>Requests the Interop service from another Plugin.</summary>
    /// <typeparam name="IService">Interface for desired remote service.</typeparam>
    /// <returns>Service or <see langword="null" /></returns>
    public virtual IService GetService<IService>()
      where IService : class
    {
      var svcType = typeof(IService);

      if (svcType.IsInterface == false)
        throw new ArgumentException($"{nameof(IService)} must be an interface");

      var svcTypeName = svcType.FullName;

      if (svcTypeName == null)
        throw new ArgumentException("Invalid type");

      if (ConsumedServiceMap.ContainsKey(svcTypeName))
        return (IService)ConsumedServiceMap[svcTypeName];

      var pipeName = PluginMgr.GetService(svcTypeName);

      if (string.IsNullOrWhiteSpace(pipeName))
        return null;

      try
      {
        var svc = RpcEndpoint.Connect<IService>(pipeName);

        ConsumedServiceMap[svcTypeName] = svc;

        return svc;
      }
      catch (RemotingException ex)
      {
        LogError(ex, $"Failed to acquire remoting object for published service {svcTypeName}");
        return null;
      }
    }

    /// <summary>Publishes a service for other Plugins to consume.</summary>
    /// <typeparam name="IService">The interop interface for the service.</typeparam>
    /// <typeparam name="TService">
    ///   A type which <paramref name="service" /> can be cast to and which
    ///   extends <see cref="PerpetualMarshalByRefObject" />. Typically the type implementing the
    ///   service.
    /// </typeparam>
    /// <param name="service">An instance of the service.</param>
    /// <param name="pipeName">
    ///   Optional user-defined channel name. If <see langword="null" />, a
    ///   random channel name will be generated. Useful when the interface is intended to be shared
    ///   with other applications.
    /// </param>
    /// <remarks>
    ///   Only a single instance of the same interface <typeparamref name="IService" /> can be
    ///   published at once.
    /// </remarks>
    public virtual void PublishService<IService, TService>(TService service, string pipeName = null)
      where IService : class
      where TService : PerpetualMarshalByRefObject, IService
    {
      var svcType = typeof(IService);

      if (svcType.IsInterface == false)
        throw new ArgumentException($"{nameof(IService)} must be an interface");

      var svcTypeName = svcType.FullName;

      if (svcTypeName == null)
        throw new ArgumentException("Invalid type");

      if (RegisteredServicesMap.ContainsKey(svcTypeName))
        throw new ArgumentException($"Service {svcTypeName} already registered");

      LogInformation($"Publishing service {svcTypeName}");

      pipeName ??= RpcEndpoint.NewPipeName();
      var server = RpcEndpoint.Serve(pipeName, service);

      var unregisterObj = PluginMgr.RegisterService(SessionGuid, svcTypeName, pipeName);

      RegisteredServicesMap[svcTypeName] = (server, unregisterObj);
    }

    /// <summary>
    /// Revokes a service previously published through <see cref="PublishService{IService, TService}(TService, string)"/>.
    /// </summary>
    /// <typeparam name="IService">The interop interface for the service.</typeparam>
    /// <returns>Whether the operation was successful</returns>
    public bool RevokeService<IService>()
      where IService : class
    {
      return RevokeService(typeof(IService));
    }

    /// <summary>
    /// Revokes a service previously published through <see cref="PublishService{IService, TService}(TService, string)"/>.
    /// </summary>
    /// <param name="svcType">Instance of the service published.</param>
    /// <returns>Whether the operation was successful</returns>
    public bool RevokeService(Type svcType)
    {
      if (svcType.IsInterface == false)
        throw new ArgumentException("Service type must be an interface");

      var svcTypeName = svcType.FullName;

      if (svcTypeName == null)
        throw new ArgumentException("Invalid type");

      if (RegisteredServicesMap.TryRemove(svcTypeName, out var svcData) == false)
        return false;

      LogInformation($"Revoking service {svcTypeName}");

      svcData.disposable.Dispose();
      svcData.server.Dispose();

      return true;
    }

    /// <summary>
    /// Revokes all services previously published through <see cref="PublishService{IService, TService}(TService, string)"/>.
    /// </summary>
    public void RevokeServices()
    {
      foreach (var svcKeyValue in RegisteredServicesMap)
      {
        var svcType = svcKeyValue.Key;
        var svcData = svcKeyValue.Value;

        LogInformation($"Revoking service {svcType}");

        try
        {
          svcData.server.Dispose();
          svcData.disposable.Dispose();
        }
        catch (Exception ex)
        {
          LogError(ex, $"Exception while stopping published service {svcType}");
        }
      }
    }

    /// <summary>
    ///   Called when the Plugin has been instantiated and the remote service is available.
    ///   Override to run custom initialization code.
    /// </summary>
    protected virtual void OnPluginInitialized()
    {
      // Ignore
    }

    #endregion




    #region Methods Abs

    protected abstract void LogInformation(string message);
    protected abstract void LogError(Exception    ex, string message);

    /// <inheritdoc />
    public abstract string Name { get; }

    #endregion
  }
}
