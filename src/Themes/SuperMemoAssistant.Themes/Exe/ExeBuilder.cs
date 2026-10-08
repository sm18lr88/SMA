// Rebuilds sm20.exe from the untouched original: window styles, element-theming switch, live-theme patch.
using System.Security.Cryptography;
using System.Text;
using SuperMemoAssistant.Themes.Forms;

namespace SuperMemoAssistant.Themes.Exe;

/// <summary>
///   Every change produces the exe from scratch: original, then resources, then the new code section, then the call
///   patches. Building forward from the original keeps changes repeatable and lets <see cref="Restore" /> return the
///   original byte for byte. A build is verified before it replaces the exe.
/// </summary>
internal static class ExeBuilder
{
  public const string PatchSection = ".smc";

  public const string CustomPrefix = "SMC_";

  public static readonly string[] Themed = ["seFont", "seClient", "seBorder"];

  /// <summary>
  ///   Form resource to the controls that SuperMemo ships with StyleElements = [] (never styled): the card area, the
  ///   status bar and its text panels. The "8.1%" and "4 days" labels of the learn bar are left out on purpose: SuperMemo sets their
  ///   background to white in code, so a style would leave light text on white.
  /// </summary>
  public static readonly IReadOnlyDictionary<string, string[]> ElementObjects = new Dictionary<string, string[]>
  {
    ["TELWIND"]  = ["ScrollBox1"],
    ["TSTATBAR"] = ["StatBar", "MemorizedLabel", "OutstandingLabel"],
  };

  private static readonly int TotalObjects = ElementObjects.Sum(kv => kv.Value.Length);

  private static Dictionary<ulong, byte[]> PatchSites()
  {
    var sites = LiveThemePatch.CallSites.ToDictionary(s => s, s => LiveThemePatch.OriginalCall(s));

    sites[LiveThemePatch.OkCallSite] = LiveThemePatch.OriginalCall(LiveThemePatch.OkCallSite, LiveThemePatch.OkCallOriginalTarget);

    return sites;
  }

  /// <summary>Hash of the program code and data with resources and this tool's changes taken out.</summary>
  public static string ProgramHash(byte[] data)
  {
    var pe = new PeImage(data);

    if (pe.HasSection(PatchSection))
    {
      foreach (var (va, original) in PatchSites())
        pe.Write(va, original);
    }

    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    foreach (var s in pe.Sections().Where(s => s.Name is not (".rsrc" or PatchSection)))
    {
      hash.AppendData(Encoding.UTF8.GetBytes(s.Name));
      hash.AppendData(pe.Bytes.AsSpan((int)s.RawPointer, (int)s.RawSize));
    }

    return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
  }

  private static int ThemedCount(string exe)
  {
    var count = 0;

    foreach (var (form, names) in ElementObjects)
    {
      var root = DfmForm.Parse(ExeResources.ReadForm(exe, form));

      foreach (var name in names)
      {
        if (root.Find(name) is { } node && DfmForm.StyleElementsOf(node) is { } set && set.SetEquals(Themed))
          count++;
      }
    }

    return count;
  }

  public static ExeState ReadState(string exe)
  {
    var styles = ExeResources.ReadStyles(exe)
                             .Where(kv => kv.Key.StartsWith(CustomPrefix, StringComparison.Ordinal))
                             .ToDictionary(kv => kv.Key, kv => kv.Value);
    var patched = new PeImage(File.ReadAllBytes(exe)).HasSection(PatchSection);

    return new ExeState { Styles = styles, Elements = ThemedCount(exe) == TotalObjects, Patched = patched };
  }

  /// <summary>True when the exe holds any change from this tool: an installed style, a themed element, or the code patch.</summary>
  public static bool HasChanges(string exe) => ReadState(exe) is { Styles.Count: > 0 } or { Patched: true } || ThemedCount(exe) > 0;

  /// <summary>The untouched exe. Refreshed when SuperMemo was updated (different program code).</summary>
  public static string PristinePath(string backupDir, string exe)
  {
    Directory.CreateDirectory(backupDir);

    var pristine = Path.Combine(backupDir, $"{Path.GetFileName(exe)}.original");

    if (!File.Exists(pristine) || ProgramHash(File.ReadAllBytes(pristine)) != ProgramHash(File.ReadAllBytes(exe)))
    {
      var state = ReadState(exe);

      if (state.Styles.Count > 0 || ThemedCount(exe) > 0 || state.Patched)
        throw new ThemeException("sm20.exe has theme changes but no matching original backup; reinstall SuperMemo or restore the exe first.");

      File.Copy(exe, pristine, true);
    }

    return pristine;
  }

