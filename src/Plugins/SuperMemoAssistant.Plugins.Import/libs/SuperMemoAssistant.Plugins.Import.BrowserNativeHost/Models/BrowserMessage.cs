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
// Created On:   2020/01/24 10:10
// Modified On:  2020/01/24 14:24
// Modified By:  Alexis

#endregion




using Newtonsoft.Json;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.Import.Models.NativeMessaging;
using SuperMemoAssistant.Sys.Converters.Json;

// ReSharper disable ClassNeverInstantiated.Global

namespace SuperMemoAssistant.Plugins.Import.Models
{
  internal class BrowserMessage
  {
    #region Properties & Fields - Public

    public MessageType Type { get; set; }

    [JsonConverter(typeof(JsonConverterObjectToString))]
    public string Data { get; set; }

    #endregion




    #region Methods

    public T GetData<T>()
    {
      return Data.Deserialize<T>();
    }
    
    public bool GetData<T>(out T data, out JsonException jsonEx)
    {
      data = default;
      jsonEx = null;

      if (Data == null)
      {
        jsonEx = new JsonException("Data is null");
        return false;
      }

      try
      {
        data = Data.Deserialize<T>();

        return true;
      }
      catch (JsonException ex)
      {
        jsonEx = ex;
        return false;
      }
    }

    #endregion
  }
}
