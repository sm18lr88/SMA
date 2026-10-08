// Reads JSON that has comments and trailing commas (VS Code theme and package files), and decodes text files strictly as UTF-8.
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes.Import;

internal static partial class Jsonc
{
  [GeneratedRegex(@"(""(?:\\.|[^""\\])*"")|//[^\n]*|/\*.*?\*/", RegexOptions.Singleline)]
  private static partial Regex Comments();

  [GeneratedRegex(@",(\s*[}\]])")]
  private static partial Regex TrailingComma();

  private static readonly UTF8Encoding Strict = new(false, true);

  /// <summary>Strips comments (not inside strings) and trailing commas, then parses.</summary>
  public static JsonElement Parse(string text)
  {
    var stripped = Comments().Replace(text, m => m.Groups[1].Success ? m.Groups[1].Value : "");

    stripped = TrailingComma().Replace(stripped, "$1");

    using var document = JsonDocument.Parse(stripped);

    return document.RootElement.Clone();
  }

  /// <summary>Reads a UTF-8 file, throwing when it holds bytes that are not UTF-8. A byte order mark is dropped.</summary>
  public static string ReadFile(string path)
  {
    var text = Strict.GetString(File.ReadAllBytes(path));

    return text.TrimStart('\uFEFF');
  }

  public static string? String(JsonElement obj, string name) =>
    obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
