// Resolved native symbols for one SuperMemo build: function RVAs, global RVAs and field offsets.
namespace SuperMemoAssistant.Hooks.Symbols;

using System.Text.Json;
using System.Text.Json.Serialization;
using SuperMemoAssistant.Hooks.Agent;
using SuperMemoAssistant.SuperMemo;

public sealed class SymbolTable
{
  [JsonConstructor]
  public SymbolTable(string                                         buildId,
                     IReadOnlyDictionary<NativeMethod, MethodSymbol> methods,
                     IReadOnlyDictionary<NativePointer, long>        pointers)
  {
    BuildId  = buildId;
    Methods  = methods;
    Pointers = pointers;
  }

  /// <summary>See <see cref="SymbolResolver.BuildIdOf" />.</summary>
  public string BuildId { get; }

  public IReadOnlyDictionary<NativeMethod, MethodSymbol> Methods { get; }

  /// <summary>Globals are RVAs (<see cref="NativePointerEx.IsGlobal" />); all other entries are offsets or sizes.</summary>
  public IReadOnlyDictionary<NativePointer, long> Pointers { get; }

  /// <summary>Absolute address of a unit global in a process where sm20.exe is mapped at <paramref name="moduleBase" />.</summary>
  public IntPtr GlobalAddress(NativePointer pointer, IntPtr moduleBase)
  {
    if (!pointer.IsGlobal())
      throw new ArgumentException($"{pointer} is an offset, not a global.", nameof(pointer));

    return moduleBase + (nint)Pointers[pointer];
  }

  public int Offset(NativePointer pointer)
  {
    if (pointer.IsGlobal())
      throw new ArgumentException($"{pointer} is a global, not an offset.", nameof(pointer));

    return checked((int)Pointers[pointer]);
  }

  public IReadOnlyList<NativeFunction> ToAgentFunctions() =>
    Methods.Select(kv => new NativeFunction(kv.Key, kv.Value.Rva, kv.Value.ReturnKind)).ToArray();

  public string ToJson() => JsonSerializer.Serialize(this, SymbolTableJson.Default.SymbolTable);

  public static SymbolTable FromJson(string json) =>
    JsonSerializer.Deserialize(json, SymbolTableJson.Default.SymbolTable)
    ?? throw new SymbolResolutionException("Empty symbol table JSON.");

  /// <summary>Lists differences against <paramref name="other" /> (empty when identical).</summary>
  public IReadOnlyList<string> Diff(SymbolTable other)
  {
    var diffs = new List<string>();

    if (BuildId != other.BuildId)
      diffs.Add($"BuildId {BuildId} != {other.BuildId}");

    foreach (var key in Methods.Keys.Union(other.Methods.Keys))
      if (!Methods.TryGetValue(key, out var a) || !other.Methods.TryGetValue(key, out var b) || a != b)
        diffs.Add($"{key}: {Describe(Methods, key)} != {Describe(other.Methods, key)}");

    foreach (var key in Pointers.Keys.Union(other.Pointers.Keys))
      if (!Pointers.TryGetValue(key, out var a) || !other.Pointers.TryGetValue(key, out var b) || a != b)
        diffs.Add($"{key}: {Describe(Pointers, key)} != {Describe(other.Pointers, key)}");

    return diffs;
  }

  private static string Describe<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> map, TKey key)
    where TKey : notnull =>
    map.TryGetValue(key, out var value) ? value?.ToString() ?? "null" : "missing";
}

public readonly record struct MethodSymbol(long Rva, NativeReturnKind ReturnKind);

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SymbolTable))]
internal sealed partial class SymbolTableJson : JsonSerializerContext;

public sealed class SymbolResolutionException(string message) : Exception(message);
