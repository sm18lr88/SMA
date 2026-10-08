namespace SuperMemoAssistant.Plugins.PDF.Models
{
  using Forge.Forms.Annotations;

  /// <summary>How the PDF plugin titles new extracts.</summary>
  public enum ExtractTitleSource
  {
    /// <summary>Text extracts: the beginning of the text. Image and page extracts: the bookmark and the pages.</summary>
    [EnumDisplay("Extract content (text beginning, bookmark, pages)")]
    Content,

    /// <summary>The title of the article, as in earlier versions.</summary>
    [EnumDisplay("Article title (earlier behavior)")]
    ArticleTitle,
  }
}
