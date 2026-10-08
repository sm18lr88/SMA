// Loads the golden JSON in the Golden folder (see Golden\README.md) and finds the optional real sm20.exe files.
using System.Text.Json;

namespace SuperMemoAssistant.Themes.Tests;

internal static class GoldenData
{
  public static JsonElement Load(string name)
  {
    var path = Path.Combine(AppContext.BaseDirectory, "Golden", name);

    return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
  }

  public static int[] Ints(JsonElement array) => array.EnumerateArray().Select(e => e.GetInt32()).ToArray();

  public static double[] Doubles(JsonElement array) => array.EnumerateArray().Select(e => e.GetDouble()).ToArray();

  public static string? Sm20Path => SuperMemoLocation.Exe;

  public static string? Sm20OriginalPath => SuperMemoLocation.OriginalExe;
}
