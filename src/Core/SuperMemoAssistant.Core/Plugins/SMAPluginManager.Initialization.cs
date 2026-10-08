// Waits until every connected plugin has finished initializing, so the plugins have registered what they register while initializing.
namespace SuperMemoAssistant.Plugins
{
  using System;
  using System.Linq;
  using System.Threading.Tasks;
  using Anotar.Serilog;

  public partial class SMAPluginManager
  {
    /// <summary>
    ///   A plugin counts as started when its host process has connected, which happens before the plugin runs its
    ///   initialization. This waits for the initialization of every connected plugin, so that, for example, the launch hooks
    ///   of a plugin are registered before they are needed. A plugin that cannot answer (built for an older SMA) counts as ready.
    /// </summary>
    /// <returns>How many plugins did not finish initializing within <paramref name="timeout" /></returns>
    public async Task<int> WaitForPluginsInitializedAsync(TimeSpan timeout)
    {
      var plugins = RunningPluginMap.Values.Select(instance => (Name: instance.ToString(), instance.Plugin))
                                    .Where(p => p.Plugin != null)
                                    .ToList();
      var limit   = (int)Math.Min(int.MaxValue, timeout.TotalMilliseconds);

      var results = await Task.WhenAll(plugins.Select(p => Task.Run(() => IsInitialized(p.Name, () => p.Plugin.WaitUntilInitialized(limit)))))
                              .ConfigureAwait(false);
      var late = results.Count(ready => !ready);

      if (late > 0)
        LogTo.Warning("{Late} of {Count} plugins did not finish initializing within {Seconds} seconds", late, plugins.Count, timeout.TotalSeconds);
      else
        LogTo.Information("{Count} connected plugin(s) have finished initializing", plugins.Count);

      return late;
    }

    private static bool IsInitialized(string plugin, Func<bool> wait)
    {
      try
      {
        return wait();
      }
      catch (Exception ex)
      {
        LogTo.Debug(ex, "{Plugin} cannot report its initialization (it was probably built for an older SMA); treating it as ready", plugin);

        return true;
      }
    }
  }
}
