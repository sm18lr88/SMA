namespace SuperMemoAssistant.Pdfium;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Native;

public sealed unsafe partial class PdfForms
{
  /// <summary>
  ///   Allocates FPDF_FORMFILLINFO followed by one pointer-sized slot that holds the owner's GCHandle, so each callback can
  ///   find its <see cref="PdfForms" /> from the pThis pointer that PDFium passes.
  /// </summary>
  private static FormFillInfo* AllocateInfo(IntPtr owner)
  {
    var info = (FormFillInfo*)NativeMemory.AllocZeroed((nuint)(sizeof(FormFillInfo) + sizeof(IntPtr)));
    info->Version = 1;
    info->Invalidate = &OnInvalidate;
    info->OutputSelectedRect = &OnOutputSelectedRect;
    info->SetCursor = &OnSetCursor;
    info->SetTimer = &OnSetTimer;
    info->KillTimer = &OnKillTimer;
    info->GetPage = &OnGetPage;
    info->GetCurrentPage = &OnGetCurrentPage;
    info->GetRotation = &OnGetRotation;
    info->ExecuteNamedAction = &OnExecuteNamedAction;
    info->DoGoToAction = &OnDoGoToAction;
    *(IntPtr*)(info + 1) = owner;
    return info;
  }

  private static PdfForms Owner(FormFillInfo* info) => (PdfForms)GCHandle.FromIntPtr(*(IntPtr*)(info + 1)).Target!;

  private static void Guard(Action action)
  {
    try
    {
      action();
    }
    catch (Exception ex)
    {
      CallbackErrors.Report(ex);
    }
  }

  private void RaiseForPage(EventHandler<InvalidatePageEventArgs>? handler, IntPtr page, PdfRect rect)
  {
    if (handler != null && _document?.Pages.FindLoaded(page) is { } pdfPage)
      handler(this, new InvalidatePageEventArgs(pdfPage, rect));
  }

  [UnmanagedCallersOnly]
  private static void OnInvalidate(FormFillInfo* info, IntPtr page, double left, double top, double right, double bottom) =>
    Guard(() =>
    {
      var forms = Owner(info);
      forms.RaiseForPage(forms.Invalidate, page, new PdfRect((float)left, (float)top, (float)right, (float)bottom));
    });

  [UnmanagedCallersOnly]
  private static void OnOutputSelectedRect(FormFillInfo* info, IntPtr page, double left, double top, double right,
                                           double bottom) =>
    Guard(() =>
    {
      var forms = Owner(info);
      forms.RaiseForPage(forms.OutputSelectedRect, page, new PdfRect((float)left, (float)top, (float)right, (float)bottom));
    });

  [UnmanagedCallersOnly]
  private static void OnSetCursor(FormFillInfo* info, int cursor) =>
    Guard(() =>
    {
      var forms = Owner(info);
      forms.SetCursor?.Invoke(forms, new SetCursorEventArgs((CursorType)cursor));
    });

  [UnmanagedCallersOnly]
  private static int OnSetTimer(FormFillInfo* info, int elapse, IntPtr callback)
  {
    var id = 0;
    Guard(() =>
    {
      var forms = Owner(info);
      if (forms.SynchronizationContext is not { } context)
        return;

      var timerId = id = ++forms._lastTimerId;
      forms._timers[timerId] = new Timer(_ => context.Post(_ => forms.Tick(timerId, callback), null), null, elapse, elapse);
    });

    return id;
  }

  [UnmanagedCallersOnly]
  private static void OnKillTimer(FormFillInfo* info, int id) =>
    Guard(() =>
    {
      var forms = Owner(info);
      if (forms._timers.Remove(id, out var timer))
        timer.Dispose();
    });

  [UnmanagedCallersOnly]
  private static IntPtr OnGetPage(FormFillInfo* info, IntPtr document, int index)
  {
    var page = IntPtr.Zero;
    Guard(() => page = Owner(info)._document?.Pages[index].Handle ?? IntPtr.Zero);
    return page;
  }

  [UnmanagedCallersOnly]
  private static IntPtr OnGetCurrentPage(FormFillInfo* info, IntPtr document)
  {
    var page = IntPtr.Zero;
    Guard(() => page = Owner(info)._document?.Pages.CurrentPage.Handle ?? IntPtr.Zero);
    return page;
  }

  [UnmanagedCallersOnly]
  private static int OnGetRotation(FormFillInfo* info, IntPtr page) => 0;

  [UnmanagedCallersOnly]
  private static void OnExecuteNamedAction(FormFillInfo* info, byte* name)
  {
    var action = Marshal.PtrToStringUTF8((IntPtr)name);
    Guard(() =>
    {
      var forms = Owner(info);
      if (forms._document is not { } document)
        return;

      int? target = action switch
      {
        "NextPage" => document.Pages.CurrentIndex + 1,
        "PrevPage" => document.Pages.CurrentIndex - 1,
        "FirstPage" => 0,
        "LastPage" => document.Pages.Count - 1,
        _ => null,
      };

      if (target is { } index && index >= 0 && index < document.Pages.Count)
        forms.GoToPage?.Invoke(forms, new GoToPageEventArgs(index));
    });
  }

  [UnmanagedCallersOnly]
  private static void OnDoGoToAction(FormFillInfo* info, int pageIndex, int zoomMode, float* position, int positionCount) =>
    Guard(() =>
    {
      var forms = Owner(info);
      forms.GoToPage?.Invoke(forms, new GoToPageEventArgs(pageIndex));
    });

  private void Tick(int timerId, IntPtr callback)
  {
    if (_timers.ContainsKey(timerId))
      ((delegate* unmanaged<int, void>)callback)(timerId);
  }
}
