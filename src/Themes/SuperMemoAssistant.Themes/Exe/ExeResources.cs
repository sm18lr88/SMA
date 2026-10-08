// Lists, reads and writes named resources (VCL styles, Delphi forms) inside an executable through the Win32 resource APIs.
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SuperMemoAssistant.Themes.Exe;

/// <summary>A resource type: a name such as VCLSTYLE, or a standard integer id such as RT_RCDATA (10).</summary>
internal readonly record struct ResType(string? Name, int Id)
{
  public static ResType VclStyle { get; } = new("VCLSTYLE", 0);

  public static ResType RcData { get; } = new(null, 10);

  public const int LangNeutral = 0;

  public const int LangEnUs = 1033;
}

/// <summary>Identifies one resource: type, name and language.</summary>
internal readonly record struct ResKey(ResType Type, string Name, int Language);

internal static unsafe partial class ExeResources
{
  private const uint LoadAsDatafile = 0x2 | 0x20; // LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE

  [LibraryImport("kernel32", EntryPoint = "LoadLibraryExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
  private static partial nint LoadLibraryEx(string path, nint file, uint flags);

  [LibraryImport("kernel32", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool FreeLibrary(nint module);

  [LibraryImport("kernel32", EntryPoint = "EnumResourceNamesW")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool EnumResourceNames(nint module, nint type, delegate* unmanaged[Stdcall]<nint, nint, nint, nint, int> proc, nint param);

  [LibraryImport("kernel32", EntryPoint = "FindResourceExW")]
  private static partial nint FindResourceEx(nint module, nint type, nint name, ushort language);

  [LibraryImport("kernel32")]
  private static partial nint LoadResource(nint module, nint resource);

  [LibraryImport("kernel32")]
  private static partial nint LockResource(nint resource);

  [LibraryImport("kernel32")]
  private static partial uint SizeofResource(nint module, nint resource);

  [LibraryImport("kernel32", EntryPoint = "BeginUpdateResourceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
  private static partial nint BeginUpdateResource(string file, [MarshalAs(UnmanagedType.Bool)] bool deleteExisting);

  [LibraryImport("kernel32", EntryPoint = "UpdateResourceW", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool UpdateResource(nint update, nint type, nint name, ushort language, nint data, uint size);

  [LibraryImport("kernel32", EntryPoint = "EndUpdateResourceW", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool EndUpdateResource(nint update, [MarshalAs(UnmanagedType.Bool)] bool discard);

  /// <summary>Resource name to bytes for every named resource of one type and language.</summary>
  public static Dictionary<string, byte[]> Read(string exe, ResType type, int language, Func<string, bool>? include = null)
  {
    var module = LoadLibraryEx(exe, 0, LoadAsDatafile);

    if (module == 0)
      throw new Win32Exception(Marshal.GetLastWin32Error());

    using var typeId = new IdPointer(type);

    try
    {
      var names  = new List<string>();
      var handle = GCHandle.Alloc(names);

      try
      {
        EnumResourceNames(module, typeId.Value, &CollectName, GCHandle.ToIntPtr(handle));
      }
      finally
      {
        handle.Free();
      }

      var output = new Dictionary<string, byte[]>();

      foreach (var name in names.Where(n => include is null || include(n)))
      {
        using var nameId  = new IdPointer(name);
        var       found   = FindResourceEx(module, typeId.Value, nameId.Value, (ushort)language);

        if (found == 0)
          continue;

        var size = (int)SizeofResource(module, found);

        output[name] = new ReadOnlySpan<byte>((void*)LockResource(LoadResource(module, found)), size).ToArray();
      }

      return output;
    }
    finally
    {
      FreeLibrary(module);
    }
  }

  [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
  private static int CollectName(nint module, nint type, nint name, nint param)
  {
    // Integer ids (below 0x10000) are skipped: every resource this tool touches is named.
    if ((ulong)name >= 0x10000 && GCHandle.FromIntPtr(param).Target is List<string> names)
      names.Add(Marshal.PtrToStringUni(name)!);

    return 1;
  }

  public static Dictionary<string, byte[]> ReadStyles(string exe) => Read(exe, ResType.VclStyle, ResType.LangEnUs);

  public static byte[] ReadForm(string exe, string name) => Read(exe, ResType.RcData, ResType.LangNeutral, n => n == name)[name];

  /// <summary>Adds, replaces or deletes resources in the file at <paramref name="path" /> (no checks, no backups).</summary>
  public static void Write(string path, IReadOnlyDictionary<ResKey, byte[]> add, IEnumerable<ResKey> remove)
  {
    var update = BeginUpdateResource(path, false);

    if (update == 0)
      throw new Win32Exception(Marshal.GetLastWin32Error());

    var keepAlive = new List<IDisposable>();
    var buffers   = new List<nint>();

    try
    {
      foreach (var (key, blob) in add.Select(kv => (kv.Key, (byte[]?)kv.Value)).Concat(remove.Select(k => (k, (byte[]?)null))))
      {
        var typeId = new IdPointer(key.Type);
        var nameId = new IdPointer(key.Name);

        keepAlive.Add(typeId);
        keepAlive.Add(nameId);

        var buffer = blob is null ? 0 : Marshal.AllocHGlobal(Math.Max(1, blob.Length));

        if (blob is not null)
        {
          Marshal.Copy(blob, 0, buffer, blob.Length);
          buffers.Add(buffer);
        }

        if (!UpdateResource(update, typeId.Value, nameId.Value, (ushort)key.Language, buffer, (uint)(blob?.Length ?? 0)))
        {
          var error = Marshal.GetLastWin32Error();

          EndUpdateResource(update, true);
          update = 0;

          throw new Win32Exception(error);
        }
      }

      if (!EndUpdateResource(update, false))
      {
        update = 0;

        throw new Win32Exception(Marshal.GetLastWin32Error());
      }

      update = 0;
    }
    finally
    {
      if (update != 0)
        EndUpdateResource(update, true);

      foreach (var d in keepAlive)
        d.Dispose();

      foreach (var b in buffers)
        Marshal.FreeHGlobal(b);
    }
  }

  /// <summary>A Win32 resource id argument: the integer itself, or a pointer to a wide string that this object owns.</summary>
  private sealed class IdPointer : IDisposable
  {
    private readonly nint _owned;

    public IdPointer(ResType type)
    {
      _owned = type.Name is null ? 0 : Marshal.StringToHGlobalUni(type.Name);
      Value  = type.Name is null ? type.Id : _owned;
    }

    public IdPointer(string name)
    {
      _owned = Marshal.StringToHGlobalUni(name);
      Value  = _owned;
    }

    public nint Value { get; }

    public void Dispose()
    {
      if (_owned != 0)
        Marshal.FreeHGlobal(_owned);
    }
  }
}
