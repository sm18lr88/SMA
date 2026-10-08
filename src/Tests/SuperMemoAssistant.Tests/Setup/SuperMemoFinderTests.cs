namespace SuperMemoAssistant.Tests.Setup;

using SuperMemoAssistant.SMA.Utils;
using Xunit;

public sealed class SuperMemoFinderTests
{
  [Fact]
  public void DefaultLocationSearchReturnsOnlyExistingExecutables()
  {
    var found = SuperMemoFinder.SearchSuperMemoInDefaultLocations();

    Assert.NotNull(found);
    Assert.All(found, path =>
    {
      Assert.True(Path.IsPathFullyQualified(path.FullPath), $"{path.FullPath} is not an absolute path.");
      Assert.True(File.Exists(path.FullPath), $"{path.FullPath} does not exist.");
    });
  }
}
