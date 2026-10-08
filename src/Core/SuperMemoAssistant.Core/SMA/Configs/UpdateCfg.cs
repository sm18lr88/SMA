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




namespace SuperMemoAssistant.SMA.Configs
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using Forge.Forms.Annotations;
  using Newtonsoft.Json;
  using Plugins;
  using PropertyChanged;
  using Services.UI.Configuration;
  using SuperMemoAssistant.Extensions;
  using Sys.Collections;

  /// <summary>Core configuration for SMA and Plugins updates</summary>
  [Form(Mode = DefaultFields.None)]
  [Title("Update Settings",
         IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel",
                "Cancel",
                IsCancel = true)]
  [DialogAction("save",
                "Save",
                IsDefault = true,
                Validates = true)]
  [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
  [SuppressMessage("Design", "CA1056:Uri properties should not be strings")]
  [SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
  public class UpdateCfg : CfgBase<UpdateCfg>, INotifyPropertyChanged
  {
    #region Constants & Statics

    /// <summary>GitHub repository whose Releases carry the Velopack packages of this community build.</summary>
    public const string CoreDefaultUpdateUrl = "https://github.com/sm18lr88/SMA";

    /// <summary>Update host of upstream SMA 2.x. Configs written by 2.x can still name it; SMA ignores it.</summary>
    public const string LegacyUpdateHost = "releases.supermemo.wiki";

    public const string CoreStableChannel  = "Stable";
    public const string CoreBetaChannel    = "Beta";
    public const string CoreNightlyChannel = "Nightly";
    public const string CoreDefaultChannel = CoreBetaChannel;

    /// <summary>GitHub Pages site of this repository. build\pack-plugins.ps1 publishes the plugin feed there.</summary>
    public const string PluginsFeedBaseUrl = "https://sm18lr88.github.io/SMA/";

    /// <summary>Static plugin catalog (a JSON list of <see cref="Plugins.Models.PluginMetadata" />).</summary>
    public const string PluginsDefaultCatalogUrl = PluginsFeedBaseUrl + "plugins.json";

    /// <summary>Static NuGet v3 feed that serves the packages listed in the catalog.</summary>
    public const string PluginsDefaultRepositoryUrl = PluginsFeedBaseUrl + "nuget/index.json";

    /// <summary>
    ///   Plugin sources of upstream SMA 2.x: the catalog host, the Azure DevOps alpha feed, and nuget.org. They serve only
    ///   .NET Framework plugins or no longer exist, so configs written by older versions fall back to the feed above.
    /// </summary>
    private static readonly string[] LegacyPluginSources =
    {
      LegacyUpdateHost,
      "pkgs.dev.azure.com/accounts0054/",
      "api.nuget.org",
    };

    #endregion




    #region Constructors

    public UpdateCfg()
    {
      CoreUpdateChannel = CoreDefaultChannel;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>Enable auto-updates of SMA</summary>
    [JsonProperty]
    [Field(Name = "Enable SMA Auto-Updates")]
    public bool EnableCoreUpdates { get; set; } = true;

    /// <summary>Check the online plugin catalog (<see cref="PluginsUpdateUrl" />) for plugin updates</summary>
    [JsonProperty]
    public bool EnablePluginsUpdates { get; set; } = true;

    /// <summary>Proxy to display the update combo box</summary>
    [Field(Name                                                    = "SMA Update Channel")]
    [SelectFrom("{Binding CoreUpdateChannels.Keys}", SelectionType = SelectionType.ComboBoxEditable)]
    public string CoreUpdateChannelField
    {
      get => CoreUpdateChannel;
      set => CoreUpdateChannel = value;
    }

    //
    // Config only

    /// <summary>All Core update channels</summary>
    [JsonProperty]
    public Dictionary<string, string> CoreUpdateChannels { get; set; } = new Dictionary<string, string>
    {
      { CoreStableChannel, CoreDefaultUpdateUrl },
      { CoreBetaChannel, CoreDefaultUpdateUrl },
      { CoreNightlyChannel, CoreDefaultUpdateUrl },
    };

    [JsonProperty]
    public string CoreUpdateChannel { get; set; }

    /// <summary>The current URL to use for core updates</summary>
    [JsonProperty]
    public string CoreUpdateUrl
    {
      get
      {
        var url = CoreUpdateChannels.SafeGet(CoreUpdateChannel);
        return string.IsNullOrWhiteSpace(url) || url.Contains(LegacyUpdateHost, System.StringComparison.OrdinalIgnoreCase)
          ? CoreDefaultUpdateUrl
          : url;
      }
    }

    private string _pluginsUpdateUrl = PluginsDefaultCatalogUrl;

    /// <summary>
    ///   Online plugin catalog. Only packages it lists appear in "Browse plugins". An empty value, or the upstream catalog
    ///   that lists only .NET Framework plugins, means <see cref="PluginsDefaultCatalogUrl" />.
    /// </summary>
    [JsonProperty]
    public string PluginsUpdateUrl
    {
      get => IsLegacyPluginSource(_pluginsUpdateUrl) ? PluginsDefaultCatalogUrl : _pluginsUpdateUrl;
      set => _pluginsUpdateUrl = value;
    }

    /// <summary>Whether SMA reads the online catalog. <see cref="EnablePluginsUpdates" /> turns it off.</summary>
    [JsonIgnore]
    public bool HasPluginCatalog => EnablePluginsUpdates && !string.IsNullOrWhiteSpace(PluginsUpdateUrl);

    /// <summary>The NuGet feeds that serve plugin packages. A configured list replaces the default feed.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<string> PluginsUpdateNuGetUrls { get; set; } = new HashSet<string>
    {
      PluginsDefaultRepositoryUrl,
    };

    /// <summary>
    ///   The NuGet sources that SMA searches: <see cref="PluginsUpdateNuGetUrls" /> without the upstream 2.x sources, or
    ///   <see cref="PluginsDefaultRepositoryUrl" /> when none is left. Upstream nuget.org packages share the plugin IDs of
    ///   this feed but target .NET Framework, so keeping them would offer versions that cannot load.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> EffectivePluginsNuGetUrls
    {
      get
      {
        var urls = (PluginsUpdateNuGetUrls ?? Enumerable.Empty<string>())
                   .Where(url => !IsLegacyPluginSource(url))
                   .Distinct(System.StringComparer.OrdinalIgnoreCase)
                   .ToList();

        return urls.Count > 0 ? urls : new List<string> { PluginsDefaultRepositoryUrl };
      }
    }

    /// <summary>The CRC32 of the ChangeLog last displayed</summary>
    [JsonProperty]
    public string ChangeLogLastCrc32 { get; set; }

    //
    // Helpers
    [JsonIgnore]
    public bool CoreUpdateChannelIsPrerelease => CoreUpdateChannel != CoreStableChannel;

    #endregion




    #region Methods

    private static bool IsLegacyPluginSource(string url) =>
      string.IsNullOrWhiteSpace(url) || LegacyPluginSources.Any(host => url.Contains(host, System.StringComparison.OrdinalIgnoreCase));

    #endregion




    #region Events

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }
}
