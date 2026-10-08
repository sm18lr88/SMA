#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo
{
  using System;
  using Content.Components;
  using Core;
  using Elements;
  using Registry.Types;
  using UI.Element;

  /// <summary>
  /// The main SuperMemo service interface. All SM functionalities are accessed through this interface.
  /// </summary>
  public interface ISuperMemo
  {
    /// <summary>The running SuperMemo executable's version</summary>
    Version AppVersion { get; }

    /// <summary>The loaded collection</summary>
    SMCollection Collection { get; }

    /// <summary>SuperMemo executable process ID</summary>
    int ProcessId { get; }

    /// <summary>Seems to prevent some confirmation dialogs from being displayed.</summary>
    bool IgnoreUserConfirmation { get; set; }

    /// <summary>All SuperMemo registries</summary>
    ISuperMemoRegistry Registry { get; }

    /// <summary>All SuperMemo windows and functionalities</summary>
    ISuperMemoUI UI { get; }
  }

  /// <summary>Lists SuperMemo registry and enables access to their functionalities.</summary>
  public interface ISuperMemoRegistry
  {
    /// <summary>The element registry</summary>
    IElementRegistry Element { get; }

    /// <summary>The binary registry (e.g. for pdf files)</summary>
    IBinaryRegistry Binary { get; }

    /// <summary>The component registry</summary>
    IComponentRegistry Component { get; }

    /// <summary>The concept registry</summary>
    IConceptRegistry Concept { get; }

    /// <summary>The Text registry</summary>
    ITextRegistry Text { get; }

    /// <summary>The image registry</summary>
    IImageRegistry Image { get; }

    /// <summary>The sound registry</summary>
    ISoundRegistry Sound { get; }

    /// <summary>The video registry</summary>
    IVideoRegistry Video { get; }

    /// <summary>The template registry</summary>
    ITemplateRegistry Template { get; }
  }

  /// <summary>Lists SuperMemo windows and enables access to their functionalities.</summary>
  public interface ISuperMemoUI
  {
    /// <summary>
    /// The element window (enable e.g. editing text, etc.)
    /// </summary>
    IElementWdw ElementWdw { get; }
  }
}
