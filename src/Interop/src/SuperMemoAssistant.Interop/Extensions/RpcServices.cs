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

#endregion




// ReSharper disable IdentifierTypo

namespace SuperMemoAssistant.Extensions
{
  using System;
  using System.Linq;
  using Anotar.Serilog;
  using PluginManager.Remoting;

  /// <summary>Plugin helpers over <see cref="RpcEndpoint" />: connect to and serve RPC endpoints, and raise events across processes.</summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1715:Identifiers should have correct prefix",
                                                   Justification = "Extending services")]
  public static class RpcServices
  {
    #region Methods

    /// <summary>Connects to the service published on <paramref name="pipeName" /> and returns its proxy.</summary>
    public static IService Connect<IService>(string pipeName) => RpcEndpoint.Connect<IService>(pipeName);

    /// <summary>Publishes <paramref name="service" /> on a pipe named <paramref name="pipeName" /> (random when null).</summary>
    public static RpcServer Serve<IService, TService>(TService service, string pipeName = null)
      where IService : class
      where TService : MarshalByRefObject, IService =>
      RpcEndpoint.Serve(pipeName ?? NewPipeName(), service);

    /// <summary>Generates a random, hard to guess name for an RPC server pipe</summary>
    public static string NewPipeName() => RpcEndpoint.NewPipeName();

    /// <summary>
    ///   Safely raises the <paramref name="event" /> event. If a target from the invocation list throws a
    ///   <see cref="RemotingException" /> it is forcefully unsubscribed for the event.
    /// </summary>
    /// <param name="event">The event to raise</param>
    /// <param name="eventName">Friendly name for the event</param>
    /// <param name="unsubscribeDelegate">
    ///   The action to run to unsubscribe the given delegate from the
    ///   <paramref name="event" />
    /// </param>
    public static void InvokeRemote(
      this Action    @event,
      string         eventName,
      Action<Action> unsubscribeDelegate)
    {
      foreach (var handler in @event.GetInvocationList().Cast<Action>())
        try
        {
          handler();
        }
        catch (RemotingException remoteEx)
        {
          LogTo.Warning(remoteEx, "{EventName}: Remoting exception while notifying remote service - forcing unsubscribe", eventName);
          unsubscribeDelegate?.Invoke(handler);
        }
        catch (NullReferenceException)
        {
          LogTo.Warning("Null handler called for event {EventName}.", eventName);
        }
        catch (Exception ex)
        {
          LogTo.Warning(ex, "{EventName}: Exception while notifying remote service", eventName);
        }
    }

    /// <summary>
    ///   Safely raises the <paramref name="event" /> event. If a target from the invocation list throws a
    ///   <see cref="RemotingException" /> it is forcefully unsubscribed for the event.
    /// </summary>
    /// <typeparam name="TParam1"></typeparam>
    /// <param name="event">The event to raise</param>
    /// <param name="eventName">Friendly name for the event</param>
    /// <param name="p1">The event argument</param>
    /// <param name="unsubscribeDelegate">
    ///   The action to run to unsubscribe the given delegate from the
    ///   <paramref name="event" />
    /// </param>
    public static void InvokeRemote<TParam1>(
      this Action<TParam1>    @event,
      string                  eventName,
      TParam1                 p1,
      Action<Action<TParam1>> unsubscribeDelegate)
    {
      foreach (var handler in @event.GetInvocationList().Cast<Action<TParam1>>())
        try
        {
          handler(p1);
        }
        catch (RemotingException remoteEx)
        {
          LogTo.Warning(remoteEx, "{EventName}: Remoting exception while notifying remote service - forcing unsubscribe", eventName);
          unsubscribeDelegate?.Invoke(handler);
        }
        catch (NullReferenceException)
        {
          LogTo.Warning("Null handler called for event {EventName}.", eventName);
        }
        catch (Exception ex)
        {
          LogTo.Warning(ex, "{EventName}: Exception while notifying remote service", eventName);
        }
    }

    #endregion
  }
}
