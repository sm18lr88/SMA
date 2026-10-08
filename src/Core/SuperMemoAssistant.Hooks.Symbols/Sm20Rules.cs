// Name-based resolution rules for SuperMemo 20: which Delphi RTTI method or field backs each native symbol.
namespace SuperMemoAssistant.Hooks.Symbols;

using SuperMemoAssistant.SuperMemo;

internal static class Sm20Rules
{
  /// <summary>Methods that extended RTTI names directly (Class, Method).</summary>
  public static readonly IReadOnlyDictionary<NativeMethod, (string Class, string Method)> RttiMethods =
    new Dictionary<NativeMethod, (string, string)>
    {
      [NativeMethod.ElWdw_GoToElement]                  = ("TElWind", "NewElement"),
      [NativeMethod.ElWdw_PasteElement]                 = ("TElWind", "PasteElement"),
      [NativeMethod.ElWdw_AppendElement]                = ("TElWind", "AppendElement"),
      [NativeMethod.ElWdw_GenerateExtract]              = ("TElWind", "GenerateExtract"),
      [NativeMethod.ElWdw_GenerateClozeDeletion]        = ("TElWind", "GenerateClozeDeletion"),
      [NativeMethod.ElWdw_AddElementFromText]           = ("TElWind", "AddElementFromText"),
      [NativeMethod.ElWdw_DeleteCurrentElement]         = ("TElWind", "DeleteCurrentElement"),
      [NativeMethod.ElWdw_GetText]                      = ("TElWind", "GetText"),
      [NativeMethod.ElWdw_EnterUpdateLock]              = ("TElWind", "EnterUpdateLock"),
      [NativeMethod.ElWdw_QuitUpdateLock]               = ("TElWind", "QuitUpdateLock"),
      [NativeMethod.ElWdw_PasteArticle]                 = ("TElWind", "PasteArticle"),
      // SM20's Forget/GoRandomTest end with "if LearningMode <> 0 then DrillNextElement".
      [NativeMethod.ElWdw_NextElementInLearningQueue]   = ("TElWind", "DrillNextElement"),
      [NativeMethod.ElWdw_SetElementState]              = ("TElWind", "SetElementState"),
      [NativeMethod.ElWdw_ScheduleInInterval]           = ("TElWind", "ScheduleInInterval"),
      [NativeMethod.ElWdw_ExecuteUncommittedRepetition] = ("TElWind", "ExecuteUncommittedRepetition"),
      [NativeMethod.ElWdw_ForceRepetitionExt]           = ("TElWind", "ForceRepetitionExt"),
      [NativeMethod.ElWdw_NewTemplate]                  = ("TElWind", "NewTemplate"),
      [NativeMethod.TCompData_GetType]                  = ("TComponentData", "GetType"),
      [NativeMethod.TCompData_GetText]                  = ("TComponentData", "GetText"),
      [NativeMethod.TCompData_SetText]                  = ("TComponentData", "SetText"),
      [NativeMethod.TCompData_GetTextRegMember]         = ("TComponentData", "GetTextItem"),
      [NativeMethod.TCompData_SetTextRegMember]         = ("TComponentData", "SetTextItem"),
      [NativeMethod.TCompData_GetImageRegMember]        = ("TComponentData", "GetImageItem"),
      [NativeMethod.TCompData_SetImageRegMember]        = ("TComponentData", "SetImageItem"),
      [NativeMethod.TSMMain_SelectDefaultConcept]       = ("TSMMain", "SelectDefaultConcept"),
      [NativeMethod.TSMMain_NewTitle]                   = ("TSMMain", "NewTitle"),
      [NativeMethod.TRegistry_AddMember]                = ("TRegistry", "AddMember"),
      [NativeMethod.TRegistry_ImportFile]               = ("TRegistry", "ImportFile"),
      [NativeMethod.FileSpace_GetTopSlot]               = ("TFilespace", "GetTopSlot"),
      [NativeMethod.FileSpace_IsSlotOccupied]           = ("TFilespace", "IsSlotOccupied"),
      [NativeMethod.Queue_GetItem]                      = ("TQueue", "GetItem"),
    };

