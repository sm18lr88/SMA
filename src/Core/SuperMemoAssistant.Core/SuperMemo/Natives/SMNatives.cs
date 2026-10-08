// Typed access to SuperMemo 20's native objects: absolute addresses of unit globals, field locations, and main-thread calls.
namespace SuperMemoAssistant.SuperMemo.Natives
{
  using System;
  using Anotar.Serilog;
  using global::SuperMemoAssistant.Hooks.Symbols;
  using Process.NET.Memory;
  using SMA;

  /// <summary>
  ///   Built from a resolved <see cref="SymbolTable" /> and sm20.exe's load address. Pointers are read out of process;
  ///   every native call runs on SuperMemo's main thread through the injected agent.
  /// </summary>
  public partial class SMNatives
  {
    public SMNatives(SymbolTable symbols, IntPtr moduleBase)
    {
      Globals  = new TGlobals(symbols, moduleBase);
      Control  = new TControl(symbols);
      ElWind   = new TElWind(symbols, moduleBase);
      SMMain   = new TSMMain(symbols, moduleBase);
      Database = new TDatabase(symbols, moduleBase);
      Registry = new TRegistry(symbols, Database);
    }

    public TGlobals  Globals  { get; }
    public TControl  Control  { get; }
    public TElWind   ElWind   { get; }
    public TSMMain   SMMain   { get; }
    public TDatabase Database { get; }
    public TRegistry Registry { get; }

    /// <summary>Runs a native method on SuperMemo's main thread and returns its normalized result.</summary>
    internal static long Call(NativeMethod method, params object[] args) => Core.Hook.ExecuteOnMainThread(method, args);

    /// <summary>Runs a native procedure; success means it returned without an error (Delphi procedures have no result).</summary>
    internal static bool TryCall(NativeMethod method, params object[] args) => TryCall(_ => true, false, method, args);

    internal static T TryCall<T>(Func<long, T> map, T fallback, NativeMethod method, params object[] args)
    {
      try
      {
        return map(Call(method, args));
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Native method {Method} failed.", method);
        return fallback;
      }
    }

    /// <summary>Unit-level variables (no RTTI: located by code shape, see Hooks.Symbols).</summary>
    public class TGlobals
    {
      public TGlobals(SymbolTable symbols, IntPtr moduleBase)
      {
        IntPtr Global(NativePointer p) => symbols.GlobalAddress(p, moduleBase);

        LimitChildrenCountPtr     = Global(NativePointer.Globals_LimitChildrenCountPtr);
        CurrentConceptGroupIdPtr  = Global(NativePointer.Globals_CurrentConceptGroupIdPtr);
        CurrentRootIdPtr          = Global(NativePointer.Globals_CurrentRootIdPtr);
        CurrentHookIdPtr          = Global(NativePointer.Globals_CurrentHookIdPtr);
        CurrentConceptIdPtr       = Global(NativePointer.Globals_CurrentConceptIdPtr);
        IgnoreUserConfirmationPtr = Global(NativePointer.Globals_IgnoreUserConfirmationPtr);
      }

      public IntPtr LimitChildrenCountPtr     { get; }
      public IntPtr CurrentConceptGroupIdPtr  { get; }
      public IntPtr CurrentRootIdPtr          { get; }
      public IntPtr CurrentHookIdPtr          { get; }
      public IntPtr CurrentConceptIdPtr       { get; }
      public IntPtr IgnoreUserConfirmationPtr { get; }
    }

    /// <summary>VCL TControl / TWinControl field offsets.</summary>
    public class TControl
    {
      public TControl(SymbolTable symbols)
      {
        ParentOffset     = symbols.Offset(NativePointer.Control_ParentOffset);
        WindowProcOffset = symbols.Offset(NativePointer.Control_WindowProcOffset);
        HandleOffset     = symbols.Offset(NativePointer.Control_HandleOffset);
        LeftOffset       = symbols.Offset(NativePointer.Control_LeftOffset);
        TopOffset        = symbols.Offset(NativePointer.Control_TopOffset);
        WidthOffset      = symbols.Offset(NativePointer.Control_WidthOffset);
        HeightOffset     = symbols.Offset(NativePointer.Control_HeightOffset);
      }

