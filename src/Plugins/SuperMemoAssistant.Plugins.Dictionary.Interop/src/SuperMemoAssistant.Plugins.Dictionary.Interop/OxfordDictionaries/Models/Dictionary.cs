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
// Created On:   2019/09/03 18:15
// Modified On:  2020/01/15 17:14
// Modified By:  Alexis

#endregion




using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models
{
  /// <summary>A type of dictionary</summary>
  [Serializable]
  public enum DictionaryType
  {
    Monolingual,
    Bilingual,
    Other
  }

  [Serializable]
  public class OxfordDictionaryLanguage
  {
    #region Properties & Fields - Public

    public string Id       { get; set; }
    public string Language { get; set; }

    #endregion
  }

  /// <summary>A class that hold information about dictionary</summary>
  [Serializable]
  public class OxfordDictionary
  {
    #region Properties & Fields - Public

    /// <summary>The region e.g. gb or us, this property maybe null</summary>
    public string Region { get; set; }

    /// <summary>The source of this dictionary</summary>
    public string Source { get; set; }

    /// <summary>The source language of this dictionary in IANA language code</summary>
    public OxfordDictionaryLanguage SourceLanguage { get; set; }

    /// <summary>
    ///   The translate to language of this dictionary in IANA language code, this property is
    ///   null if Type is Monolingual
    /// </summary>
    public OxfordDictionaryLanguage TargetLanguage { get; set; }

    // The type of this dictionary
    [JsonConverter(typeof(StringEnumConverter))]
    public DictionaryType Type { get; set; }

    #endregion




    #region Methods Impl

    public override string ToString()
    {
      return Source;
    }

    #endregion




    #region Methods

    public string GetLanguageId()
    {
      switch (Type)
      {
        case DictionaryType.Monolingual:
          return $"{SourceLanguage.Id}-{Region}";

        case DictionaryType.Bilingual:
          return $"{SourceLanguage.Id}-{TargetLanguage.Id}";

        default:
          throw new NotSupportedException();
      }
    }

    #endregion
  }
}
