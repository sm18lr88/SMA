// Machine code added to sm20.exe (build 2026-06-06): per-theme card stylesheet selection and a collection reload after the Themes dialog.
using System.Buffers.Binary;

namespace SuperMemoAssistant.Themes.Exe;

/// <summary>
///   Addresses are virtual addresses of the SuperMemo 20 build linked on 2026-06-06 (ImageBase 0x400000).
///   SuperMemo draws cards with MSHTML and asks a host method (0xDCC440, GetStyleSheetFile) which stylesheet file to use.
///   The default answer is built by <c>call UStrCat3(&amp;path, root, "\bin\supermemo.css")</c> at 0xDCC489; a second
///   routine (0xF50910) builds the same path at 0xF50AE1. Both calls are redirected to the path routine, which has the
///   same signature. It returns &lt;root&gt;\bin\themes\&lt;active style&gt;.css when that file exists, else the original path.
///   Only RIP-relative data and rel32 calls are used, so ASLR needs no relocations. The bytes are checked against
///   Keystone's output for the reference implementation (see the golden tests).
/// </summary>
internal static class LiveThemePatch
{
  public static readonly ulong[] CallSites = [0xDCC489, 0xF50AE1];

  public const ulong CallOriginalTarget = 0x4161F0;

  public const ulong OkCallSite           = 0xC844EE;
  public const ulong OkCallOriginalTarget = 0x73E160;

  private const ulong ActiveStyle   = 0x677A60;
  private const ulong UStrCat3      = 0x4161F0;
  private const ulong UStrClear     = 0x4140A0;
  private const ulong FileExists    = 0xF6F220;
  private const int   NameGetterSlot = 0xA0; // TCustomStyleServices.Name, a virtual slot used by TThemeTestDlg.OKBtnClick

  private const ulong DarkFlagPtr = 0x1156720; // variables that hold pointers to SuperMemo's globals
  private const ulong MainFormPtr = 0x11544C8;
  private const ulong WinHandle   = 0x5E7200;
  private const ulong PostMessage = 0x427430;
  private const int   MsgDarkMode = 0x457;

  private const string ThemesFolder = "\\bin\\themes\\";
  private const string CssExtension = ".css";

  /// <summary>Section bytes for a section placed at <paramref name="sectionVa" />, and the addresses of the two entry points.</summary>
  public static (byte[] Blob, ulong PathEntry, ulong OkEntry) Build(ulong sectionVa)
  {
    var path   = PathCode(sectionVa);
    var okVa   = sectionVa + (ulong)((path.Length + 15) / 16 * 16);
    var padded = new byte[(int)(okVa - sectionVa)];

    padded.AsSpan().Fill(0xCC);
    path.CopyTo(padded, 0);

    return ([.. padded, .. OkHook(okVa)], sectionVa, okVa);
  }

  /// <summary>Bytes of <c>call cave</c> that replace the original call at <paramref name="site" />.</summary>
  public static byte[] CallPatch(ulong site, ulong caveVa) => Call(site, caveVa);

  public static byte[] OriginalCall(ulong site, ulong target = CallOriginalTarget) => Call(site, target);

  private static byte[] Call(ulong site, ulong target)
  {
    var bytes = new byte[5];

    bytes[0] = 0xE8;
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(1), checked((int)((long)target - ((long)site + 5))));

