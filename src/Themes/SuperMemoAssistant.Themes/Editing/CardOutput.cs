// The text the card commands print: element listings, a single element with its HTML, and the dry-run lines of a write.
using System.Text;

namespace SuperMemoAssistant.Themes.Editing;

internal static class CardOutput
{
  public static void Records(TextWriter output, CardCollection collection, IEnumerable<CardRecord> records, bool templates)
  {
    Line(output, $"{"#",5} {"kind",-8} {"template",-12} {"el.color",-9} components | title");

    foreach (var r in records)
    {
      var components = string.Join(", ", r.Components.Select(Describe));
      var template   = templates ? "" : collection.TemplateName(r.TemplateId);

      Line(output, $"{r.Number,5} {r.Kind,-8} {Cut(template, 12),-12} {CardColors.ToText(r.Color),-9} {components} | {Cut(r.Title, 70)}");
    }
  }

  private static string Describe(CardComponent c) =>
    c.Kind
    + (c.Path is null ? "" : $"[{Path.GetFileName(Path.GetDirectoryName(c.Path))}/{Path.GetFileName(c.Path)}]")
    + (c.InlineText is null ? "" : "[inline]")
    + (c.Color is null ? "" : $"({CardColors.ToText(c.Color)})");

  private static string Cut(string text, int length) => text.Length > length ? text[..length] : text;

  public static void Element(TextWriter output, CardCollection collection, CardRecord record, Encoding ansi)
  {
    Records(output, collection, [record], false);

    foreach (var c in record.Components)
    {
      Line(output, $"\n-- component {c.Index}: {c.Kind}, display-at=0x{c.DisplayAt:x2}, file={c.Path ?? "None"}");

      if (c.Path is not null && File.Exists(c.Path))
        Line(output, HtmlEdits.Read(c.Path, ansi).Text);
      else if (c.InlineText is not null)
        Line(output, $"[inline registry text, edit in SuperMemo] {c.InlineText}");
    }
  }

  /// <summary>Shows what a plan would change, then writes it (backing up first) when <paramref name="apply" /> is set.</summary>
  public static void GuardedWrite(TextWriter output, SafeWriter writer, CardEditPlan plan, string label, bool apply)
  {
    foreach (var note in plan.Notes)
      Line(output, note);

    var pending = SafeWriter.Pending(plan.Changes);

    if (pending.Count == 0)
    {
      Line(output, "Nothing to change.");

      return;
    }

    foreach (var change in pending)
      Line(output, $"  will modify: {change.Key}");

    if (!apply)
    {
      Line(output, $"DRY RUN: {pending.Count} file(s) would change. Re-run with --apply to write.");

      return;
    }

    var written = writer.Write(pending, label, out var backup);

    Line(output, $"Wrote {written} file(s). Backup: {backup}");
  }

  public static void Line(TextWriter output, string text) => output.Write(text + "\n");

  public const string Usage = """
    Usage: sma-cards [--sm-root <SuperMemo folder>] [--collection <folder or name part>] <command> [options]

    SuperMemo is found from --sm-root, else the SMCARDS_ROOT variable, else the sm20.exe that SMA is set up with, else the
    program registered for .kno files.

    A write only shows what it would do unless --apply is given. --apply needs SuperMemo to be closed, and backs up
    every file it changes first (in <SuperMemo folder>\smcards-backups).

    Commands:
      status                                  whether SuperMemo runs, and the selected collection
      collections                             list the collections
      list [--templates]                      list elements (or templates) with colors and card files
      show <number>                           one element's components and card HTML
      element-color <#RRGGBB|default>         set the element window background
          [--with-templates | --only-templates] [--apply]
      css show [selector]                     show the card stylesheet rules
      css set <selector> <property> <value>   set a property (for example BODY background-color #1E1E2E)
      css unset <selector> <property>         remove a property
          [--mode active|light|dark|both] [--apply]
      html replace <find> <replacement>       find and replace in card HTML
          [--regex] [--ignore-case] [--apply]
      html strip-style <property>...          remove inline CSS properties, such as pasted colors
          [--apply]
      theme list [--search <text>] [--variant dark|light] [--source <text>]
                                              list the palette library (the shipped palettes and your imports)
      theme show <id or name>                 the color roles of one theme
      theme status                            what sm20.exe holds now: styles, active themes, switches
          [--user-library <file>]             (the library defaults to the file of the Themes plugin)
      backups                                 list the backups that this tool made
      restore <name> [--apply]                put a backup back
      snapshot <name>                         save the state before a change you make in SuperMemo
      diff <name>                             show the bytes that changed since a snapshot

    Selecting elements (list, element-color, html):
      --elements 53,57,60-81   --kind item|topic|concept|task   --grep <title text>   --template <name>

    Exit codes: 0 done, 1 the command could not be carried out, 2 the command line is wrong.
    """;
}
