// Redirects sm20.exe's import address table entries; only calls made by sm20.exe's own code are intercepted.
namespace SuperMemoAssistant.Hooks.Agent;

internal static unsafe class IatPatcher
{
  private const int DataDirectoriesOffset = 24 + 112; // NT signature + file header, then PE32+ optional header fields

  /// <summary>
  ///   Replaces every IAT slot of <paramref name="module" /> that points at <paramref name="dll" />!<paramref name="function" />
  ///   (Delphi emits several import descriptors per DLL). Returns the original function pointer, or 0 when not imported.
  /// </summary>
  public static nint Patch(nint module, string dll, string function, nint replacement)
  {
    var original = Win32.GetProcAddress(Win32.GetModuleHandle(dll), function);
    if (original == 0)
      return 0;

    var patched = 0;
    foreach (var slot in ImportSlots(module))
    {
      if (*(nint*)slot != original)
        continue;

      Write(slot, replacement);
      patched++;
    }

    return patched > 0 ? original : 0;
  }

  /// <summary>Restores slots previously redirected to <paramref name="replacement" />.</summary>
  public static void Restore(nint module, nint replacement, nint original)
  {
    foreach (var slot in ImportSlots(module))
      if (*(nint*)slot == replacement)
        Write(slot, original);
  }

  private static List<nint> ImportSlots(nint module)
  {
    var image     = (byte*)module;
    var nt        = image + *(int*)(image + 0x3C);
    var importRva = *(uint*)(nt + DataDirectoriesOffset + Win32.IMAGE_DIRECTORY_IMPORT * 8);
    var slots     = new List<nint>();

    if (importRva == 0)
      return slots;

    // IMAGE_IMPORT_DESCRIPTOR = { OriginalFirstThunk, TimeDateStamp, ForwarderChain, Name, FirstThunk } (uint32 each)
    for (var descriptor = (uint*)(image + importRva); descriptor[3] != 0; descriptor += 5)
      for (var thunk = (nint*)(image + descriptor[4]); *thunk != 0; thunk++)
        slots.Add((nint)thunk);

    return slots;
  }

  private static void Write(nint slot, nint value)
  {
    if (!Win32.VirtualProtect(slot, (nuint)sizeof(nint), Win32.PAGE_READWRITE, out var old))
      throw new InvalidOperationException($"VirtualProtect failed on IAT slot 0x{slot:X}.");

    *(nint*)slot = value;
    Win32.VirtualProtect(slot, (nuint)sizeof(nint), old, out _);
  }
}