  /// <summary>Instance fields that extended RTTI names directly (Class, Field).</summary>
  public static readonly IReadOnlyDictionary<NativePointer, (string Class, string Field)> RttiFields =
    new Dictionary<NativePointer, (string, string)>
    {
      [NativePointer.ElWdw_ElementIdPtr]                         = ("TElWind", "LoadedElement"),
      [NativePointer.ElWdw_ObjectsPtr]                           = ("TElWind", "Objects"),
      [NativePointer.ElWdw_ComponentsDataPtr]                    = ("TElWind", "ComponentData"),
      [NativePointer.ElWdw_RecentGradePtr]                       = ("TElWind", "RecentGrade"),
      [NativePointer.ElWdw_FocusedComponentPtr]                  = ("TElWind", "TheCurrentComponent"),
      [NativePointer.ElWdw_LearningModePtr]                      = ("TElWind", "LearningMode"),
      [NativePointer.Registry_FileSpaceInstance]                 = ("TDatabase", "Filespace"),
      [NativePointer.Registry_TextRegistryInstance]              = ("TDatabase", "TextRegistry"),
      [NativePointer.Registry_ImageRegistryInstance]             = ("TDatabase", "ImageRegistry"),
      [NativePointer.Registry_SoundRegistryInstance]             = ("TDatabase", "SoundRegistry"),
      [NativePointer.Registry_VideoRegistryInstance]             = ("TDatabase", "VideoRegistry"),
      [NativePointer.Registry_BinaryRegistryInstance]            = ("TDatabase", "BinaryRegistry"),
      [NativePointer.Registry_TemplateRegistryInstance]          = ("TDatabase", "TemplateRegistry"),
      [NativePointer.Registry_ConceptRegistryInstance]           = ("TDatabase", "ConceptRegistry"),
      [NativePointer.FileSpace_EmptySlotsOffset]                 = ("TFilespace", "EmptySlots"),
      [NativePointer.FileSpace_AllocatedSlotsOffset]             = ("TFilespace", "AllocatedSlots"),
      [NativePointer.ElWdw_ComponentData_ComponentDataArrOffset] = ("TComponentData", "ComponentArray"),
      [NativePointer.ElWdw_ComponentData_ComponentCountPtr]      = ("TComponentData", "DComponentNo"),
      [NativePointer.ElWdw_ComponentData_IsModifiedPtr]          = ("TComponentData", "TheModifiedStatus"),
      [NativePointer.Control_ParentOffset]                       = ("TControl", "FParent"),
      [NativePointer.Control_WindowProcOffset]                   = ("TControl", "FWindowProc"),
      [NativePointer.Control_LeftOffset]                         = ("TControl", "FLeft"),
      [NativePointer.Control_TopOffset]                          = ("TControl", "FTop"),
      [NativePointer.Control_WidthOffset]                        = ("TControl", "FWidth"),
      [NativePointer.Control_HeightOffset]                       = ("TControl", "FHeight"),
      [NativePointer.Control_HandleOffset]                       = ("TWinControl", "FHandle"),
      [NativePointer.Queue_SizeOffset]                           = ("TQueue", "Size"),
      [NativePointer.Application_OnMessageOffset]                = ("TApplication", "FOnMessage"),
    };

  /// <summary>The current concept globals are fields of one TConcept record variable (Field name in the TConcept record).</summary>
  public static readonly IReadOnlyDictionary<NativePointer, string> ConceptRecordFields =
    new Dictionary<NativePointer, string>
    {
      [NativePointer.Globals_CurrentConceptGroupIdPtr] = "TheElement",
      [NativePointer.Globals_CurrentRootIdPtr]         = "Root",
      [NativePointer.Globals_CurrentHookIdPtr]         = "Hook",
      [NativePointer.Globals_CurrentConceptIdPtr]      = "Posit",
    };
}
