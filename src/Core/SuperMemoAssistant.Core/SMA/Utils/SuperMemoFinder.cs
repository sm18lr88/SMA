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
// Modified On:  2020/04/09 15:08
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.SMA.Utils
{
  using System;
  using System.Collections.Generic;
  using System.Data.OleDb;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Configs;
  using Exceptions;
  using Extensions;
  using global::SuperMemoAssistant.Hooks.Symbols;
  using global::Extensions.System.IO;
  using SuperMemo;
  using Sys.Windows.Search;

  public static class SuperMemoFinder
  {
    #region Constants & Statics

    private static readonly string[]        SuperMemoFolderNames  = { "SuperMemo", "SuperMemo20" };
    private static readonly string[]        SuperMemoExeFileNames = { "sm20.exe" };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SymbolTable> SymbolCache = new();

    #endregion




    #region Methods

    /// <summary>Lists the SuperMemo executables in the usual install folders. Never returns null.</summary>
    public static List<FilePath> SearchSuperMemoInDefaultLocations()
    {
      var smExePaths = new List<FilePath>();

      foreach (var rootDirPath in ListRootDirPaths())
      foreach (var smFolderName in SuperMemoFolderNames)
      foreach (var smExeFileName in SuperMemoExeFileNames)
        try
        {
          var smExePath = new FilePath(Path.Combine(rootDirPath, smFolderName, smExeFileName));

          if (smExePath.Exists())
            smExePaths.Add(smExePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
          LogTo.Warning(ex, "Could not check {RootDir} for a SuperMemo executable", rootDirPath);
        }

      return smExePaths;
    }
    public static async Task<List<FilePath>> SearchSuperMemoInWindowsIndexAsync()
    {
      if (WindowsSearch.Instance.IsAvailable == false)
      {
        LogTo.Warning("Windows search is unavailable. Searching for SuperMemo executable aborted");
        return new List<FilePath>();
      }

      // The index can fail a query even when it is available (service busy or rebuilding). It is only a convenience, so a
      // failure must not surface as an error: the other ways to find SuperMemo still work.
      try
      {
        var wsRes = await WindowsSearch.Instance.SearchAsync("sm20.exe", WindowsSearchKinds.Program)
                                       .ConfigureAwait(false);

        return wsRes.Select(wsr => new FilePath(wsr.FilePath))
                    .ToList();
      }
      catch (Exception ex) when (ex is OleDbException or InvalidOperationException)
      {
        LogTo.Warning(ex, "Windows search failed. Searching for SuperMemo executable aborted");
        return new List<FilePath>();
      }
    }

    /// <summary>
    ///   Validates a SuperMemo executable: it must be readable, not running, and a SuperMemo 20 x64 build whose native symbols
    ///   resolve. <paramref name="symbols" /> receives the resolved (and, for verified builds, pinned-checked) symbol table.
    /// </summary>
    public static bool CheckSuperMemoExecutable(
      FilePath         smFile,
      out SymbolTable  symbols,
      out SMAException ex)
    {
      symbols = null;

      if (smFile == null)
      {
        ex = new SMAException("SM exe file path is null", new ArgumentNullException(nameof(smFile)));
        return false;
      }

      if (smFile.Exists() == false)
      {
        ex = new SMAException(
          $"Invalid file path for sm executable file: '{smFile}' could not be found. SMA cannot continue.");
        return false;
      }

      if (smFile.IsLocked())
      {
        ex = new SMAException($"{smFile.FullPath} is locked. Make sure it isn't already running.");
        return false;
      }

      try
      {
        var key = smFile.FullPath.ToLowerInvariant() + "|" + File.GetLastWriteTimeUtc(smFile.FullPath).Ticks;
        symbols = SymbolCache.GetOrAdd(key, _ => SymbolResolver.ResolveVerified(smFile.FullPath));
        ex      = null;
        return true;
      }
      catch (SymbolResolutionException resolutionEx)
      {
        ex = new SMAException(
          $"{smFile.FullPath} is not a supported SuperMemo build. SMA requires SuperMemo 20 (64-bit). {resolutionEx.Message}",
          resolutionEx);
        return false;
      }
      catch (IOException ioEx)
      {
        ex = new SMAException($"SMA needs read access to {smFile.FullPath}.", ioEx);
        return false;
      }
      catch (UnauthorizedAccessException accessEx)
      {
        ex = new SMAException($"SMA needs read access to {smFile.FullPath}.", accessEx);
        return false;
      }
    }

    /// <summary>
    ///   Folders where users install SuperMemo, and the root of every ready drive. Special folders that do not exist on this
    ///   account (for example, no Desktop folder) come back empty and are skipped.
    /// </summary>
    private static IEnumerable<string> ListRootDirPaths()
    {
      var userRootDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

      var folderPaths = new List<string>
      {
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        userRootDir,
      };

      if (string.IsNullOrEmpty(userRootDir) == false)
        folderPaths.AddRange(new[] { "Google Drive", "Dropbox", "OneDrive" }.Select(dir => Path.Combine(userRootDir, dir)));

      folderPaths.AddRange(DriveInfo.GetDrives().Where(drive => drive.IsReady).Select(drive => drive.RootDirectory.FullName));

      return folderPaths.Where(path => string.IsNullOrWhiteSpace(path) == false)
                        .Distinct(StringComparer.OrdinalIgnoreCase);
    }
    #endregion
  }
}
