// A small argument parser for the card commands: global options, a command (and a subcommand), options, and positional arguments.
namespace SuperMemoAssistant.Themes.Editing;

/// <summary>What one command accepts. A bad command line throws <see cref="UsageException" />.</summary>
internal sealed record CommandSpec(string Name, int MinPositional, int MaxPositional, string[] Flags, string[] Values, string? Sub = null, string[]? Subs = null);

internal sealed class UsageException(string message) : Exception(message);

internal sealed class CardArguments
{
  private static readonly string[] Selection = ["--elements", "--kind", "--grep", "--template"];

  private static readonly string[] Kinds = ["item", "topic", "concept", "task"];

  private static readonly string[] Modes = ["active", "light", "dark", "both"];

  private static readonly string[] Variants = ["dark", "light"];

  private static readonly CommandSpec[] Commands =
  [
    new("status", 0, 0, [], []),
    new("collections", 0, 0, [], []),
    new("list", 0, 0, ["--templates"], Selection),
    new("show", 1, 1, [], []),
    new("element-color", 1, 1, ["--with-templates", "--only-templates", "--apply"], Selection),
    new("css", 0, 0, [], [], Subs: ["show", "set", "unset"]),
    new("html", 0, 0, [], [], Subs: ["replace", "strip-style"]),
    new("theme", 0, 0, [], [], Subs: ["list", "show", "status"]),
    new("backups", 0, 0, [], []),
    new("restore", 1, 1, ["--apply"], []),
    new("snapshot", 1, 1, [], []),
    new("diff", 1, 1, [], []),
  ];

  private static readonly Dictionary<string, CommandSpec> SubCommands = new()
  {
    ["css show"]          = new("css show", 0, 1, [], ["--mode"]),
    ["css set"]           = new("css set", 3, 3, ["--apply"], ["--mode"]),
    ["css unset"]         = new("css unset", 2, 2, ["--apply"], ["--mode"]),
    ["html replace"]      = new("html replace", 2, 2, ["--regex", "--ignore-case", "--apply"], Selection),
    ["html strip-style"]  = new("html strip-style", 1, -1, ["--apply"], Selection),
    ["theme list"]        = new("theme list", 0, 0, [], ["--search", "--variant", "--source", "--user-library"]),
    ["theme show"]        = new("theme show", 1, 1, [], ["--user-library"]),
    ["theme status"]      = new("theme status", 0, 0, [], ["--user-library"]),
  };

  private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
  private readonly HashSet<string>            _flags  = [];

  private CardArguments() { }

  public string? SmRoot { get; private set; }

  public string? CollectionName { get; private set; }

  /// <summary>The command and subcommand, for example "css set".</summary>
  public string Command { get; private set; } = "";

  public List<string> Positionals { get; } = [];

  public bool Flag(string name) => _flags.Contains(name);

  public string? Value(string name) => _values.GetValueOrDefault(name);

  public CardSelection ToSelection() => new(Value("--elements"), Value("--kind"), Value("--grep"), Value("--template"));

  public CssMode Mode => Value("--mode") switch { "light" => CssMode.Light, "dark" => CssMode.Dark, "both" => CssMode.Both, _ => CssMode.Active };

  public static CardArguments Parse(IReadOnlyList<string> args)
  {
    var parsed = new CardArguments();
    var i      = 0;

    while (i < args.Count && args[i].StartsWith("--", StringComparison.Ordinal))
    {
      var (name, inline) = Split(args[i]);

      if (name is not ("--sm-root" or "--collection"))
        throw new UsageException($"unrecognized argument {args[i]}");

      var value = inline ?? (i + 1 < args.Count ? args[++i] : throw new UsageException($"{name} needs a value"));

      if (name == "--sm-root")
        parsed.SmRoot = value;
      else
        parsed.CollectionName = value;

      i++;
    }

    if (i >= args.Count)
      throw new UsageException("a command is required");

    var spec = Commands.FirstOrDefault(c => c.Name == args[i]) ?? throw new UsageException($"unknown command '{args[i]}'");

    parsed.Command = spec.Name;
    i++;

    if (spec.Subs is not null)
    {
      var sub = i < args.Count ? args[i] : throw new UsageException($"{spec.Name} needs one of: {string.Join(", ", spec.Subs)}");

      if (!spec.Subs.Contains(sub))
        throw new UsageException($"unknown {spec.Name} command '{sub}'");

      spec            = SubCommands[$"{spec.Name} {sub}"];
      parsed.Command = spec.Name;
      i++;
    }

    parsed.Fill(spec, args.Skip(i).ToList());

    return parsed;
  }

  private static (string Name, string? Inline) Split(string argument)
  {
    var at = argument.IndexOf('=');

    return at > 0 ? (argument[..at], argument[(at + 1)..]) : (argument, null);
  }

  private void Fill(CommandSpec spec, List<string> rest)
  {
    for (var i = 0; i < rest.Count; i++)
    {
      var (name, inline) = Split(rest[i]);

      if (!rest[i].StartsWith("--", StringComparison.Ordinal))
      {
        Positionals.Add(rest[i]);

        continue;
      }

      if (spec.Flags.Contains(name))
      {
        _flags.Add(name);
      }
      else if (spec.Values.Contains(name))
      {
        var value = inline ?? (i + 1 < rest.Count ? rest[++i] : throw new UsageException($"{name} needs a value"));

        CheckChoice(name, value);
        _values[name] = value;
      }
      else
      {
        throw new UsageException($"unrecognized argument {rest[i]}");
      }
    }

    if (Positionals.Count < spec.MinPositional || (spec.MaxPositional >= 0 && Positionals.Count > spec.MaxPositional))
      throw new UsageException($"{spec.Name} takes {Describe(spec)} argument(s), got {Positionals.Count}");
  }

  private static string Describe(CommandSpec spec) =>
    spec.MaxPositional < 0 ? $"{spec.MinPositional} or more" : spec.MinPositional == spec.MaxPositional ? $"{spec.MinPositional}" : $"{spec.MinPositional} to {spec.MaxPositional}";

  private static void CheckChoice(string name, string value)
  {
    var allowed = name switch { "--kind" => Kinds, "--mode" => Modes, "--variant" => Variants, _ => null };

    if (allowed is not null && !allowed.Contains(value))
      throw new UsageException($"argument {name}: invalid choice '{value}' (choose from {string.Join(", ", allowed)})");
  }
}
