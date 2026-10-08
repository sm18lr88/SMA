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
// Modified On:  2020/01/15 12:11
// Modified By:  Alexis

#endregion




using System;
using System.Linq;

namespace SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models
{
  #region Shared

  [Serializable]
  public class Metadata
  {
    #region Properties & Fields - Public

    public string Provider { get; set; }

    #endregion
  }

  [Serializable]
  public class TextTypeDTO
  {
    #region Properties & Fields - Public

    public string Id   { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }

    #endregion
  }

  [Serializable]
  public class TextDTO
  {
    #region Properties & Fields - Public

    public string Id   { get; set; }
    public string Text { get; set; }

    #endregion
  }

  #endregion




  #region Entries

  //
  //
  // Entries

  [Serializable]
  public class Example
  {
    #region Properties & Fields - Public

    public string[]      Definitions { get; set; }
    public TextTypeDTO[] Notes       { get; set; }
    public TextDTO[]     Regions     { get; set; }
    public TextDTO[]     Registers   { get; set; }
    public string        Text        { get; set; }

    #endregion
  }

  [Serializable]
  public class Subsens
  {
    #region Properties & Fields - Public

    public string[]  Definitions { get; set; }
    public Example[] Examples    { get; set; }
    public string    Id          { get; set; }
    public TextDTO[]  Domains     { get; set; }
    public TextDTO[]  Regions     { get; set; }
    public TextDTO[]  Registers   { get; set; }

    #endregion
  }

  [Serializable]
  public class Sense
  {
    #region Properties & Fields - Public

    public string[]        Definitions      { get; set; }
    public TextDTO[]       Domains          { get; set; }
    public string[]        Etymologies      { get; set; }
    public Example[]       Examples         { get; set; }
    public string          Id               { get; set; }
    public TextTypeDTO[]   Notes            { get; set; }
    public Pronunciation[] Pronunciations   { get; set; }
    public string[]        ShortDefinitions { get; set; }
    public Subsens[]       Subsenses        { get; set; }
    public TextDTO[]       Registers        { get; set; }

    #endregion
  }

  [Serializable]
  public class Entry
  {
    #region Properties & Fields - Public

    public string[]        Etymologies         { get; set; }
    public TextTypeDTO[]   GrammaticalFeatures { get; set; }
    public string          HomographNumber     { get; set; }
    public Pronunciation[] Pronunciations      { get; set; }
    public Sense[]         Senses              { get; set; }

    #endregion
  }

  [Serializable]
  public class Pronunciation
  {
    #region Properties & Fields - Public

    public string    AudioFile        { get; set; }
    public string[]  Dialects         { get; set; }
    public string    PhoneticNotation { get; set; }
    public string    PhoneticSpelling { get; set; }
    public TextDTO[] Regions          { get; set; }
    public TextDTO[] Registers        { get; set; }

    #endregion
  }

  [Serializable]
  public class LexicalEntry
  {
    #region Properties & Fields - Public

    public Entry[]         Entries             { get; set; }
    public TextTypeDTO[]   GrammaticalFeatures { get; set; }
    public string          Language            { get; set; }
    public TextDTO         LexicalCategory     { get; set; }
    public TextTypeDTO[]   Notes               { get; set; }
    public Pronunciation[] Pronunciations      { get; set; }
    public string          Text                { get; set; }
    public bool            HasEtymologies      => Entries?.Any(e => e?.Etymologies?.Any() ?? false) ?? false;

    #endregion
  }

  [Serializable]
  public class HeadwordEntry
  {
    #region Properties & Fields - Public

    public string          Id             { get; set; }
    public string          Language       { get; set; }
    public LexicalEntry[]  LexicalEntries { get; set; }
    public Pronunciation[] Pronunciations { get; set; }
    public string          Type           { get; set; }
    public string          Word           { get; set; }

    #endregion
  }

  [Serializable]
  public class EntryResult
  {
    #region Properties & Fields - Public

    public Metadata        Metadata { get; set; }
    public HeadwordEntry[] Results  { get; set; }

    #endregion
  }

  #endregion




  #region Lemmas

  //
  //
  // Lemmas

  [Serializable]
  public class InflectionOrCategory
  {
    #region Properties & Fields - Public

    public string Id   { get; set; }
    public string Text { get; set; }

    #endregion
  }

  [Serializable]
  public class LemmatronLexicalEntry
  {
    #region Properties & Fields - Public

    public TextTypeDTO[]          GrammaticalFeatures { get; set; }
    public InflectionOrCategory[] InflectionOf        { get; set; }
    public string                 Language            { get; set; }
    public InflectionOrCategory   LexicalCategory     { get; set; }
    public string                 Text                { get; set; }

    #endregion
  }

  [Serializable]
  public class HeadwordLemmatron
  {
    #region Properties & Fields - Public

    public string                  Id             { get; set; }
    public string                  Language       { get; set; }
    public LemmatronLexicalEntry[] LexicalEntries { get; set; }
    public string                  Type           { get; set; }
    public string                  Word           { get; set; }

    #endregion
  }

  [Serializable]
  public class LemmatronResult
  {
    #region Properties & Fields - Public

    public Metadata            Metadata { get; set; }
    public HeadwordLemmatron[] Results  { get; set; }

    #endregion
  }

  #endregion
}
