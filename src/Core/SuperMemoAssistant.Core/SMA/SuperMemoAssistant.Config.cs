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




namespace SuperMemoAssistant.SMA
{
  using System;
  using System.IO;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Configs;
  using Extensions;
  using Interop.SuperMemo.Core;
  using Process.NET.Windows;
  using Services.Configuration;

  public partial class SMA
  {
    #region Constants & Statics

    private static CoreCfg CoreConfig => Core.CoreConfig;

    #endregion




    #region Properties & Fields - Public

    public CollectionCfg CollectionConfig { get; set; }

    #endregion




    #region Methods

    private void ApplySuperMemoWindowStyles()
    {
      if (CollectionConfig.CollapseElementWdwTitleBar)
        Task.Run(async () =>
        {
          var deadline = DateTime.UtcNow.AddSeconds(30);
          IntPtr handle;
          while (((handle = _sm?.UI.ElementWdw.Handle ?? IntPtr.Zero) == IntPtr.Zero || IsWindowVisible(handle) == false)
                 && DateTime.UtcNow < deadline)
            await Task.Delay(100).ConfigureAwait(false);

          if (handle != IntPtr.Zero)
            WindowStyling.MakeWindowTitleless(handle);
        }).RunAsync();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    private async Task LoadConfigAsync(SMCollection collection)
    {
      Core.CollectionConfiguration = new CollectionConfigurationService(collection, "Core");

      // CollectionsCfg
      CollectionConfig = await Core.CollectionConfiguration.LoadAsync<CollectionCfg>().ConfigureAwait(false) ?? new CollectionCfg();
    }

    public async Task SaveConfigAsync()
    {
      try
      {
        var tasks = new[]
        {
          Core.Configuration.SaveAsync<CoreCfg>(CoreConfig),
          Core.CollectionConfiguration.SaveAsync<CollectionCfg>(CollectionConfig),
        };

        await Task.WhenAll(tasks).ConfigureAwait(false);
      }
      catch (IOException ex)
      {
        if (ex.Message.StartsWith("The process cannot access the file", StringComparison.OrdinalIgnoreCase))
          LogTo.Warning(ex, "Failed to save config files in SMA.SaveConfig");

        else
          LogTo.Error(ex, "Failed to save config files in SMA.SaveConfig");
      }
    }

    #endregion
  }
}
