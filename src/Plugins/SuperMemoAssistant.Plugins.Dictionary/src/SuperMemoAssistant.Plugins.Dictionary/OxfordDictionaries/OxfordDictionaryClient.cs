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
// Created On:   2018/12/31 01:03
// Modified On:  2018/12/31 01:22
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Anotar.Serilog;
using Newtonsoft.Json;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models;

// ReSharper disable InconsistentNaming

namespace SuperMemoAssistant.Plugins.Dictionary.OxfordDictionaries
{
  using System.Globalization;

  public class OxfordDictionaryClient : IDisposable
  {
    #region Properties & Fields - Non-Public

    private readonly HttpClient _httpClient;

    #endregion




    #region Constructors

    /// <summary>
    ///   Initializes a new instance of the OxfordDictionaryClient class.  This instance should
    ///   be reused.
    /// </summary>
    public OxfordDictionaryClient()
    {
      _httpClient = new HttpClient();
      _httpClient.DefaultRequestHeaders.Accept.Clear();
      _httpClient.BaseAddress = new Uri("https://od-api.oxforddictionaries.com/api/v2/");
      _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void Dispose()
    {
      _httpClient?.Dispose();
    }

    #endregion




    #region Methods

    public void SetAuthentication(string appId,
                                  string appKey)
    {
      _httpClient.DefaultRequestHeaders.Remove("app_id");
      _httpClient.DefaultRequestHeaders.Remove("app_key");

      _httpClient.DefaultRequestHeaders.Add("app_id", appId);
      _httpClient.DefaultRequestHeaders.Add("app_key", appKey);
    }

    /// <summary>
    ///   Retrieve available dictionary entries for a given word and language. Return
    ///   SearchResult of a given word. Return null if not found.
    /// </summary>
    /// <param name="ct">CancellationToken use for cancel</param>
    /// <param name="word">
    ///   A word that want to search.  It should be lowercase and should replace
    ///   whitespace with underscore
    /// </param>
    /// <param name="language">Abbreviated name of language, must be lowercase</param>
    /// <returns>SearchResult of a given word. Return null if not found</returns>
    /// <exception cref="HttpRequestException">
    ///   Throw when underlying HTTPClient throw exception other
    ///   than 404 HTTP error
    /// </exception>
    public async Task<EntryResult> LookupEntry(CancellationToken ct,
                                               string            word,
                                               string            language = "en-gb")
    {
      try
      {
        if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(language))
          throw new ArgumentNullException(nameof(word));

        word = word.Trim()
                   .Replace(" ", "_")
                   .ToLowerInvariant();
        var searchPath = $"entries/{language}/{word}";

        var jsonString = await SendHttpGetRequest(searchPath, ct);

        if (jsonString == null)
          return null;

        return JsonConvert.DeserializeObject<EntryResult>(jsonString);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception caught while looking up entry for word {Word}", word);
        throw;
      }
    }

    /// <summary>
    ///   Use this to check if a word exists in the dictionary, or what 'root' form it links to
    ///   (e.g., swimming > swim). The response tells you the possible lemmas for a given inflected
    ///   word.This can then be combined with other endpoints to retrieve more information.
    /// </summary>
    /// <param name="ct">CancellationToken use for cancel</param>
    /// <param name="word">
    ///   A word that want to search.  It should be lowercase and should replace
    ///   whitespace with underscore
    /// </param>
    /// <param name="language">Abbreviated name of language, must be lowercase</param>
    /// <returns>SearchResult of a given word. Return null if not found</returns>
    /// <exception cref="HttpRequestException">
    ///   Throw when underlying HTTPClient throw exception other
    ///   than 404 HTTP error
    /// </exception>
    public async Task<LemmatronResult> LookupLemma(CancellationToken ct,
                                                   string            word,
                                                   string            language = "en-gb")
    {
      try
      {
        if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(language))
          throw new ArgumentNullException(nameof(word));

        word = word.Trim()
                   .Replace(" ", "_")
                   .ToLowerInvariant();
        var searchPath = $"lemmas/{language}/{word}";

        var jsonString = await SendHttpGetRequest(searchPath, ct);

        if (jsonString == null)
          return null;

        return JsonConvert.DeserializeObject<LemmatronResult>(jsonString);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, $"Exception caught while looking up lemma for word {word}");
        throw;
      }
    }

    /// <summary>Get available dictionaries in Oxford Dictionary API, return null if not found</summary>
    /// <param name="ct">CancellationToken use for cancel</param>
    /// <param name="sourceLanguage">
    ///   IANA language code. If provided output will be filtered by
    ///   sourceLanguage
    /// </param>
    /// <param name="targetLanguage">
    ///   IANA language code. If provided output will be filtered by
    ///   targetLanguage
    /// </param>
    /// <returns>List of OxfordDictionary</returns>
    /// <exception cref="HttpRequestException">
    ///   Throw when underlying HTTPClient throw exception other
    ///   than 404 HTTP error
    /// </exception>
    public async Task<List<OxfordDictionary>> GetAvailableDictionaries(CancellationToken ct,
                                                                       string            sourceLanguage = null,
                                                                       string            targetLanguage = null)
    {
      try
      {
        var path = "languages";

        if (!string.IsNullOrWhiteSpace(sourceLanguage) && string.IsNullOrWhiteSpace(targetLanguage))
          path += $"?sourceLanguage={sourceLanguage}";
        else if (string.IsNullOrWhiteSpace(sourceLanguage) && !string.IsNullOrWhiteSpace(targetLanguage))
          path += $"?targetLanguage={targetLanguage}";
        else if (!string.IsNullOrWhiteSpace(sourceLanguage) && !string.IsNullOrWhiteSpace(targetLanguage))
          path += $"?sourceLanguage={sourceLanguage}&targetLanguage={targetLanguage}";

        string jsonString = await SendHttpGetRequest(path, ct);

        return jsonString?.Deserialize<List<OxfordDictionary>>();
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception caught while fetching available dictionaries");
        throw;
      }
    }

    private async Task<string> SendHttpGetRequest(string            path,
                                                  CancellationToken ct)
    {
      HttpResponseMessage responseMsg = null;

      try
      {
        responseMsg = await _httpClient.GetAsync(path, ct);

        if (responseMsg.IsSuccessStatusCode)
        {
          return await responseMsg.Content.ReadAsStringAsync();
        }
        else
        {
          responseMsg.EnsureSuccessStatusCode();
          // Will never return because EnsureSuccessStatusCode throws exception.
          return null;
        }
      }
      catch (HttpRequestException) when (responseMsg != null && responseMsg.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        return null;
      }
      catch (HttpRequestException) when (responseMsg != null && responseMsg.StatusCode == System.Net.HttpStatusCode.Forbidden)
      {
        LogTo.Warning($"Request GET '{path}' failed: 403 Forbidden. Please check your credentials");
        return "Authentication failed. Please check your API credentials";
      }
      catch (OperationCanceledException)
      {
        return null;
      }
      finally
      {
        responseMsg?.Dispose();
      }
    }

    #endregion
  }
}
