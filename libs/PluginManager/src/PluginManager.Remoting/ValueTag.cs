// Wire tags for values. Objects travel by reference (MarshalByRefObject, delegates, proxies) or by value (fields).
namespace PluginManager.Remoting;

internal enum ValueTag : byte
{
  Null,
  Bool, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Decimal, Char, String,
  DateTime, TimeSpan, Guid, DateTimeOffset, IntPtr, Type, Uri, Version,
  Enum = 40,
  Bytes,
  Array,
  Collection,
  Dictionary,
  Object,
  BackReference,
  Exception,
  RemoteObject = 60, // owned by the sender: the receiver gets a proxy
  ReturnedObject,    // owned by the receiver: a proxy coming home resolves to the original object
}

/// <summary>Implemented by the connection: maps by-reference objects to wire identifiers and back.</summary>
internal interface IReferenceCodec
{
  /// <summary>True when <paramref name="value" /> must cross by reference.</summary>
  bool IsByReference(object value);

  /// <summary>Encodes a by-reference value: (ReturnedObject, peer id) for the peer's own proxies, otherwise exports it.</summary>
  (ValueTag Tag, long Id) Encode(object value);

  object Decode(ValueTag tag, long id, string[] interfaceNames, Type declaredType);
}
