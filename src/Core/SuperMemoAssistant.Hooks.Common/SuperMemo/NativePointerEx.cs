// Classifies NativePointer entries: unit globals are RVAs into sm20.exe, everything else is a field offset or size.
namespace SuperMemoAssistant.SuperMemo
{
  public static class NativePointerEx
  {
    public static bool IsGlobal(this NativePointer pointer) => pointer switch
    {
      NativePointer.ElWdw_InstancePtr                 => true,
      NativePointer.SMMain_InstancePtr                => true,
      NativePointer.Database_InstancePtr              => true,
      NativePointer.Application_InstancePtr           => true,
      NativePointer.Globals_LimitChildrenCountPtr     => true,
      NativePointer.Globals_CurrentConceptGroupIdPtr  => true,
      NativePointer.Globals_CurrentRootIdPtr          => true,
      NativePointer.Globals_CurrentHookIdPtr          => true,
      NativePointer.Globals_CurrentConceptIdPtr       => true,
      NativePointer.Globals_IgnoreUserConfirmationPtr => true,
      _                                               => false,
    };
  }
}
