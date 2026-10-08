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
// Created On:   2018/12/31 05:35
// Modified On:  2018/12/31 05:35
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Linq;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models;

namespace SuperMemoAssistant.Plugins.Dictionary.Interop {
  public static class DictionaryConst
  {
    #region Constants & Statics

    public const string Loading = @"<body>
 <div style=""margin: auto""><p>Loading...</p></div>
</body>";

    public const string Error = @"<body>
 <div style=""margin: auto""><p>An error occurred while looking up the definition.</p></div>
</body>";

    public const string DefinitionRenderTemplate = @"<html>
<head>
<style>
.horizontal-line {
  margin-bottom: 10px;
  border-top: 1px solid #C9C9C9;
}
.pronunciation {
  margin-left: 2px;
  padding-left: 10px;
  border-left: 1px dotted #444;
}
.etymologies {
  font-size: 0.8em;
}
</style>
</head>
<body>
{{ #Results.0 }}
  <h3>{{ Word }}</h3>

  {{ #Results }}
  {{ #LexicalEntries }}
  <div class=horizontal-line></div>
  <div class=""lexical-entry"">
    <span><b>{{ Word }}{{ #LexicalCategory }} ({{ LexicalCategory.Text }}){{ /LexicalCategory }}</b></span>
    {{ #Pronunciations.0 }}<span class=""pronunciation"">{{ #Pronunciations }}/{{ F_Escape PhoneticSpelling }}/ {{ /Pronunciations }}</span>{{ /Pronunciations.0 }}
    <ol>
    {{ #Entries }}
      {{ #Senses }}{{ #Definitions.0 }}
      <li>
        {{ F_Escape Definitions.0 }}
        {{ #Exemples.0 }}
        <ul>
        {{ #Exemples }}
          <li><i>{{ F_Escape Text }}</i></li>
        {{ /Exemples }}
        </ul>
        {{ /Exemples.0 }}
      </li>
      {{ /Definitions.0 }}{{ /Senses }}
    {{ /Entries }}
    </ol>
    
    {{ #HasEtymologies }}
    <p class=""etymologies""><u>Etymologies</u>:
    {{ #Entries }}
      {{ #Etymologies }}<br/>- {{ F_Escape . }}{{ /Etymologies }}
    {{ /Entries }}
    </p>
    {{ /HasEtymologies }}
  </div>
  {{ /LexicalEntries }}
  {{ /Results }}

{{ /Results.0 }}
</body>
</html>";


    public const double DefaultExtractPriority = 30.0;
    
    public static IReadOnlyDictionary<string, OxfordDictionary> MonolingualDictionaries { get; } =
      OxfordLanguagesJson.Deserialize<List<OxfordDictionary>>()
                         .Where(d => d.Type == DictionaryType.Monolingual)
                         .ToDictionary(d => d.ToString());

    public static ISet<string> AllMonolingualLanguages { get; } =
      OxfordLanguagesJson.Deserialize<List<OxfordDictionary>>()
                         .Where(d => d.Type == DictionaryType.Monolingual)
                         .Select(d => d.GetLanguageId())
                         .ToHashSet();


    public static OxfordDictionary DefaultDictionary { get; } =
      MonolingualDictionaries.SafeRead(DefaultDictionaryStr) ?? MonolingualDictionaries.Values.FirstOrDefault();

    public const string DefaultDictionaryStr = "British English Dictionary (ODE)";
    public const string OxfordLanguagesJson = @"[
    {
      ""source"": ""English-Spanish Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""es""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Igbo Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""ig""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Romanian Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""ro""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Tatar Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""tt""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-isiZulu Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""zu""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Spanish Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""es""
      },
      ""targetLanguage"": {
        ""id"": ""es""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""Indonesian-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""id""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Northern Sotho-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""nso""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Telugu-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""te""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Turkmen-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""tk""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""other""
    },
    {
      ""source"": ""Setswana-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""tn""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Tok Pisin-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""tpi""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Chinese-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""zh""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Turkmen Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""tk""
      },
      ""type"": ""other""
    },
    {
      ""source"": ""Spanish-Quechua(Southern Bolivian) Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""es""
      },
      ""targetLanguage"": {
        ""id"": ""qu""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Italian-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""it""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Greek-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""el""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Greek Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""el""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Malay Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""ms""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Setswana Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""tn""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Chinese Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""zh""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Malay-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""ms""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Quechua(Southern Bolivian)-Spanish Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""qu""
      },
      ""targetLanguage"": {
        ""id"": ""es""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""isiXhosa-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""xh""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""German-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""de""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""region"": ""us"",
      ""source"": ""American English Dictionary (NOAD)"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""English-German Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""de""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Indonesian Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""id""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Italian Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""it""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Portuguese Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""pt""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Tajik Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""tg""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Tok Pisin Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""tpi""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-isiXhosa Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""xh""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Spanish-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""es""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Gujarati Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""gu""
      },
      ""targetLanguage"": {
        ""id"": ""gu""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""Hindi Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""hi""
      },
      ""targetLanguage"": {
        ""id"": ""hi""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""Latvian Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""lv""
      },
      ""targetLanguage"": {
        ""id"": ""lv""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""Portuguese-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""pt""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Swahili Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""sw""
      },
      ""targetLanguage"": {
        ""id"": ""sw""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""Tamil-Tamil-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""ta""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Tatar-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""tt""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Urdu-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""ur""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    },
    {
      ""region"": ""gb"",
      ""source"": ""British English Dictionary (ODE)"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""English-Northern Sotho Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""nso""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""English-Yoruba Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""en""
      },
      ""targetLanguage"": {
        ""id"": ""yo""
      },
      ""type"": ""bilingual""
    },
    {
      ""source"": ""Romanian Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""ro""
      },
      ""targetLanguage"": {
        ""id"": ""ro""
      },
      ""type"": ""monolingual""
    },
    {
      ""source"": ""isiZulu-English Dictionary"",
      ""sourceLanguage"": {
        ""id"": ""zu""
      },
      ""targetLanguage"": {
        ""id"": ""en""
      },
      ""type"": ""bilingual""
    }
  ]";

    #endregion
  }
}
