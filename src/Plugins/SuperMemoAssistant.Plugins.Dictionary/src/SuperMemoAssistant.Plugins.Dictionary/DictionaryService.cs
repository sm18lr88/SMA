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
// Created On:   2020/01/13 16:51
// Modified On:  2020/01/15 17:24
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using Anotar.Serilog;
using PluginManager.Interop.Sys;
using Stubble.Core;
using Stubble.Core.Builders;
using Stubble.Core.Exceptions;
using Stubble.Helpers;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Plugins.Dictionary.Interop;
using SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models;
using SuperMemoAssistant.Plugins.Dictionary.OxfordDictionaries;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.Remoting;

namespace SuperMemoAssistant.Plugins.Dictionary
{
  internal class DictionaryService : PerpetualMarshalByRefObject, IDictionaryService
  {
    #region Properties & Fields - Non-Public

    private static DictCfg                Config           => Svc<DictionaryPlugin>.Plugin.Config;
    private OxfordDictionaryClient OxfordDictClient { get; }
    private StubbleVisitorRenderer MustacheEngine   { get; }

    #endregion




    #region Constructors

    /// <inheritdoc />
    public DictionaryService()
    {
      OxfordDictClient = new OxfordDictionaryClient();
      MustacheEngine   = CreateStubbleVisitor();
    }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public bool CredentialsAvailable =>
      string.IsNullOrWhiteSpace(Config.AppKey) == false
      && string.IsNullOrWhiteSpace(Config.AppId) == false;

    /// <inheritdoc />
    public IElement RootElement => Config.RootDictElement ?? Svc.SM.Registry.Element.Root;

    /// <inheritdoc />
    public string Layout => Config.Layout;
    /// <inheritdoc />
    public double ExtractPriority => Config.ExtractPriority;
    /// <inheritdoc />
    public OxfordDictionary DefaultDictionary =>
      Config.DefaultLanguage ?? DictionaryConst.DefaultDictionary;

    #endregion




    #region Methods

    private static StubbleVisitorRenderer CreateStubbleVisitor()
    {
      var helpers = new Helpers()
        .Register<string>("F_Escape", (_, str) => str.HtmlEncode());

      return new StubbleBuilder()
             .Configure(conf => conf.AddHelpers(helpers))
             .Build();
    }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    [LogToErrorOnException]
    public RemoteTask<EntryResult> LookupEntry(RemoteCancellationToken ct,
                                               string                  word,
                                               string                  language = "en-gb")
    {
      if (DictionaryConst.AllMonolingualLanguages.Contains(language) == false)
      {
        LogTo.Warning("Invalid language requested: {Language}", language);
        // ReSharper disable once LocalizableElement
        throw new ArgumentException($"Invalid language requested: {language}", nameof(language));
      }

      OxfordDictClient.SetAuthentication(Config.AppId,
                                         Config.AppKey);

      return OxfordDictClient.LookupEntry(ct.Token(),
                                          word,
                                          language);
    }

    /// <inheritdoc />
    [LogToErrorOnException]
    public RemoteTask<LemmatronResult> LookupLemma(RemoteCancellationToken ct,
                                                   string                  word,
                                                   string                  language = "en-gb")
    {
      if (DictionaryConst.AllMonolingualLanguages.Contains(language) == false)
      {
        LogTo.Warning("Invalid language requested: {Language}", language);
        // ReSharper disable once LocalizableElement
        throw new ArgumentException($"Invalid language requested: {language}", nameof(language));
      }

      OxfordDictClient.SetAuthentication(Config.AppId,
                                         Config.AppKey);

      return OxfordDictClient.LookupLemma(ct.Token(),
                                          word,
                                          language);
    }

    [LogToErrorOnException]
    public RemoteTask<List<OxfordDictionary>> GetAvailableDictionaries(RemoteCancellationToken ct)
    {
      OxfordDictClient.SetAuthentication(Config.AppId,
                                         Config.AppKey);

      return OxfordDictClient.GetAvailableDictionaries(ct.Token());
    }

    /// <inheritdoc />
    [LogToErrorOnException]
    public RemoteTask<string> ApplyUserTemplate(EntryResult entryResult)
    {
      // Stubble.Helpers (F_Escape) only works with the visitor renderer, so templates are not compiled.
      return MustacheEngine.RenderAsync(Config.RenderTemplate, entryResult)
                           .AsTask()
                           .ConfigureRemoteTask(ex =>
                           {
                             switch (ex)
                             {
                               case StubbleException stubbleException:
                                 LogTo.Warning(stubbleException, @"Mustache rendering failed.
Template:
-------------------------------------
{RenderTemplate}

Data:
-------------------------------------
{EntryResult}", Config.RenderTemplate, entryResult.Serialize());
                                 break;

                               default:
                                 LogTo.Error(ex, "An exception was thrown while applying user template");
                                 break;
                             }
                           });

      #endregion
    }
  }
}
