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




namespace SuperMemoAssistant.Plugins.PDF.PDF
{
  using System;
  using System.Text;
  using System.Collections.ObjectModel;
  using System.Collections.Specialized;
  using System.ComponentModel;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using PluginManager.Remoting;
  using System.Text.RegularExpressions;
  using System.Windows;
  using Anotar.Serilog;
  using Forge.Forms.Annotations;
  using Interop.SuperMemo.Content.Controls;
  using Interop.SuperMemo.Core;
  using Interop.SuperMemo.Elements.Builders;
  using Interop.SuperMemo.Elements.Models;
  using Interop.SuperMemo.Elements.Types;
  using Interop.SuperMemo.Registry.Members;
  using Microsoft.Toolkit.Uwp.Notifications;
  using Models;
  using Newtonsoft.Json;
  using SuperMemoAssistant.Pdfium;
  using SuperMemoAssistant.Pdfium.Wpf;
  using PropertyChanged;
  using Services;
  using Services.ToastNotifications;
  using SuperMemoAssistant.Extensions;

  [Form(Mode = DefaultFields.None)]
  public class PDFElement : INotifyPropertyChanged
  {
    #region Constructors

    public PDFElement()
    {
      BinaryMemberId   = -1;
      StartPage        = -1;
      EndPage          = -1;
      StartIndex       = -1;
      EndIndex         = -1;
      ReadPage         = 0;
      ReadPoint        = default;
      PDFExtracts      = new ObservableCollection<PDFTextExtract>();
      SMExtracts       = new ObservableCollection<PDFTextExtract>();
      SMImgExtracts    = new ObservableCollection<PDFImageExtract>();
      IgnoreHighlights = new ObservableCollection<PDFTextExtract>();

      PDFExtracts.CollectionChanged      += OnCollectionChanged;
      SMExtracts.CollectionChanged       += OnCollectionChanged;
      SMImgExtracts.CollectionChanged    += OnCollectionChanged;
      IgnoreHighlights.CollectionChanged += OnCollectionChanged;
    }

    #endregion




    #region Properties & Fields - Public

    [JsonProperty(PropertyName = "BM")]
    public int BinaryMemberId { get; set; }

    [JsonProperty(PropertyName = "SP")]
    public int StartPage { get; set; }
    [JsonProperty(PropertyName = "EP")]
    public int EndPage { get; set; }
    [JsonProperty(PropertyName = "SI")]
    public int StartIndex { get; set; }
    [JsonProperty(PropertyName = "EI")]
    public int EndIndex { get; set; }

    [JsonProperty(PropertyName = "PDFE")]
    public ObservableCollection<PDFTextExtract> PDFExtracts { get; }
    [JsonProperty(PropertyName = "SME")]
    public ObservableCollection<PDFTextExtract> SMExtracts { get; }
    [JsonProperty(PropertyName = "SMIE")]
    public ObservableCollection<PDFImageExtract> SMImgExtracts { get; }
    [JsonProperty(PropertyName = "IH")]
    public ObservableCollection<PDFTextExtract> IgnoreHighlights { get; }

    [JsonProperty(PropertyName = "RPg")]
    public int ReadPage { get; set; }
    [JsonProperty(PropertyName = "RPt")]
    public Point ReadPoint { get; set; }

    [Field(Name = "Extract format")]
    [SelectFrom(typeof(ExtractFormat),
                SelectionType = SelectionType.ComboBox)]
    [JsonProperty(PropertyName = "EF")]
    public ExtractFormat ExtractFormat { get; set; } = ExtractFormat.HtmlRichText;