    return bytes;
  }

  private static byte[] PathCode(ulong va)
  {
    var e = new X64Emitter(va);

    e.Raw(0x53);                                        // push rbx
    e.Raw(0x56);                                        // push rsi
    e.Raw(0x57);                                        // push rdi
    e.Raw(0x55);                                        // push rbp
    e.Raw(0x48, 0x83, 0xEC, 0x58);                      // sub rsp, 0x58
    e.Raw(0x48, 0x89, 0x4C, 0x24, 0x40);                // mov [rsp+0x40], rcx
    e.Raw(0x48, 0x89, 0x54, 0x24, 0x48);                // mov [rsp+0x48], rdx
    e.Raw(0x4C, 0x89, 0x44, 0x24, 0x50);                // mov [rsp+0x50], r8
    e.Raw(0x31, 0xC0);                                  // xor eax, eax
    e.Raw(0x48, 0x89, 0x44, 0x24, 0x20);                // mov [rsp+0x20], rax
    e.Raw(0x48, 0x89, 0x44, 0x24, 0x28);                // mov [rsp+0x28], rax
    e.Raw(0x48, 0x89, 0x44, 0x24, 0x30);                // mov [rsp+0x30], rax
    e.Call(ActiveStyle);                                // call ActiveStyle
    e.Raw(0x48, 0x89, 0xC1);                            // mov rcx, rax
    e.Raw(0x48, 0x8D, 0x54, 0x24, 0x28);                // lea rdx, [rsp+0x28]
    e.Raw(0x48, 0x8B, 0x18);                            // mov rbx, [rax]
    e.Raw(0xFF, 0x93, (byte)NameGetterSlot, 0, 0, 0);   // call qword ptr [rbx+0xA0]
    e.Raw(0x48, 0x8D, 0x4C, 0x24, 0x20);                // lea rcx, [rsp+0x20]
    e.Raw(0x48, 0x8B, 0x54, 0x24, 0x48);                // mov rdx, [rsp+0x48]
    var themesRef = e.LeaR8Placeholder();               // lea r8, [rip+lit_themes]
    e.Call(UStrCat3);                                   // call UStrCat3
    e.Raw(0x48, 0x8D, 0x4C, 0x24, 0x30);                // lea rcx, [rsp+0x30]
    e.Raw(0x48, 0x8B, 0x54, 0x24, 0x20);                // mov rdx, [rsp+0x20]
    e.Raw(0x4C, 0x8B, 0x44, 0x24, 0x28);                // mov r8, [rsp+0x28]
    e.Call(UStrCat3);                                   // call UStrCat3
    e.Raw(0x48, 0x8B, 0x4C, 0x24, 0x40);                // mov rcx, [rsp+0x40]
    e.Raw(0x48, 0x8B, 0x54, 0x24, 0x30);                // mov rdx, [rsp+0x30]
    var cssRef = e.LeaR8Placeholder();                  // lea r8, [rip+lit_css]
    e.Call(UStrCat3);                                   // call UStrCat3
    e.Raw(0x48, 0x8B, 0x44, 0x24, 0x40);                // mov rax, [rsp+0x40]
    e.Raw(0x48, 0x8B, 0x08);                            // mov rcx, [rax]
    e.Call(FileExists);                                 // call FileExists
    e.Raw(0x84, 0xC0);                                  // test al, al
    var jump = e.ShortJumpIfNotEqualPlaceholder();      // jne done
    e.Raw(0x48, 0x8B, 0x4C, 0x24, 0x40);                // mov rcx, [rsp+0x40]
    e.Raw(0x48, 0x8B, 0x54, 0x24, 0x48);                // mov rdx, [rsp+0x48]
    e.Raw(0x4C, 0x8B, 0x44, 0x24, 0x50);                // mov r8, [rsp+0x50]
    e.Call(UStrCat3);                                   // call UStrCat3
    e.ResolveShortJump(jump);                           // done:

    foreach (var slot in new byte[] { 0x20, 0x28, 0x30 })
    {
      e.Raw(0x48, 0x8D, 0x4C, 0x24, slot);              // lea rcx, [rsp+slot]
      e.Call(UStrClear);                                // call UStrClear
    }

    e.Raw(0x48, 0x83, 0xC4, 0x58);                      // add rsp, 0x58
    e.Raw(0x5D);                                        // pop rbp
    e.Raw(0x5F);                                        // pop rdi
    e.Raw(0x5E);                                        // pop rsi
    e.Raw(0x5B);                                        // pop rbx
    e.Raw(0xC3);                                        // ret

    e.ResolveLea(themesRef, e.Literal(ThemesFolder));
    e.ResolveLea(cssRef, e.Literal(CssExtension));

    return e.ToArray();
  }

  /// <summary>
  ///   Replacement for the last call in TThemeTestDlg.OKBtnClick: run it, then make SuperMemo reload. SuperMemo's own
  ///   dark-mode handler (message 0x457) closes and reopens the collection, which makes every card ask for its stylesheet
  ///   again. It only acts when the new dark-mode flag differs from the stored one, so the flag is flipped first and the
  ///   real value is sent as the message.
  /// </summary>
  private static byte[] OkHook(ulong va)
  {
    var e = new X64Emitter(va);

    e.Raw(0x53);                                        // push rbx
    e.Raw(0x48, 0x83, 0xEC, 0x30);                      // sub rsp, 0x30
    e.Call(OkCallOriginalTarget);                       // call original
    e.RipLoadRax(DarkFlagPtr);                          // mov rax, [rip+DarkFlagPtr]
    e.Raw(0x0F, 0xB6, 0x18);                            // movzx ebx, byte ptr [rax]
    e.Raw(0x89, 0xD9);                                  // mov ecx, ebx
    e.Raw(0x83, 0xF1, 0x01);                            // xor ecx, 1
    e.Raw(0x88, 0x08);                                  // mov [rax], cl
    e.RipLoadRax(MainFormPtr);                          // mov rax, [rip+MainFormPtr]
    e.Raw(0x48, 0x8B, 0x08);                            // mov rcx, [rax]
    e.Call(WinHandle);                                  // call TWinControl.Handle
    e.Raw(0x48, 0x89, 0xC1);                            // mov rcx, rax
    e.Raw(0xBA, MsgDarkMode & 0xFF, MsgDarkMode >> 8, 0, 0); // mov edx, 0x457
    e.Raw(0x41, 0x89, 0xD8);                            // mov r8d, ebx
    e.Raw(0x45, 0x31, 0xC9);                            // xor r9d, r9d
    e.Call(PostMessage);                                // call PostMessage
    e.Raw(0x48, 0x83, 0xC4, 0x30);                      // add rsp, 0x30
    e.Raw(0x5B);                                        // pop rbx
    e.Raw(0xC3);                                        // ret

    return e.ToArray();
  }
}
