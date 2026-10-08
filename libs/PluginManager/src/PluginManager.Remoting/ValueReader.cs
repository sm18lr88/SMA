// Deserializes values written by ValueWriter. By-value objects are rebuilt from fields without running constructors.
namespace PluginManager.Remoting;

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

internal sealed class ValueReader(BinaryReader reader, IReferenceCodec references)
{
  private readonly List<object> _graph = new();

  public object? Read(Type declaredType)
  {
    var tag = (ValueTag)reader.ReadByte();
    switch (tag)
    {
      case ValueTag.Null:           return null;
      case ValueTag.Bool:           return reader.ReadBoolean();
      case ValueTag.Byte:           return reader.ReadByte();
      case ValueTag.SByte:          return reader.ReadSByte();
      case ValueTag.Int16:          return reader.ReadInt16();
      case ValueTag.UInt16:         return reader.ReadUInt16();
      case ValueTag.Int32:          return reader.ReadInt32();
      case ValueTag.UInt32:         return reader.ReadUInt32();
      case ValueTag.Int64:          return reader.ReadInt64();
      case ValueTag.UInt64:         return reader.ReadUInt64();
      case ValueTag.Single:         return reader.ReadSingle();
      case ValueTag.Double:         return reader.ReadDouble();
      case ValueTag.Decimal:        return reader.ReadDecimal();
      case ValueTag.Char:           return reader.ReadChar();
      case ValueTag.String:         return reader.ReadString();
      case ValueTag.DateTime:       return DateTime.FromBinary(reader.ReadInt64());
      case ValueTag.TimeSpan:       return new TimeSpan(reader.ReadInt64());
      case ValueTag.Guid:           return new Guid(reader.ReadBytes(16));
      case ValueTag.DateTimeOffset: return new DateTimeOffset(reader.ReadInt64(), TimeSpan.FromMinutes(reader.ReadInt16()));
      case ValueTag.IntPtr:         return (IntPtr)reader.ReadInt64();
      case ValueTag.Type:           return TypeNames.Resolve(reader.ReadString());
      case ValueTag.Uri:            return new Uri(reader.ReadString(), UriKind.RelativeOrAbsolute);
      case ValueTag.Version:        return Version.Parse(reader.ReadString());
      case ValueTag.Bytes:          return reader.ReadBytes(reader.ReadInt32());
      case ValueTag.Enum:           return Enum.ToObject(TypeNames.Resolve(reader.ReadString()), reader.ReadInt64());
      case ValueTag.BackReference:  return _graph[reader.ReadInt32()];

      case ValueTag.RemoteObject:
      case ValueTag.ReturnedObject:
        var id         = reader.ReadInt64();
        var interfaces = tag == ValueTag.RemoteObject ? ReadStrings() : [];
        return references.Decode(tag, id, interfaces, declaredType);

      case ValueTag.Exception:
        var exception = new RemoteException(reader.ReadString(), reader.ReadString(), reader.ReadString());
        _graph.Add(exception);
        return exception;

      case ValueTag.Array:      return ReadArray();
      case ValueTag.Collection: return ReadCollection();
      case ValueTag.Dictionary: return ReadDictionary();
      case ValueTag.Object:     return ReadObject();

      default: throw new RemotingException($"Unknown value tag {(byte)tag}.");
    }
  }

  private Array ReadArray()
  {
    var elementType = TypeNames.Resolve(reader.ReadString());
    var array       = Array.CreateInstance(elementType, reader.ReadInt32());
    _graph.Add(array);
    for (var i = 0; i < array.Length; i++)
      array.SetValue(Read(elementType), i);
    return array;
  }

  private object ReadCollection()
  {
    var type        = Allowed(TypeNames.Resolve(reader.ReadString()));
    var collection  = Activator.CreateInstance(type)!;
    var elementType = type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
    var add         = typeof(ICollection<>).MakeGenericType(elementType).GetMethod(nameof(ICollection<object>.Add))!;
    _graph.Add(collection);

    var count = reader.ReadInt32();
    for (var i = 0; i < count; i++)
      add.Invoke(collection, [Read(elementType)]);
    return collection;
  }

  private object ReadDictionary()
  {
    var type       = Allowed(TypeNames.Resolve(reader.ReadString()));
    var dictionary = (IDictionary)Activator.CreateInstance(type)!;
    var args       = type.IsGenericType ? type.GetGenericArguments() : [typeof(object), typeof(object)];
    _graph.Add(dictionary);

    var count = reader.ReadInt32();
    for (var i = 0; i < count; i++)
    {
      var key = Read(args[0])!;
      dictionary[key] = Read(args[^1]);
    }
    return dictionary;
  }

  private object ReadObject()
  {
    var type     = Allowed(TypeNames.Resolve(reader.ReadString()));
    var instance = RuntimeHelpers.GetUninitializedObject(type);
    _graph.Add(instance);

    var fields = ValueWriter.SerializableFields(type).ToDictionary(f => f.DeclaringType!.Name + "." + f.Name, StringComparer.Ordinal);
    var count  = reader.ReadInt32();
    for (var i = 0; i < count; i++)
    {
      var name = reader.ReadString();
      if (!fields.TryGetValue(name, out var field))
        throw new RemotingException($"Field '{name}' does not exist on '{type.FullName}'. Both sides must use the same assembly version.");

      field.SetValue(instance, Read(field.FieldType));
    }

    InvokeOnDeserialized(type, instance);
    return instance;
  }

  private string[] ReadStrings()
  {
    var values = new string[reader.ReadInt32()];
    for (var i = 0; i < values.Length; i++)
      values[i] = reader.ReadString();
    return values;
  }

  /// <summary>By-value reconstruction is limited to serializable types, collections and tuples (BinaryFormatter parity).</summary>
  private static Type Allowed(Type type) =>
    ValueWriter.IsMarkedSerializable(type) || ValueWriter.IsReconstructibleCollection(type) || typeof(ITuple).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type)
      ? type
      : throw new RemotingException($"Type '{type.FullName}' may not be deserialized by value.");

  private static void InvokeOnDeserialized(Type type, object instance)
  {
    for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
      foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        if (m.IsDefined(typeof(System.Runtime.Serialization.OnDeserializedAttribute), false))
          m.Invoke(instance, [default(System.Runtime.Serialization.StreamingContext)]);
  }
}
