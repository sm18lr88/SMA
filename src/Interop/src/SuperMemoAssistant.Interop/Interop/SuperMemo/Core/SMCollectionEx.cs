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




namespace SuperMemoAssistant.Interop.SuperMemo.Core
{
  using System.IO;
  using global::Extensions.System.IO;

  /// <summary>Extension methods for <see cref="SMCollection" /></summary>
  public static class SMCollectionEx
  {
    #region Methods

    /// <summary>
    ///   Gets the root directory which contains the collection data (not the folder which contains the .kno)
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static string GetRootDirPath(this SMCollection collection)
    {
      return Path.Combine(collection.Path, collection.Name);
    }

    /// <summary>Combines the given path with the root directory of the collection</summary>
    /// <seealso cref="GetRootDirPath(SMCollection)" />
    /// <param name="collection"></param>
    /// <param name="paths"></param>
    /// <returns></returns>
    public static string CombinePath(
      this   SMCollection collection,
      params string[]     paths)
    {
      return Path.Combine(collection.Path,
                          collection.Name,
                          Path.Combine(paths));
    }

    /// <summary>Combines the given path with the collection's element folder</summary>
    /// <param name="collection"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetElementFilePath(
      this SMCollection collection,
      string            filePath)
    {
      return collection.CombinePath(SMConst.Paths.ElementsFolder, filePath);
    }

    /// <summary>Returns the path to the SMA folder under the collection's root folder</summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static string GetSMAFolder(
      this SMCollection collection)
    {
      return collection.CombinePath(SMAFileSystem.CollectionSMAFolder);
    }

    /// <summary>Returns the config folder path under the sma collection folder</summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static string GetSMAConfigsFolder(
      this SMCollection collection)
    {
      return collection.CombinePath(SMAFileSystem.CollectionSMAFolder,
                                    SMAFileSystem.ConfigsFolder);
    }


    /// <summary>Returns a plugin's config folder path under the sma collection config folder</summary>
    /// <param name="collection"></param>
    /// <param name="subFolder"></param>
    /// <returns></returns>
    public static string GetSMAConfigsSubFolder(
      this SMCollection collection,
      string            subFolder)
    {
      return collection.CombinePath(SMAFileSystem.CollectionSMAFolder,
                                    SMAFileSystem.ConfigsFolder,
                                    subFolder);
    }

    /// <summary>Returns the path to the .kno file for this collection</summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static string GetKnoFilePath(this SMCollection collection)
    {
      return Path.Combine(collection.Path,
                          collection.Name + ".Kno");
    }

    /// <summary>Makes <paramref name="absolutePath" /> relative to the collection's path</summary>
    /// <seealso cref="GetRootDirPath(SMCollection)" />
    /// <param name="collection"></param>
    /// <param name="absolutePath"></param>
    /// <returns></returns>
    public static string MakeRelative(this SMCollection collection,
                                      string            absolutePath)
    {
      string basePath = collection.CombinePath();

      return absolutePath.StartsWith(basePath, System.StringComparison.InvariantCultureIgnoreCase)
        ? absolutePath.Substring(basePath.Length).TrimStart('\\', '/')
        : absolutePath;
    }

    /// <summary>
    /// Checks whether the collection exists or not.
    /// </summary>
    /// <param name="collection">The collection to check</param>
    /// <returns>Whether the collection exists or not.</returns>
    public static bool Exists(this SMCollection collection)
    {
      var knoFilePath = new FilePath(collection.GetKnoFilePath());

      return knoFilePath.Exists() && Directory.Exists(collection.GetRootDirPath());
    }

    /// <summary>
    /// Checks whether the collection is locked or not.
    /// </summary>
    /// <param name="collection">The collection to check</param>
    /// <returns>Whether the collection is locked or not.</returns>
    public static bool IsLocked(this SMCollection collection)
    {
      var knoFilePath = new FilePath(collection.GetKnoFilePath());

      return knoFilePath.IsLocked();
    }

    #endregion
  }
}
