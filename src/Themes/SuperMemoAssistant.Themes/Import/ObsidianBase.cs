// Obsidian's default variables, which an Obsidian theme builds on: a cache, else an installed Obsidian, else built-in approximations.
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace SuperMemoAssistant.Themes.Import;

internal static class ObsidianBase
{
  /// <summary>Approximate defaults, written for this project and not copied from Obsidian. Themes override most of these; they only fill gaps.</summary>
  public const string FallbackCss = """

    body {
      --color-red: #e93147; --color-orange: #e9973f; --color-yellow: #e0ac00;
      --color-green: #08b94e; --color-cyan: #00bfbc; --color-blue: #086ddd; --color-purple: #7852ee;
      --background-primary: var(--color-base-00);
      --background-primary-alt: var(--color-base-10);
      --background-secondary: var(--color-base-20);
      --background-modifier-border: var(--color-base-30);
      --background-modifier-form-field: var(--color-base-00);
      --interactive-normal: var(--color-base-00);
      --interactive-accent: var(--color-accent);
      --text-normal: var(--color-base-100);
      --text-muted: var(--color-base-70);
      --text-faint: var(--color-base-50);
    }
    .theme-dark {
      --color-base-00: #1e1e1e; --color-base-10: #242424; --color-base-20: #262626;
      --color-base-30: #363636; --color-base-50: #666666; --color-base-70: #bababa;
      --color-base-100: #dadada; --color-accent: #7b6cd9;
      --text-selection: rgba(123, 108, 217, 0.25);
      --interactive-normal: var(--color-base-30);
      --background-modifier-form-field: var(--color-base-30);
    }
    .theme-light {
      --color-base-00: #ffffff; --color-base-10: #f6f6f6; --color-base-20: #fafafa;
      --color-base-30: #e0e0e0; --color-base-50: #ababab; --color-base-70: #5c5c5c;
      --color-base-100: #222222; --color-accent: #8a5cf5;
      --text-selection: rgba(138, 92, 245, 0.25);
      --interactive-normal: var(--color-base-00);
      --background-modifier-form-field: var(--color-base-00);
    }

    """;

  public const string NotInstalledNote = "Obsidian was not found; built-in approximate defaults were used (set OBSIDIAN_ASAR to its obsidian.asar for exact ones).";

  /// <summary>obsidian.asar from $OBSIDIAN_ASAR or a standard Windows install location, or null.</summary>
  public static string? FindAsar()
  {
    var candidates = new List<string?> { Environment.GetEnvironmentVariable("OBSIDIAN_ASAR") };

    foreach (var variable in new[] { "LOCALAPPDATA", "ProgramFiles", "ProgramFiles(x86)" })
    {
      var root = Environment.GetEnvironmentVariable(variable);

      if (!string.IsNullOrEmpty(root))
        candidates.Add(Path.Combine(root, variable == "LOCALAPPDATA" ? @"Programs\Obsidian" : "Obsidian", "resources", "obsidian.asar"));
    }

    return candidates.FirstOrDefault(c => !string.IsNullOrEmpty(c) && File.Exists(c));
  }

  /// <summary>The cached defaults, else the ones read from an installed Obsidian (and cached), else the built-in ones. <paramref name="note" /> says when the built-in ones were used.</summary>
  public static string Load(string? cachePath, out string? note)
  {
    note = null;

    if (cachePath is not null && File.Exists(cachePath))
      return ReadText(cachePath);

    if (FindAsar() is { } asar)
    {
      try
      {
        var css = Distill(ReadAppCss(asar));

        if (cachePath is not null)
        {
          Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cachePath))!);
          File.WriteAllText(cachePath, css, new UTF8Encoding(false));
        }

        return css;
      }
      catch (Exception ex) when (ex is IOException or KeyNotFoundException or JsonException or FormatException or InvalidDataException or UnauthorizedAccessException)
      {
        // an unreadable asar counts as not installed
      }
    }

    note = NotInstalledNote;

    return FallbackCss;
  }

  private static string ReadText(string path) => new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n");

  /// <summary>app.css from an Electron asar archive: a pickled header (sizes and a JSON file table), then the file data.</summary>
  public static string ReadAppCss(string asarPath)
  {
    using var stream = File.OpenRead(asarPath);
    var       prefix = new byte[16];

    stream.ReadExactly(prefix);

    var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(prefix.AsSpan(4));
    var jsonLength = BinaryPrimitives.ReadUInt32LittleEndian(prefix.AsSpan(12));
    var json       = new byte[jsonLength];

    stream.ReadExactly(json);

    using var table  = JsonDocument.Parse(json);
    var       entry  = table.RootElement.GetProperty("files").GetProperty("app.css");
    var       offset = long.Parse(entry.GetProperty("offset").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
    var       size   = entry.GetProperty("size").GetInt32();
    var       data   = new byte[size];

    stream.Seek(8 + headerSize + offset, SeekOrigin.Begin);
    stream.ReadExactly(data);

    return new UTF8Encoding(false, true).GetString(data);
  }

  /// <summary>Keeps only the body-level custom properties of a stylesheet: the defaults that themes build on.</summary>
  public static string Distill(string css)
  {
    var keep = new List<string>();

    foreach (var (selector, body) in CssSheet.Rules(css))
    {
      var parts        = CssSheet.SplitTop(selector).Select(p => p.Trim()).Where(p => CssSheet.BodyClasses(p) is not null).ToList();
      var declarations = CssSheet.CustomProperties(body).Select(kv => $"{kv.Key}: {kv.Value};").ToList();

      if (parts.Count > 0 && declarations.Count > 0)
        keep.Add($"{string.Join(", ", parts)} {{\n  " + string.Join("\n  ", declarations) + "\n}");
    }

    return string.Join("\n", keep);
  }
}
