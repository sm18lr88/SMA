using ReverseMarkdown;

namespace SuperMemoAssistant.Plugins.Writing.Html
{
  /// <summary>Converts cleaned HTML (see <see cref="SuperMemoHtml" />) to CommonMark with GitHub pipe tables.</summary>
  public sealed class HtmlToMarkdown
  {
    private readonly Converter _converter = new(new Config
    {
      GithubFlavored = true,
      Tags           = { Unknown = Config.UnknownTagsOption.Bypass },
      Formatting     = { RemoveComments = true, OutputLineEnding = "\n" },
      Links          = { SmartHref = true },
    });

    /// <summary>Converts one HTML fragment. Returns trimmed Markdown.</summary>
    public string Convert(string html)
    {
      return string.IsNullOrWhiteSpace(html) ? string.Empty : _converter.Convert(html).Trim();
    }
  }
}
