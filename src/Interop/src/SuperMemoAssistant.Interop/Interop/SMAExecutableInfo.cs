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




namespace SuperMemoAssistant.Interop
{
  using System.Reflection;
  using System.Text.RegularExpressions;
  using global::Extensions.System.IO;

  /// <summary>Contains information about the current executable's assembly</summary>
  public class SMAExecutableInfo
  {
    #region Constants & Statics

    // Installed: %LocalAppData%\SuperMemoAssistant\current (Velopack). Development: any ...\app-dev folder (artifacts\app-dev).
    private static readonly Regex EntryAssembly =
      new(@"/(?<folder>[^/]+)/(?<exe>SuperMemoAssistant|PluginHost)\.(?:exe|dll)$", RegexOptions.IgnoreCase);

    /// <summary>The <see cref="SMAExecutableInfo" /> singleton</summary>
    public static SMAExecutableInfo Instance { get; } = new SMAExecutableInfo();

    #endregion




    #region Constructors

    private SMAExecutableInfo()
    {
      var entryAssemblyFilePath = new FilePath(Assembly.GetEntryAssembly().Location);
      var match                 = EntryAssembly.Match(entryAssemblyFilePath.FullPath);

      DirectoryPath = entryAssemblyFilePath.Directory;

      if (!match.Success)
        return;

      var folder = match.Groups["folder"].Value;
      IsDev              = folder.Equals("app-dev", System.StringComparison.OrdinalIgnoreCase);
      IsPathLocalAppData = folder.Equals("current", System.StringComparison.OrdinalIgnoreCase)
                        && DirectoryPath.Parent.FullPath.Equals(SMAFileSystem.AppRootDir.FullPath, System.StringComparison.OrdinalIgnoreCase);

      ExecutableType = match.Groups["exe"].Value.Equals("PluginHost", System.StringComparison.OrdinalIgnoreCase)
        ? SMAExecutableType.PluginHost
        : SMAExecutableType.SuperMemoAssistant;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The directory which contains the executing assembly's exe</summary>
    public DirectoryPath DirectoryPath { get; }

    /// <summary>Which SMA executable is currently hosting this dll -- internal use only (PluginHost, SMA)</summary>
    public SMAExecutableType ExecutableType { get; } = SMAExecutableType.Unknown;

    /// <summary>Whether the current SMA executable is a development version</summary>
    public bool IsDev { get; } = false;

    /// <summary>Whether SMA is installed in %LocalAppData%</summary>
    public bool IsPathLocalAppData { get; } = false;

    #endregion
  }

  /// <summary>Defines the different type of executables for SMA (plugin, SMA core, ..)</summary>
  public enum SMAExecutableType
  {
    /// <summary>Other executable</summary>
    Unknown,
    /// <summary>SuperMemoAssistant.exe</summary>
    SuperMemoAssistant,
    /// <summary>PluginHost.exe</summary>
    PluginHost,
  }
}