    [Field(Name = "PDF Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double PDFExtractPriority { get; set; }
    [Field(Name = "SM Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double SMExtractPriority { get; set; }

    [JsonProperty(PropertyName = "VM")]
    public ViewModes ViewMode { get; set; }

    [Field(Name                = "Page margin")]
    [JsonProperty(PropertyName = "PM")]
    public int PageMargin { get; set; } = PDFConst.DefaultPageMargin;
    [JsonProperty(PropertyName = "PME")]
    public bool PageMarginEnabled { get; set; } = true;

    [JsonProperty(PropertyName = "Z")]
    public float Zoom { get; set; } = PDFConst.DefaultZoom;

    [JsonIgnore]
    [DoNotNotify]
    public int ElementId { get; set; }

    [JsonIgnore]
    [DoNotNotify]
    public string FilePath { get; set; }

    [JsonIgnore]
    [DoNotNotify]
    public bool IsChanged { get; set; }

    [JsonIgnore]
    [DoNotNotify]
    public bool IsFullDocument => StartPage < 0;

    [JsonIgnore]
    [DoNotNotify]
    public IBinary BinaryMember => Svc.SM.Registry.Binary?[BinaryMemberId];

    #endregion




    #region Methods

    public static CreationResult Create(
      string    filePath,
      int       startPage       = -1,
      int       endPage         = -1,
      int       startIdx        = -1,
      int       endIdx          = -1,
      int       parentElementId = -1,
      int       readPage        = 0,
      Point     readPoint       = default,
      ViewModes viewMode        = PDFConst.DefaultViewMode,
      int       pageMargin      = PDFConst.DefaultPageMargin,
      float     zoom            = PDFConst.DefaultZoom,
      bool      shouldDisplay   = true)
    {
      IBinary binMem = null;

      try
      {
        var fileName = Path.GetFileName(filePath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
          LogTo.Warning($"Path.GetFileName(filePath) returned null for filePath '{filePath}'.");
          return CreationResult.FailUnknown;
        }

        LogTo.Debug("PDF import: searching binary registry for {FileName}", fileName);
        var binMems = Svc.SM.Registry.Binary.FindByName(
          new Regex(Regex.Escape(fileName) + ".*", RegexOptions.IgnoreCase)).ToList();

        if (binMems.Any())
        {
          var oriPdfFileInfo = new FileInfo(filePath);

          if (oriPdfFileInfo.Exists == false)
          {
            LogTo.Warning($"New PDF file '{filePath}' doesn't exist.");
            return CreationResult.FailUnknown;
          }

          foreach (var itBinMem in binMems)
          {
            var smPdfFilePath = itBinMem.GetFilePath("pdf");
            var smPdfFileInfo = new FileInfo(smPdfFilePath);

            if (smPdfFileInfo.Exists == false)
            {
              LogTo.Warning($"PDF file '{smPdfFilePath}' associated with Binary member id {itBinMem.Id} is missing.");
              continue;
            }

            try
            {
              if (smPdfFileInfo.Length != oriPdfFileInfo.Length)
                continue;
            }
            catch (FileNotFoundException ex)
            {
              LogTo.Warning(ex, "PDF file '{SmPdfFilePath}' or '{FilePath}' has gone missing. Weird.", smPdfFilePath, filePath);
              continue;
            }

            binMem = itBinMem;
            break;
          }
        }

        if (binMem == null)
        {
          LogTo.Debug("PDF import: adding file to binary registry");
          int binMemId = Svc.SM.Registry.Binary.Add(filePath, fileName);
          LogTo.Debug("PDF import: binary registry returned id {MemberId}", binMemId);

          if (binMemId < 0)
            return CreationResult.FailBinaryRegistryInsertionFailed;

          binMem = Svc.SM.Registry.Binary[binMemId];
        }
      }
      catch (RemotingException)
      {
        return CreationResult.FailUnknown;
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception thrown while creating new PDF element");
        return CreationResult.FailUnknown;
      }

      return Create(binMem,
                    startPage,
                    endPage,
                    startIdx,
                    endIdx,
                    parentElementId,
                    readPage,
                    readPoint,
                    viewMode,
                    pageMargin,
                    zoom,
                    shouldDisplay);
    }

    public static CreationResult Create(
      IBinary   binMem,
      int       startPage       = -1,
      int       endPage         = -1,
      int       startIdx        = -1,
      int       endIdx          = -1,
      int       parentElementId = -1,
      int       readPage        = 0,
      Point     readPoint       = default,
      ViewModes viewMode        = PDFConst.DefaultViewMode,
      int       pageMargin      = PDFConst.DefaultPageMargin,
      float     zoom            = PDFConst.DefaultZoom,
      bool      shouldDisplay   = true,
      string    subtitle        = null)
    {
      PDFElement pdfEl;
      string     title;
      string     author;
      string     creationDate;
      string     filePath;

      try
      {
        LogTo.Debug("PDF import: resolving binary member file");
        filePath = binMem.GetFilePath("pdf");

        if (File.Exists(filePath) == false)
          return CreationResult.FailBinaryMemberFileMissing;

        pdfEl = new PDFElement
        {
          BinaryMemberId = binMem.Id,
          FilePath       = filePath,
          StartPage      = startPage,
          EndPage        = endPage,
          StartIndex     = startIdx,
          EndIndex       = endIdx,
          ReadPage       = readPage,
          ReadPoint      = readPoint,
          ViewMode       = viewMode,
          PageMargin     = pageMargin,
          Zoom           = zoom,
        };

        LogTo.Debug("PDF import: reading PDF metadata");
        pdfEl.GetInfos(out string pdfTitle,
                       out author,
                       out creationDate);

        title = pdfEl.ConfigureTitle(pdfTitle, subtitle);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception thrown while creating new PDF element");
        return CreationResult.FailUnknown;
      }

      string elementHtml = string.Format(CultureInfo.InvariantCulture,
                                         PDFConst.ElementFormat,
                                         title,
                                         binMem.Name,
                                         pdfEl.GetJsonB64());

      IElement parentElement =
        parentElementId > 0
          ? Svc.SM.Registry.Element[parentElementId]
          : null;

      var elemBuilder =
        new ElementBuilder(ElementType.Topic,
                           elementHtml)
          .WithParent(parentElement)
          .WithTitle(subtitle ?? title)
          .WithPriority(PDFState.Instance.Config.PDFExtractPriority)
          .WithReference(
            r => r.WithTitle(title)
                  .WithAuthor(author)
                  .WithDate(creationDate)
                  .WithSource("PDF")
                  .WithLink("..\\" + Svc.SM.Collection.MakeRelative(filePath))
          );

      if (shouldDisplay == false)
        elemBuilder = elemBuilder.DoNotDisplay();

      LogTo.Debug("PDF import: creating SuperMemo topic");
      bool accepted = Svc.SM.Registry.Element.Add(out var results, ElemCreationFlags.CreateSubfolders, elemBuilder);
      var result = results?.SingleOrDefault();
      LogTo.Debug("PDF import: Element.Add accepted={Accepted}, code={Code}, id={ElementId}, current={CurrentElementId}",
                  accepted, result?.Result, result?.ElementId, Svc.SM.UI.ElementWdw.CurrentElementId);
      return accepted && result?.Success == true && result.ElementId > 0
        ? CreationResult.Ok
        : CreationResult.FailCannotCreateElement;
    }

    public static PDFElement TryReadElement(string elText,
                                            int    elementId)
    {
      if (string.IsNullOrWhiteSpace(elText))
        return null;

      var reRes = PDFConst.RE_Element.Match(elText);

      if (reRes.Success == false)
        return null;

      try
      {
        string toDeserialize = reRes.Groups[1].Value.FromBase64();

        var pdfEl = JsonConvert.DeserializeObject<PDFElement>(toDeserialize);

        if (pdfEl != null) // && elementId > 0)
        {
          pdfEl.ElementId = elementId;
          pdfEl.FilePath  = pdfEl.BinaryMember.GetFilePath("pdf");

          // The displayed element can change while its HTML is being read.
          if (Svc.SM.UI.ElementWdw.CurrentElementId != elementId)
            return null;

          if (File.Exists(pdfEl.FilePath) == false)
          {
            var pdfDirPath = Svc.SM.Collection.CombinePath(Path.GetDirectoryName(pdfEl.FilePath));

            $"The PDF document is missing.\r\nFilename: {Path.GetFileName(pdfEl.FilePath)}".ShowDesktopNotification(
              new ToastButton("Open containing folder", pdfDirPath)
              {
                ActivationType = ToastActivationType.Protocol
              }
            );

            return null;
          }
        }

        return pdfEl;
      }
      catch (Exception ex)
      {
        LogTo.Warning(ex, "PDF import: failed to parse the displayed PDF topic");
        return null;
      }
    }

    public SaveResult Save()
    {
      if (IsChanged == false)
        return SaveResult.Ok;

      if (ElementId <= 0)
        return SaveToBackup();

      try
      {
        bool saveToControl = Svc.SM.UI.ElementWdw.CurrentElementId == ElementId;

        if (saveToControl)
        {
          IControlHtml ctrlHtml = Svc.SM.UI.ElementWdw.ControlGroup.GetFirstHtmlControl();

          ctrlHtml.Text = UpdateHtml(ctrlHtml.Text);

          IsChanged = false;
        }

        else
        {
          return SaveToBackup();
        }


        return SaveResult.Ok;
      }
      catch (Exception)
      {
        return SaveToBackup();
      }
    }

    /// <summary>
    ///   Writes this element's reading state to the collection's <c>sma\PDF\Backups</c> folder when it cannot be saved into
    ///   SuperMemo (for example, after the user moved to another element), and tells the user where it is.
    /// </summary>
    public SaveResult SaveToBackup()
    {
      try
      {
        var backupDir = Path.Combine(Svc.SM.Collection.GetSMAFolder(), "PDF", "Backups");
        Directory.CreateDirectory(backupDir);

        var backupPath = Path.Combine(backupDir, $"element-{ElementId}-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(backupPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        IsChanged = false;

        LogTo.Warning("The state of PDF element {ElementId} could not be saved in SuperMemo. It was backed up to {BackupPath}",
                      ElementId, backupPath);
        $"The PDF reading state of element {ElementId} could not be saved in SuperMemo. A backup was written to {backupPath}"
          .ShowDesktopNotification();

        return SaveResult.FailWithBackup;
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        LogTo.Error(ex, "The state of PDF element {ElementId} could not be saved or backed up", ElementId);
        return SaveResult.Fail;
      }
    }

    public bool IsPageInBound(int pageNo)
    {
      return IsFullDocument || pageNo >= StartPage && pageNo <= EndPage;
    }

    private string UpdateHtml(string html)
    {
      string newElementDataDiv = string.Format(CultureInfo.InvariantCulture,
                                               PDFConst.ElementDataFormat,
                                               GetJsonB64());

      return PDFConst.RE_Element.Replace(html,
                                         newElementDataDiv);
    }

    private string GetJsonB64()
    {
      string elementJson = JsonConvert.SerializeObject(this,
                                                       Formatting.None);

      return elementJson.ToBase64();
    }

    public static void GetInfos(string     filePath,
                                out string title,
                                out string authors,
                                out string date)
    {
      authors = null;
      date    = null;

      using (var pdfDoc = PdfDocument.Load(filePath))
      {
        title   = PdfMetadataHelper.FixMetadataEncoding(pdfDoc.Title);
        authors = PdfMetadataHelper.FixMetadataEncoding(pdfDoc.Author);
        date    = pdfDoc.CreationDate;

        if (string.IsNullOrWhiteSpace(date) == false)
        {
          var match = Regex.Match(date, "D\\:([0-9]{14})\\+[0-9]{2}'[0-9]{2}'");

          if (match.Success)
            if (DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeUniversal, out var dateTime))
              date = dateTime.ToString(CultureInfo.InvariantCulture);
        }
      }

      if (string.IsNullOrWhiteSpace(title))
        title = null;
    }

    public void GetInfos(out string title,
                         out string authors,
                         out string date)
    {
      GetInfos(FilePath,
               out title,
               out authors,
               out date);

      title ??= BinaryMember.Name;

      if (StartPage >= 0 && EndPage >= 0)
        title += $" ({StartPage + 1}:{StartIndex} -> {EndPage + 1}:{EndIndex})";
    }

    public string ConfigureTitle(string title, string subtitle = null)
    {
      return string.IsNullOrWhiteSpace(subtitle)
        ? title
        : $"{title} - {subtitle}";
    }

    public References ConfigureSMReferences(References r,
                                            string     subtitle  = null,
                                            string     bookmarks = null)
    {
      string filePath = BinaryMember.GetFilePath("pdf");

      GetInfos(out string pdfTitle,
               out string author,
               out string creationDate);

      var title = ConfigureTitle(pdfTitle, subtitle);

      return r.WithTitle(title + (bookmarks != null ? $" -- {bookmarks}" : string.Empty))
              .WithAuthor(author)
              .WithDate(creationDate)
              .WithSource("PDF")
              .WithLink("..\\" + Svc.SM.Collection.MakeRelative(filePath));
    }

    [SuppressPropertyChangedWarnings]
    private void OnCollectionChanged(object                           sender,
                                     NotifyCollectionChangedEventArgs e)
    {
      IsChanged = true;
    }

    #endregion




    #region Events

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion




    #region Enums

    public enum CreationResult
    {
      Ok,
      FailUnknown,
      FailCannotCreateElement,
      FailBinaryRegistryInsertionFailed,
      FailBinaryMemberFileMissing
    }

    public enum SaveResult
    {
      Ok             = 0,
      FailWithBackup = 1,
      Fail           = 2,
      FailDeleted,
      FailInvalidComponent,
      FailInvalidTextMember
    }

    #endregion
  }

  internal static class PdfMetadataHelper
  {
    // 尝试修复编码问题的主方法
    internal static string FixMetadataEncoding(string input)
    {
      if (string.IsNullOrEmpty(input))
        return input;

      // 检测是否有乱码特征
      if (!HasGarbledTextCharacters(input))
        return input; // 如果没有乱码特征，直接返回原始输入

      // 尝试多种编码方式
      try
      {
        // 尝试方法1: Unicode 转 UTF-8
        byte[] bytes = Encoding.Unicode.GetBytes(input);
        string result = Encoding.UTF8.GetString(bytes);
        if (!HasGarbledTextCharacters(result))
          return result;

        // 尝试方法2: 假设数据是UTF-16BE编码(常见于PDF文档)
        if (input.Length % 2 == 0)
        {
          byte[] beBytes = new byte[input.Length];
          for (int i = 0; i < input.Length; i++)
          {
            beBytes[i] = (byte)input[i];
          }
          result = Encoding.BigEndianUnicode.GetString(beBytes);
          if (!HasGarbledTextCharacters(result))
            return result;
        }

        // 尝试方法3: 对于可能是中文内容，使用中文编码
        if (MightBeChineseContent(input))
        {
          Encoding gbk = Encoding.GetEncoding("GB18030");
          byte[] gbkBytes = Encoding.Default.GetBytes(input);
          result = gbk.GetString(gbkBytes);
          if (!HasGarbledTextCharacters(result))
            return result;
        }
      }
      catch (Exception)
      {
        // 发生异常，回退到原始字符串
      }

      return input; // 如果所有尝试都失败，返回原始输入
    }

    // 检测字符串是否包含常见的乱码特征
    private static bool HasGarbledTextCharacters(string text)
    {
      if (string.IsNullOrEmpty(text))
        return false;

      // 检查是否包含常见的乱码字符
      foreach (char c in text)
      {
        // 检查无效的Unicode字符或代理对字符
        if (c > 0xFFFD || (c >= 0xD800 && c <= 0xDFFF))
          return true;
      }

      // 对于纯ASCII文本，不需要额外处理
      bool isAsciiOnly = true;
      foreach (char c in text)
      {
        if (c >= 128)
        {
          isAsciiOnly = false;
          break;
        }
      }

      if (isAsciiOnly)
        return false;

      // 计算特殊字符比例
      int specialCharCount = 0;
      int normalCharCount = 0;

      foreach (char c in text)
      {
        if (c < 32 || (c >= 127 && c < 160 && c != 133 && c != 145 && c != 146 && c != 147 && c != 148))
          specialCharCount++;
        else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                 c == ' ' || c == '.' || c == ',' || c == '-' || c == '_' || c == ':' || c == ';' ||
                 c == '(' || c == ')' || c == '[' || c == ']' || c == '!' || c == '?')
          normalCharCount++;
      }

      // 如果特殊字符比例过高，认为是乱码
      double specialRatio = (double)specialCharCount / text.Length;
      double normalRatio = (double)normalCharCount / text.Length;

      return specialRatio > 0.2 || normalRatio < 0.5;
    }

    // 判断内容是否可能是中文
    private static bool MightBeChineseContent(string text)
    {
      if (string.IsNullOrEmpty(text))
        return false;

      // 检查是否包含中文字符或其他CJK字符
      foreach (char c in text)
      {
        // 中文汉字范围
        if (c >= 0x4E00 && c <= 0x9FFF)
          return true;

        // 中文标点符号范围
        if (c >= 0x3000 && c <= 0x303F)
          return true;

        // 中文编码下常见的乱码模式
        if ((c >= 0xA1 && c <= 0xFE) || (c >= 0x8140 && c <= 0xFEFE))
          return true;
      }

      return false;
    }
  }

}
