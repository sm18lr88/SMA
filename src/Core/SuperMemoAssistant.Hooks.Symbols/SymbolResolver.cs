// Entry point: resolves a SymbolTable from sm20.exe and cross-checks it against the pinned table for verified builds.
namespace SuperMemoAssistant.Hooks.Symbols;

using System.Reflection;
using SuperMemoAssistant.SuperMemo;

public static class SymbolResolver
{
  /// <summary>Build identity used to key pinned tables (machine, link timestamp, code size).</summary>
  public static string BuildIdOf(string exePath) => new PeImage(File.ReadAllBytes(exePath)).BuildId;

  /// <summary>Resolves every symbol SMA needs, or throws listing each symbol that could not be resolved.</summary>
  public static SymbolTable Resolve(string exePath)
  {
    var pe     = new PeImage(File.ReadAllBytes(exePath));
    var rtti   = new DelphiRtti(pe);
    var code   = new CodeShapes(pe);
    var errors = new List<string>();

    var methods  = new Dictionary<NativeMethod, MethodSymbol>();
    var pointers = new Dictionary<NativePointer, long>();

    DelphiMethod Method(string cls, string name)
    {
      var hits = rtti.Vmts(cls)
                     .SelectMany(rtti.Methods)
                     .Where(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && pe.IsCode(m.CodeVa))
                     .DistinctBy(m => m.CodeVa)
                     .ToList();

      return hits.Count == 1 ? hits[0]
        : throw new SymbolResolutionException($"{cls}.{name}: expected one implementation in RTTI, found {hits.Count}.");
    }

    void Try(string what, Action action)
    {
      try { action(); }
      catch (SymbolResolutionException ex) { errors.Add($"{what}: {ex.Message}"); }
    }

    foreach (var (key, (cls, name)) in Sm20Rules.RttiMethods)
      Try(key.ToString(), () =>
      {
        var m = Method(cls, name);
        methods[key] = new MethodSymbol(pe.ToRva(m.CodeVa), m.ReturnKind);
      });

    foreach (var (key, (cls, field)) in Sm20Rules.RttiFields)
      Try(key.ToString(), () => pointers[key] = Field(rtti, cls, field));

    Try("TComponentDataRecord", () =>
      pointers[NativePointer.ElWdw_ComponentData_ComponentDataArrItemLength] =
        (rtti.Record("TComponentDataRecord") ?? throw new SymbolResolutionException("record type not found")).Size);

    var shapes = new Sm20CodeShapes(rtti, code, (cls, name) => Method(cls, name).CodeVa);

    Try("Form globals", () =>
    {
      foreach (var (key, va) in shapes.FormGlobals())
        pointers[key] = pe.ToRva(va);
    });
    Try(nameof(NativePointer.Database_InstancePtr), () => pointers[NativePointer.Database_InstancePtr] = pe.ToRva(shapes.Database()));
    Try(nameof(NativePointer.Globals_LimitChildrenCountPtr),
        () => pointers[NativePointer.Globals_LimitChildrenCountPtr] = pe.ToRva(shapes.LimitChildrenCount()));
    Try(nameof(NativePointer.Globals_IgnoreUserConfirmationPtr),
        () => pointers[NativePointer.Globals_IgnoreUserConfirmationPtr] = pe.ToRva(shapes.IgnoreUserConfirmation()));
    Try("Current concept", () =>
    {
      var record = shapes.CurrentConceptRecord();
      var layout = rtti.Record("TConcept") ?? throw new SymbolResolutionException("TConcept record type not found");

      foreach (var (key, field) in Sm20Rules.ConceptRecordFields)
        pointers[key] = pe.ToRva(record) + layout.Fields[field];
    });
    Try(nameof(NativeMethod.ElWdw_Done), () => methods[NativeMethod.ElWdw_Done] = new MethodSymbol(pe.ToRva(shapes.Done()), NativeReturnKind.None));
    Try(nameof(NativeMethod.ElWdw_RestoreLearningMode),
        () => methods[NativeMethod.ElWdw_RestoreLearningMode] = new MethodSymbol(pe.ToRva(shapes.RestoreLearningMode()), NativeReturnKind.None));

    if (errors.Count > 0)
      throw new SymbolResolutionException("Unresolved SuperMemo symbols:" + Environment.NewLine + string.Join(Environment.NewLine, errors));

    return new SymbolTable(pe.BuildId, methods, pointers);
  }

  /// <summary>The checked-in table for a verified build, or null for builds that were never verified.</summary>
  public static SymbolTable? Pinned(string buildId)
  {
    using var stream = typeof(SymbolResolver).Assembly.GetManifestResourceStream($"Pinned.{buildId}.json");
    if (stream is null)
      return null;

    using var reader = new StreamReader(stream);
    return SymbolTable.FromJson(reader.ReadToEnd());
  }

  /// <summary>Resolves symbols and, when a pinned table exists for this build, requires an exact match.</summary>
  public static SymbolTable ResolveVerified(string exePath)
  {
    var resolved = Resolve(exePath);
    var pinned   = Pinned(resolved.BuildId);

    if (pinned is null)
      return resolved;

    var diff = resolved.Diff(pinned);
    return diff.Count == 0 ? resolved
      : throw new SymbolResolutionException("Resolved symbols differ from the verified table:" + Environment.NewLine + string.Join(Environment.NewLine, diff));
  }

  public static IEnumerable<string> PinnedBuildIds() =>
    typeof(SymbolResolver).Assembly.GetManifestResourceNames()
                          .Where(n => n.StartsWith("Pinned.", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
                          .Select(n => n["Pinned.".Length..^".json".Length]);

  private static long Field(DelphiRtti rtti, string cls, string field)
  {
    var offsets = rtti.Vmts(cls)
                      .SelectMany(vmt => rtti.Fields(vmt))
                      .Where(f => f.Name.Equals(field, StringComparison.Ordinal))
                      .Select(f => f.Offset)
                      .Distinct()
                      .ToList();

    return offsets.Count == 1 ? offsets[0]
      : throw new SymbolResolutionException($"{cls}.{field}: expected one field in RTTI, found {offsets.Count}.");
  }
}
