// TElWind (SuperMemo's element window) and its TComponentData: field locations and main-thread operations.
namespace SuperMemoAssistant.SuperMemo.Natives
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using global::SuperMemoAssistant.Hooks.Symbols;
  using Interop.SuperMemo.Content.Controls;
  using Interop.SuperMemo.Elements.Models;
  using Interop.SuperMemo.Registry.Models;
  using Process.NET.Memory;
  using SMA;

  public partial class SMNatives
  {
    public class TElWind
    {
      private static readonly TimeSpan CreatedElementTimeout = TimeSpan.FromSeconds(5);

      public TElWind(SymbolTable symbols, IntPtr moduleBase)
      {
        InstancePtr = symbols.GlobalAddress(NativePointer.ElWdw_InstancePtr, moduleBase);

        ObjPtr Field(NativePointer p) => new(InstancePtr, symbols.Offset(p));

        ElementIdPtr        = Field(NativePointer.ElWdw_ElementIdPtr);
        ObjectsPtr          = Field(NativePointer.ElWdw_ObjectsPtr);
        ComponentsDataPtr   = Field(NativePointer.ElWdw_ComponentsDataPtr);
        RecentGradePtr      = Field(NativePointer.ElWdw_RecentGradePtr);
        FocusedComponentPtr = Field(NativePointer.ElWdw_FocusedComponentPtr);
        LearningModePtr     = Field(NativePointer.ElWdw_LearningModePtr);
        Components          = new TComponentData(ComponentsDataPtr, symbols);
      }

      /// <summary>Address of the <c>ElWind</c> variable (which holds the TElWind form pointer).</summary>
      public IntPtr InstancePtr { get; }

      public ObjPtr ElementIdPtr        { get; } // TElWind.LoadedElement
      public ObjPtr ObjectsPtr          { get; }
      public ObjPtr ComponentsDataPtr   { get; }
      public ObjPtr RecentGradePtr      { get; }
      public ObjPtr FocusedComponentPtr { get; } // TElWind.TheCurrentComponent
      public ObjPtr LearningModePtr     { get; }

      public TComponentData Components { get; }

      /// <summary>TElWind.EnterUpdateLock(Lock: true, var WasSwapped). Returns WasSwapped, to pass to <see cref="QuitUpdateLock" />.</summary>
      public bool EnterUpdateLock(IntPtr elementWdwPtr) => Call(NativeMethod.ElWdw_EnterUpdateLock, elementWdwPtr, true) != 0;

      public void QuitUpdateLock(IntPtr elementWdwPtr, bool wasSwapped) => Call(NativeMethod.ElWdw_QuitUpdateLock, elementWdwPtr, wasSwapped);

      /// <summary>Displays element <paramref name="elementId" /> (TElWind.NewElement).</summary>
      public bool GoToElement(IntPtr elementWdwPtr, int elementId) => TryCall(NativeMethod.ElWdw_GoToElement, elementWdwPtr, elementId);

      public bool PasteElement(IntPtr elementWdwPtr) => TryCall(NativeMethod.ElWdw_PasteElement, elementWdwPtr);

      public int AppendElement(IntPtr elementWdwPtr, ElementType elementType) =>
        TryCall(r => (int)r, -1, NativeMethod.ElWdw_AppendElement, elementWdwPtr, (byte)elementType, false);

      public int GenerateExtract(IntPtr      elementWdwPtr,
                                 ElementType elementType,
                                 bool        memorize                  = true,
                                 bool        askUserToScheduleInterval = false) =>
        CallAndWaitForCreatedElement(NativeMethod.ElWdw_GenerateExtract, elementWdwPtr, (byte)elementType, memorize, askUserToScheduleInterval, false);

      public int GenerateCloze(IntPtr elementWdwPtr, bool memorize = true, bool askUserToScheduleInterval = false) =>
        CallAndWaitForCreatedElement(NativeMethod.ElWdw_GenerateClozeDeletion, elementWdwPtr, memorize, askUserToScheduleInterval);

      public bool SetElementFromDescription(IntPtr elementWdwPtr, string elementDesc) =>
        TryCall(NativeMethod.ElWdw_AddElementFromText, elementWdwPtr, elementDesc);

      public bool DeleteCurrentElement(IntPtr elementWdwPtr) => TryCall(NativeMethod.ElWdw_DeleteCurrentElement, elementWdwPtr);

      /// <summary>Not supported: TElWind.GetText returns a Delphi-managed string the agent cannot free safely.</summary>
      public string GetText(IntPtr elementWdwPtr, IControl control) => null;

      public bool ApplyTemplate(IntPtr elementWdwPtr, int templateId, TemplateUseMode templateUseMode) =>
        TryCall(NativeMethod.ElWdw_NewTemplate, elementWdwPtr, templateId, (int)templateUseMode);

      public bool ShowNextElementInLearningQueue(IntPtr elementWdwPtr) => TryCall(NativeMethod.ElWdw_NextElementInLearningQueue, elementWdwPtr);

      public bool SetElementState(IntPtr elementWdwPtr, ElementDisplayState state) =>
        TryCall(NativeMethod.ElWdw_SetElementState, elementWdwPtr, (int)state);

      public bool PostponeRepetition(IntPtr elementWdwPtr, int interval) => TryCall(NativeMethod.PostponeRepetition, elementWdwPtr, interval);

      public bool ForceRepetitionAndResume(IntPtr elementWdwPtr, int interval, bool adjustPriority) =>
        TryCall(NativeMethod.ForceRepetitionAndResume, elementWdwPtr, interval, adjustPriority);

      public int AppendAndAddElementFromText(IntPtr elementWdwPtr, ElementType elementType, string elementDesc) =>
        TryCall(r => (int)r, -1, NativeMethod.AppendAndAddElementFromText, elementWdwPtr, (byte)elementType, elementDesc);

      /// <summary>The "Done" action without its confirmation dialog (the worker called by TElWind.DoneClick).</summary>
      public bool Done(IntPtr elementWdwPtr) => TryCall(NativeMethod.ElWdw_Done, elementWdwPtr);

      public bool PasteArticle(IntPtr elementWdwPtr) => TryCall(NativeMethod.ElWdw_PasteArticle, elementWdwPtr);

      public bool SetText(IntPtr elementWdwPtr, IControl control, string text) =>
        TryCall(NativeMethod.ElWdw_SetText, elementWdwPtr, control.Id + 1, text);

      public bool ForceRepetitionExt(IntPtr elementWdwPtr, int interval, bool adjustPriority) =>
        TryCall(NativeMethod.ElWdw_ForceRepetitionExt, elementWdwPtr, interval, adjustPriority);

      /// <summary>Runs a procedure that creates an element and returns the new element's id (or -1 on timeout or failure).</summary>
      private static int CallAndWaitForCreatedElement(NativeMethod method, params object[] args)
      {
        using var cts = new CancellationTokenSource(CreatedElementTimeout);
        try
        {
          var created = Core.SM.Registry.Element.WaitForNextCreatedElementAsync(cts.Token);
          Call(method, args);
          return created.GetAwaiter().GetResult();
        }
        catch (TaskCanceledException)
        {
          LogTo.Warning("{Method}: no element was created within {Timeout}.", method, CreatedElementTimeout);
          return -1;
        }
        catch (Exception ex)
        {
          LogTo.Error(ex, "Native method {Method} failed.", method);
          return -1;
        }
      }

      /// <summary>TComponentData: the loaded element's components.</summary>
      public class TComponentData
      {
        public TComponentData(ObjPtr componentsDataPtr, SymbolTable symbols)
        {
          ComponentDataArrOffset     = symbols.Offset(NativePointer.ElWdw_ComponentData_ComponentDataArrOffset);
          ComponentDataArrItemLength = symbols.Offset(NativePointer.ElWdw_ComponentData_ComponentDataArrItemLength);
          ComponentCountPtr          = new ObjPtr(componentsDataPtr, symbols.Offset(NativePointer.ElWdw_ComponentData_ComponentCountPtr));
          IsModifiedPtr              = new ObjPtr(componentsDataPtr, symbols.Offset(NativePointer.ElWdw_ComponentData_IsModifiedPtr));
        }

        public int    ComponentDataArrOffset     { get; }
        public int    ComponentDataArrItemLength { get; } // sizeof(TComponentDataRecord): 17 on x64
        public ObjPtr ComponentCountPtr          { get; }
        public ObjPtr IsModifiedPtr              { get; }

        public bool SetText(IntPtr componentDataPtr, IControl control, string text) =>
          TryCall(NativeMethod.TCompData_SetText, componentDataPtr, control.Id + 1, text);

        public int GetTextRegMember(IntPtr componentDataPtr, IControl control) =>
          TryCall(r => (int)r, -1, NativeMethod.TCompData_GetTextRegMember, componentDataPtr, control.Id + 1);

        public bool SetTextRegMember(IntPtr componentDataPtr, IControl control, int member) =>
          TryCall(r => r != 0, false, NativeMethod.TCompData_SetTextRegMember, componentDataPtr, control.Id + 1, member);

        public int GetImageRegMember(IntPtr componentDataPtr, IControl control) =>
          TryCall(r => (int)r, -1, NativeMethod.TCompData_GetImageRegMember, componentDataPtr, control.Id + 1);

        public bool SetImageRegMember(IntPtr componentDataPtr, IControl control, int member) =>
          TryCall(r => r != 0, false, NativeMethod.TCompData_SetImageRegMember, componentDataPtr, control.Id + 1, member);
      }
    }
  }
}
