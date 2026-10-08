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




namespace SuperMemoAssistant.Sys.Converters.Json
{
  using System;
  using Interop.SuperMemo.Content;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using Services;

  /// <summary>Converts an <see cref="IComponentGroup" /> to its integer id representation, and vice-versa.</summary>
  /// <example>
  ///   <code>
  /// [JsonConverter(typeof(IComponentGroupToComponentGroupIdJsonConverter))]
  /// public IComponentGroup ComponentGroup { get; set; }
  /// </code>
  /// </example>
  // ReSharper disable once InconsistentNaming
  public class IComponentGroupToComponentGroupIdJsonConverter : JsonConverter<IComponentGroup>
  {
    #region Methods Impl

    /// <inheritdoc />
    public override IComponentGroup ReadJson(JsonReader      reader,
                                             Type            objectType,
                                             IComponentGroup existingValue,
                                             bool            hasExistingValue,
                                             JsonSerializer  serializer)
    {
      JToken token = JToken.Load(reader);

      return token.Type == JTokenType.Integer
        ? Svc.SM.Registry.Component[token.ToObject<int>()]
        : null;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, IComponentGroup value, JsonSerializer serializer)
    {
      var token = JToken.FromObject(value.Offset);

      writer.WriteToken(token.CreateReader());
    }

    #endregion
  }
}
