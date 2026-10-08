// Serializes call arguments and results: primitives, collections, [Serializable] objects by fields, by-reference objects as ids.
namespace PluginManager.Remoting;

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

internal sealed class ValueWriter(BinaryWriter writer, IReferenceCodec references)
{
  private readonly Dictionary<object, int> _graph = new(ReferenceEqualityComparer.Instance);

  public void Write(object? value)
  {
    switch (value)
    {
      case null:             Tag(ValueTag.Null); return;
      case bool v:           Tag(ValueTag.Bool);   writer.Write(v); return;
      case byte v:           Tag(ValueTag.Byte);   writer.Write(v); return;
      case sbyte v:          Tag(ValueTag.SByte);  writer.Write(v); return;
      case short v:          Tag(ValueTag.Int16);  writer.Write(v); return;
      case ushort v:         Tag(ValueTag.UInt16); writer.Write(v); return;
      case int v:            Tag(ValueTag.Int32);  writer.Write(v); return;
      case uint v:           Tag(ValueTag.UInt32); writer.Write(v); return;
      case long v:           Tag(ValueTag.Int64);  writer.Write(v); return;
      case ulong v:          Tag(ValueTag.UInt64); writer.Write(v); return;
      case float v:          Tag(ValueTag.Single); writer.Write(v); return;
      case double v:         Tag(ValueTag.Double); writer.Write(v); return;
      case decimal v:        Tag(ValueTag.Decimal); writer.Write(v); return;
      case char v:           Tag(ValueTag.Char);   writer.Write(v); return;
      case string v:         Tag(ValueTag.String); writer.Write(v); return;
      case DateTime v:       Tag(ValueTag.DateTime); writer.Write(v.ToBinary()); return;
      case TimeSpan v:       Tag(ValueTag.TimeSpan); writer.Write(v.Ticks); return;
      case Guid v:           Tag(ValueTag.Guid);   writer.Write(v.ToByteArray()); return;
      case DateTimeOffset v: Tag(ValueTag.DateTimeOffset); writer.Write(v.Ticks); writer.Write((short)v.Offset.TotalMinutes); return;
      case IntPtr v:         Tag(ValueTag.IntPtr); writer.Write((long)v); return;
      case Type v:           Tag(ValueTag.Type);   writer.Write(TypeNames.Encode(v)); return;
      case Uri v:            Tag(ValueTag.Uri);    writer.Write(v.OriginalString); return;
      case Version v:        Tag(ValueTag.Version); writer.Write(v.ToString()); return;
      case byte[] v:         Tag(ValueTag.Bytes);  writer.Write(v.Length); writer.Write(v); return;
      case Enum v:           Tag(ValueTag.Enum);   writer.Write(TypeNames.Encode(v.GetType())); writer.Write(Convert.ToInt64(v, null)); return;
    }

    if (references.IsByReference(value))
    {
      var (tag, id) = references.Encode(value);
      Tag(tag);
      writer.Write(id);
      if (tag == ValueTag.RemoteObject)
        WriteInterfaces(value.GetType());
      return;
    }

    if (_graph.TryGetValue(value, out var index))
    {
      Tag(ValueTag.BackReference);
      writer.Write(index);
      return;
    }

    _graph[value] = _graph.Count;
    WriteComposite(value);
  }

  private void WriteComposite(object value)
  {
    var type = value.GetType();

    switch (value)
    {
      case Exception ex:
        Tag(ValueTag.Exception);
        writer.Write(TypeNames.Encode(type));
        writer.Write(ex.Message);
        writer.Write(ex.StackTrace ?? string.Empty);
        return;

      case Array array:
        Tag(ValueTag.Array);
        writer.Write(TypeNames.Encode(type.GetElementType()!));
        writer.Write(array.Length);
        foreach (var item in array) Write(item);
        return;

      case IDictionary dictionary:
        Tag(ValueTag.Dictionary);
        writer.Write(TypeNames.Encode(type));
        writer.Write(dictionary.Count);
        foreach (DictionaryEntry e in dictionary) { Write(e.Key); Write(e.Value); }
        return;

      case IEnumerable enumerable when IsReconstructibleCollection(type):
        var items = enumerable.Cast<object?>().ToList();
        Tag(ValueTag.Collection);
        writer.Write(TypeNames.Encode(type));
        writer.Write(items.Count);
        foreach (var item in items) Write(item);
        return;
    }

    if (!IsMarkedSerializable(type) && !IsCompilerTuple(type))
      throw new RemotingException($"Type '{type.FullName}' is neither [Serializable] nor remotable (MarshalByRefObject or interface proxy).");

    Tag(ValueTag.Object);
    writer.Write(TypeNames.Encode(type));
    var fields = SerializableFields(type);
    writer.Write(fields.Count);
    foreach (var f in fields)
    {
      writer.Write(f.DeclaringType!.Name + "." + f.Name);
      Write(f.GetValue(value));
    }
  }

  private void WriteInterfaces(Type type)
  {
    var interfaces = type.IsSubclassOf(typeof(Delegate)) ? [type] : type.GetInterfaces().Where(i => i.IsPublic || i.IsNestedPublic).ToArray();
    writer.Write(interfaces.Length);
    foreach (var i in interfaces)
      writer.Write(TypeNames.Encode(i));
  }

  private void Tag(ValueTag tag) => writer.Write((byte)tag);

  /// <summary>Instance fields in declaration order across the hierarchy, excluding [NonSerialized] and delegate (event) fields.</summary>
  internal static List<FieldInfo> SerializableFields(Type type)
  {
    var fields = new List<FieldInfo>();
    for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
      fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                       .Where(f => (f.Attributes & FieldAttributes.NotSerialized) == 0 && !typeof(Delegate).IsAssignableFrom(f.FieldType)));
    return fields;
  }

  internal static bool IsReconstructibleCollection(Type type) =>
    type.IsGenericType && type.GetConstructor(Type.EmptyTypes) is not null
    && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

  private static bool IsCompilerTuple(Type type) => typeof(ITuple).IsAssignableFrom(type);

  internal static bool IsMarkedSerializable(Type type) => (type.Attributes & TypeAttributes.Serializable) != 0;
}