  /// <summary>True when this exe is the SuperMemo 20 build the live-theme patch was written for.</summary>
  public static bool PatchSupported(string exe)
  {
    var pe = new PeImage(File.ReadAllBytes(exe));

    if (pe.HasSection(PatchSection))
      return true; // already patched, so the check passed before

    try
    {
      return PatchSites().All(kv => pe.Read(kv.Key, kv.Value.Length).AsSpan().SequenceEqual(kv.Value));
    }
    catch (InvalidOperationException)
    {
      return false;
    }
  }

  private static void CheckPatchSites(PeImage pe)
  {
    foreach (var (va, original) in PatchSites())
    {
      if (!pe.Read(va, original.Length).AsSpan().SequenceEqual(original))
        throw new ThemeException($"unexpected code at 0x{va:X}: this SuperMemo build is not supported by the live-theme patch");
    }
  }

  public static void Build(string exe, string backupDir, ExeState target)
  {
    var pristine = PristinePath(backupDir, exe);

    File.Copy(exe, Path.Combine(backupDir, $"{Path.GetFileName(exe)}.previous"), true);

    var tmp = exe + ".smcards-tmp";

    File.Copy(pristine, tmp, true);

    try
    {
      var add = target.Styles.ToDictionary(kv => new ResKey(ResType.VclStyle, kv.Key, ResType.LangEnUs), kv => kv.Value);

      if (target.Elements)
        AddElementForms(pristine, add);

      if (add.Count > 0)
        ExeResources.Write(tmp, add, []);

      if (target.Patched)
        ApplyCodePatch(tmp);

      if (ProgramHash(File.ReadAllBytes(tmp)) != ProgramHash(File.ReadAllBytes(pristine)))
        throw new InvalidOperationException("build changed program code outside the known patch sites");

      var built = ReadState(tmp);

      if (!SameStyles(built.Styles, target.Styles) || built.Elements != target.Elements || built.Patched != target.Patched)
        throw new InvalidOperationException("build verification failed: resources or patch did not read back as planned");

      SwapIn(tmp, exe);
    }
    finally
    {
      if (File.Exists(tmp))
        File.Delete(tmp);
    }
  }

  private static void AddElementForms(string pristine, Dictionary<ResKey, byte[]> add)
  {
    foreach (var (formName, names) in ElementObjects)
    {
      var form = ExeResources.ReadForm(pristine, formName);

      try
      {
        add[new ResKey(ResType.RcData, formName, ResType.LangNeutral)] = DfmForm.SetStyleElements(form, names.ToDictionary(n => n, _ => Themed));
      }
      catch (KeyNotFoundException ex)
      {
        throw new ThemeException($"element theming is not supported on this SuperMemo build: {ex.Message}", ex);
      }
    }
  }

  private static void ApplyCodePatch(string path)
  {
    var pe = new PeImage(File.ReadAllBytes(path));

    CheckPatchSites(pe);

    var (code, pathEntry, okEntry) = LiveThemePatch.Build(pe.NextVa());

    pe.AddSection(PatchSection, code);

    foreach (var site in LiveThemePatch.CallSites)
      pe.Write(site, LiveThemePatch.CallPatch(site, pathEntry));

    pe.Write(LiveThemePatch.OkCallSite, LiveThemePatch.CallPatch(LiveThemePatch.OkCallSite, okEntry));
    File.WriteAllBytes(path, pe.Bytes);
  }

  private static bool SameStyles(Dictionary<string, byte[]> a, Dictionary<string, byte[]> b) =>
    a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var other) && kv.Value.AsSpan().SequenceEqual(other));

  /// <summary>Replaces the exe with the finished build. Antivirus can hold a fresh executable for a moment, so retry.</summary>
  private static void SwapIn(string tmp, string exe)
  {
    for (var attempt = 0; ; attempt++)
    {
      try
      {
        File.Move(tmp, exe, true);

        return;
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < 19)
      {
        Thread.Sleep(500);
      }
    }
  }

  public static void Restore(string exe, string backupDir)
  {
    var pristine = Path.Combine(backupDir, $"{Path.GetFileName(exe)}.original");

    if (!File.Exists(pristine))
      throw new ThemeException("no original backup; the exe was never modified");

    if (ProgramHash(File.ReadAllBytes(pristine)) != ProgramHash(File.ReadAllBytes(exe)))
      throw new ThemeException("sm20.exe is a different SuperMemo build than the backup; not restoring.");

    File.Copy(pristine, exe, true);
  }
}
