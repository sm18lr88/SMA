// The card editing commands as a command line: list and show elements, set element colors, edit stylesheets and card HTML, back up and restore.
using System.Text;
using SuperMemoAssistant.Themes.Editing;

namespace SuperMemoAssistant.Themes;

/// <summary>
///   Inspects and edits the cards of a SuperMemo collection on disk. SuperMemo keeps the collection in memory while it runs,
///   so every write needs SuperMemo to be closed; each write backs up the files it changes first. Without <c>--apply</c> a
///   command only shows what it would do.
/// </summary>
/// <remarks>
///   Commands: status, collections, list, show, element-color, css (show, set, unset), html (replace, strip-style),
///   theme (list, show, status), backups, restore, snapshot, diff. Global options before the command: <c>--sm-root</c> and <c>--collection</c>. Exit codes: 0 done,
///   1 the command could not be carried out (the reason goes to the error writer), 2 the command line is wrong.
/// </remarks>
public static class CardCommandLine
{
  /// <summary>Runs one command. Output goes to <paramref name="output" />, problems to <paramref name="error" />.</summary>
  public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error) =>
    Run(args, output, error, SafeWriter.RunningSuperMemo, Ansi.Default);

  internal static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error, Func<IReadOnlyList<string>> running, Encoding ansi,
                          Func<DateTime>? now = null)
  {
    if (args.Count == 0 || args[0] is "--help" or "-h" or "help")
    {
      CardOutput.Line(args.Count == 0 ? error : output, CardOutput.Usage);

      return args.Count == 0 ? 2 : 0;
    }

    CardArguments parsed;

    try
    {
      parsed = CardArguments.Parse(args);
    }
    catch (UsageException ex)
    {
      CardOutput.Line(error, $"smcards: {ex.Message}");

      return 2;
    }

    try
    {
      Dispatch(parsed, output, running, ansi, now);

      return 0;
    }
    catch (Exception ex) when (ex is ThemeException or IOException or UnauthorizedAccessException or DecoderFallbackException or EncoderFallbackException)
    {
      CardOutput.Line(error, ex.Message);

      return 1;
    }
  }

  private static void Dispatch(CardArguments a, TextWriter output, Func<IReadOnlyList<string>> running, Encoding ansi, Func<DateTime>? now)
  {
    if (a.Command == "theme list")
    {
      ThemeCommands.List(a, output);

      return;
    }

    if (a.Command == "theme show")
    {
      ThemeCommands.Show(a, output);

      return;
    }

    var root = SuperMemoLocator.FindRoot(a.SmRoot);

    if (a.Command == "collections")
    {
      foreach (var c in SuperMemoLocator.ListCollections(root, ansi))
        CardOutput.Line(output, c);

      return;
    }

    var folder  = SuperMemoLocator.ResolveCollection(root, a.CollectionName, ansi);
    var install = new SuperMemoInstall(Path.Combine(root, "sm20.exe"), folder);
    var writer  = new SafeWriter(install, running, now);

    switch (a.Command)
    {
      case "status":
        CardOutput.Line(output, $"collection: {folder}\nrunning: {(running() is { Count: > 0 } r ? string.Join(", ", r) : "no")}");

        break;
      case "list":
        var collection = CardCollection.Open(folder, ansi);

        CardOutput.Records(output, collection, a.Flag("--templates") ? collection.Templates : a.ToSelection().Apply(collection), a.Flag("--templates"));

        break;
      case "show":
        Show(a, output, folder, ansi);

        break;
      case "element-color":
        ElementColor(a, output, writer, folder, ansi);

        break;
      case "css show" or "css set" or "css unset":
        Css(a, output, writer, root, ansi);

        break;
      case "html replace" or "html strip-style":
        Html(a, output, writer, folder, ansi);

        break;
      case "theme status":
        ThemeCommands.Status(a, output, install);

        break;
      case "backups":
        foreach (var name in new CardBackups(install, ansi).List())
          CardOutput.Line(output, name);

        break;
      case "restore":
        var restored = new CardBackups(install, ansi).RestoreChanges(a.Positionals[0]);

        CardOutput.GuardedWrite(output, writer, new CardEditPlan(restored, []), $"pre-restore-{a.Positionals[0]}", a.Flag("--apply"));

        break;
      case "snapshot":
        CardOutput.Line(output, $"Snapshot saved: {new CardBackups(install, ansi).Snapshot(a.Positionals[0])}");

        break;
      case "diff":
        foreach (var line in new CardBackups(install, ansi).Diff(a.Positionals[0]))
          CardOutput.Line(output, line);

        break;
    }
  }

  private static void Show(CardArguments a, TextWriter output, string folder, Encoding ansi)
  {
    var collection = CardCollection.Open(folder, ansi);

    if (!int.TryParse(a.Positionals[0], out var number))
      throw new ThemeException($"'{a.Positionals[0]}' is not an element number");

    var record = collection.Elements.FirstOrDefault(r => r.Number == number) ?? throw new ThemeException($"no element {number}");

    CardOutput.Element(output, collection, record, ansi);
  }

  private static void ElementColor(CardArguments a, TextWriter output, SafeWriter writer, string folder, Encoding ansi)
  {
    var collection = CardCollection.Open(folder, ansi);
    var records    = a.Flag("--only-templates") ? [] : a.ToSelection().Apply(collection);

    if (a.Flag("--with-templates") || a.Flag("--only-templates"))
      records.AddRange(collection.Templates);

    var plan = CardEdits.ElementColor(collection, records, CardColors.Parse(a.Positionals[0]));

    CardOutput.GuardedWrite(output, writer, plan, "element-color", a.Flag("--apply"));
  }

  private static void Css(CardArguments a, TextWriter output, SafeWriter writer, string root, Encoding ansi)
  {
    var files = CardEdits.StylesheetTargets(root, a.Mode, ansi);

    if (a.Command == "css show")
    {
      foreach (var line in CardEdits.ShowCss(files, a.Positionals.FirstOrDefault(), ansi))
        CardOutput.Line(output, line);

      return;
    }

    var value = a.Command == "css set" ? a.Positionals[2] : null;
    var plan  = CardEdits.Css(files, a.Positionals[0], a.Positionals[1], value, ansi);

    CardOutput.GuardedWrite(output, writer, plan, a.Command.Replace(' ', '-'), a.Flag("--apply"));
  }

  private static void Html(CardArguments a, TextWriter output, SafeWriter writer, string folder, Encoding ansi)
  {
    var files = HtmlEdits.Files(a.ToSelection().Apply(CardCollection.Open(folder, ansi)));
    var edit  = a.Command == "html replace"
      ? HtmlEdits.Replacer(a.Positionals[0], a.Positionals[1], a.Flag("--regex"), a.Flag("--ignore-case"))
      : HtmlEdits.StyleStripper(a.Positionals);

    CardOutput.GuardedWrite(output, writer, HtmlEdits.Plan(files, edit, ansi), a.Command.Replace(' ', '-'), a.Flag("--apply"));
  }
}