      public int ParentOffset     { get; }
      public int WindowProcOffset { get; }
      public int HandleOffset     { get; }
      public int LeftOffset       { get; }
      public int TopOffset        { get; }
      public int WidthOffset      { get; }
      public int HeightOffset     { get; }
    }

    public class TDatabase
    {
      public TDatabase(SymbolTable symbols, IntPtr moduleBase) =>
        InstancePtr = symbols.GlobalAddress(NativePointer.Database_InstancePtr, moduleBase);

      /// <summary>Address of the <c>Database</c> variable (which holds the TDatabase pointer).</summary>
      public IntPtr InstancePtr { get; }
    }

    public class TSMMain
    {
      public TSMMain(SymbolTable symbols, IntPtr moduleBase) =>
        InstancePtr = symbols.GlobalAddress(NativePointer.SMMain_InstancePtr, moduleBase);

      /// <summary>Address of the <c>SMMain</c> variable (which holds the TSMMain form pointer).</summary>
      public IntPtr InstancePtr { get; }

      /// <summary>TSMMain.NewTitle takes a UnicodeString, so non-ANSI titles survive.</summary>
      public bool SupportsUnicodeTitle => true;

      public void SetUnicodeTitle(int elementId, string title)
      {
        var smMainPtr = Core.SM.SMProcess.Memory.Read<IntPtr>(InstancePtr);
        Call(NativeMethod.TSMMain_NewTitle, smMainPtr, elementId, title);
        LogTo.Debug("Applied Unicode title to element {ElementId}: {Title}", elementId, title);
      }

      public bool SelectDefaultConcept(IntPtr smMainPtr, int conceptId) =>
        TryCall(NativeMethod.TSMMain_SelectDefaultConcept, smMainPtr, conceptId);
    }

    /// <summary>TDatabase's registry fields; each <see cref="ObjPtr" /> reads Database^ then the field.</summary>
    public class TRegistry
    {
      public TRegistry(SymbolTable symbols, TDatabase db)
      {
        ObjPtr Field(NativePointer p) => new(db.InstancePtr, symbols.Offset(p));

        TextRegistryInstance     = Field(NativePointer.Registry_TextRegistryInstance);
        ImageRegistryInstance    = Field(NativePointer.Registry_ImageRegistryInstance);
        SoundRegistryInstance    = Field(NativePointer.Registry_SoundRegistryInstance);
        VideoRegistryInstance    = Field(NativePointer.Registry_VideoRegistryInstance);
        BinaryRegistryInstance   = Field(NativePointer.Registry_BinaryRegistryInstance);
        TemplateRegistryInstance = Field(NativePointer.Registry_TemplateRegistryInstance);
        ConceptRegistryInstance  = Field(NativePointer.Registry_ConceptRegistryInstance);
      }

      public ObjPtr TextRegistryInstance     { get; }
      public ObjPtr ImageRegistryInstance    { get; }
      public ObjPtr SoundRegistryInstance    { get; }
      public ObjPtr VideoRegistryInstance    { get; }
      public ObjPtr BinaryRegistryInstance   { get; }
      public ObjPtr TemplateRegistryInstance { get; }
      public ObjPtr ConceptRegistryInstance  { get; }

      /// <summary>TRegistry.AddMember: returns the new member id, or a value &lt;= 0 on failure.</summary>
      public int AddMember(IntPtr registryPtr, string text) => (int)Call(NativeMethod.TRegistry_AddMember, registryPtr, text);

      /// <summary>TRegistry.ImportFile: returns the new member id, or a value &lt;= 0 on failure.</summary>
      public int ImportFile(IntPtr registryPtr, string filePath, string name) =>
        (int)Call(NativeMethod.TRegistry_ImportFile, registryPtr, filePath, name);
    }
  }
}
