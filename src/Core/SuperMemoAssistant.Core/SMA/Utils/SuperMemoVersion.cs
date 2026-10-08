// Reads SuperMemo's version from its executable's version resource.
namespace SuperMemoAssistant.SMA.Utils
{
  using System;
  using System.Diagnostics;

  public static class SuperMemoVersion
  {
    private static readonly Version Fallback = new(20, 0);

    public static Version Of(string smExePath)
    {
      var info = FileVersionInfo.GetVersionInfo(smExePath);
      return info.FileMajorPart > 0 ? new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart) : Fallback;
    }
  }
}
