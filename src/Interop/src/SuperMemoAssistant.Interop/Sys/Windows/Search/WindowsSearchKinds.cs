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
// 
// 
// Modified On:  2020/02/02 22:51
// Modified By:  Alexis

#endregion




using System;
// ReSharper disable CommentTypo

namespace SuperMemoAssistant.Sys.Windows.Search
{
  /// <summary>
  /// The content type to look for
  /// </summary>
  [Flags]
  [Serializable]
  public enum WindowsSearchKinds
  {
    /// <summary>
    /// KIND_CALENDAR
    /// </summary>
    Calendar = 1,

    /// <summary>
    /// KIND_COMMUNICATION
    /// </summary>
    Communication = 2,

    /// <summary>
    /// KIND_CONTACT
    /// </summary>
    Contact = 4,

    /// <summary>
    /// KIND_DOCUMENT
    /// </summary>
    Document = 8,

    /// <summary>
    /// KIND_EMAIL
    /// </summary>
    Email = 16,

    /// <summary>
    /// KIND_FEED
    /// </summary>
    Feed = 32,

    /// <summary>
    /// KIND_FOLDER
    /// </summary>
    Folder = 64,

    /// <summary>
    /// KIND_GAME
    /// </summary>
    Game = 128,

    /// <summary>
    /// KIND_INSTANTMESSAGE
    /// </summary>
    InstantMessage = 256,

    /// <summary>
    /// KIND_JOURNAL
    /// </summary>
    Journal = 512,

    /// <summary>
    /// KIND_LINK
    /// </summary>
    Link = 1024,

    /// <summary>
    /// KIND_MOVIE
    /// </summary>
    Movie = 2048,

    /// <summary>
    /// KIND_MUSIC
    /// </summary>
    Music = 4096,

    /// <summary>
    /// KIND_NOTE
    /// </summary>
    Note = 8192,

    /// <summary>
    /// KIND_PICTURE
    /// </summary>
    Picture = 16384,

    /// <summary>
    /// KIND_PROGRAM
    /// </summary>
    Program = 32768,

    /// <summary>
    /// KIND_RECORDEDTV
    /// </summary>
    RecordedTv = 131072,

    /// <summary>
    /// KIND_SEARCHFOLDER
    /// </summary>
    SearchFolder = 262144,

    /// <summary>
    /// KIND_TASK
    /// </summary>
    Task = 524288,

    /// <summary>
    /// KIND_VIDEO
    /// </summary>
    Video = 1048576,

    /// <summary>
    /// KIND_WEBHISTORY
    /// </summary>
    WebHistory = 2097152,

    /// <summary>
    /// Other
    /// </summary>
    File = 4194304,

    /// <summary>
    /// Include all types
    /// </summary>
    All = int.MaxValue
  }
}
