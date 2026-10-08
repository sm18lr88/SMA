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




// ReSharper disable PossibleNullReferenceException
// ReSharper disable InconsistentNaming

namespace SuperMemoAssistant.Interop
{
  using System;
  using System.Diagnostics.CodeAnalysis;
  using System.Windows.Media;

  /// <summary>Contains SM-related constants (not to be mixed up with to <see cref="SMAConst" />)</summary>
  public static class SMConst
  {
    /// <summary>SuperMemo stylesheet well-known values</summary>
    public static class Stylesheet
    {
      #region Constants & Statics

      /// <summary>
      ///   The background color of the text after it has been used to create a cloze (not to be mixed up with [...])
      /// </summary>
      public static readonly Color ExtractClozedBackgroundColor = (Color)ColorConverter.ConvertFromString("#E67300");

      /// <summary>The background color used for text after it has been extracted</summary>
      public static readonly Color ExtractBackgroundColor = (Color)ColorConverter.ConvertFromString("#44C2FF");

      /// <summary>The background color used for text after it has been extracted, with transparency</summary>
      public static readonly Color ExtractTransparentColor = (Color)ColorConverter.ConvertFromString("#8044C2FF");

      /// <summary>The background color used for text that has been ignored</summary>
      public static readonly Color IgnoreColor = (Color)ColorConverter.ConvertFromString("#DAB6B6");

      #endregion
    }

    /// <summary>Elements-related SM well-known values</summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public static class Elements
    {
      #region Constants & Statics

      /// <summary>The default number children a node can withstand before overflowing</summary>
      public const int DefaultChildrenPerNode = 100;

      /// <summary>
      ///   Template for the reference html. Use <see cref="O:string.Format()" /> with a single string containing the reference
      ///   items
      /// </summary>
      public const string ReferenceFormat =
        @"<br><br><hr SuperMemo><SuperMemoReference><H5 dir=ltr align=left><FONT style=""COLOR: transparent"" size=1>#SuperMemo Reference:</FONT><BR><FONT class=reference>{0}</FONT></SuperMemoReference>";

      #endregion
    }

    /// <summary>SM well-known folder names</summary>
    public static class Paths
    {
      #region Constants & Statics

      /// <summary>The elements folder. Contains html and other documents</summary>
      public const string ElementsFolder = "elements";

      /// <summary>The info folder. Contains data about the element tree, repetition history, etc.</summary>
      public const string InfoFolder = "info";

      /// <summary>
      ///   The registry folder. Contains all the registry members (excluding elements, which are in <see cref="InfoFolder" />
      /// </summary>
      public const string RegistryFolder = "registry";

      /// <summary>
      /// The recovery folder. Contains files that would otherwise automatically have been deleted (e.g. orphaned elements).
      /// </summary>
      public const string RecoverFolder = "recover";

      #endregion
    }

    /// <summary>SM well-known file names</summary>
    public static class Files
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
      public const string EmptySlotsFile       = "emptyslots.dat";
      public const string BinaryMemFileName    = "program.mem";
      public const string BinaryRtxFileName    = "program.rtx";
      public const string ConceptMemFileName   = "concept.mem";
      public const string ConceptRtxFileName   = "concept.rtx";
      public const string ContentsFileName     = "contents.dat";
      public const string ElementsInfoFileName = "elementinfo.dat";
      public const string ImageMemFileName     = "image.mem";
      public const string ImageRtxFileName     = "image.rtx";
      public const string SoundMemFileName     = "sound.mem";
      public const string SoundRtxFileName     = "sound.rtx";
      public const string TemplateMemFileName  = "template.mem";
      public const string TemplateRtxFileName  = "template.rtx";
      public const string TextMemFileName      = "text.mem";
      public const string TextRtfFileName      = "text.rtf";
      public const string TextRtxFileName      = "text.rtx";
      public const string VideoMemFileName     = "video.mem";
      public const string VideoRtxFileName     = "video.rtx";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>SM well known UI values</summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public static class UI
    {
      #region Constants & Statics

      /// <summary>The ElementData window class name</summary>
      public const string ElementDataWindowClassName = "TElDataWind";

      /// <summary>The element window class name</summary>
      public const string ElementWindowClassName = "TElWind";

      /// <summary>The tree view class name (which coincides with the main window)</summary>
      public const string SMMainClassName = "TSMMain";

      /// <summary>The main menu class name, unreliable</summary>
      [Obsolete("Unreliable")] public const string MainMenuItemClassName = "#32768";

      #endregion
    }
  }
}
