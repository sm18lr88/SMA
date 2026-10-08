namespace SuperMemoAssistant.Plugins.LocalApi.Server;

using System;

/// <summary>The JSON body of POST /elements.</summary>
public sealed class CreateElementBody
{
  public string?         Type       { get; set; }
  public string?         Title      { get; set; }
  public string?         Html       { get; set; }
  public string?         Question   { get; set; }
  public string?         Answer     { get; set; }
  public int?            ParentId   { get; set; }
  public double?         Priority   { get; set; }
  public ReferencesBody? References { get; set; }
}

/// <summary>The references in the body of POST /elements.</summary>
public sealed class ReferencesBody
{
  public string? Title   { get; set; }
  public string? Author  { get; set; }
  public string? Link    { get; set; }
  public string? Source  { get; set; }
  public string? Date    { get; set; }
  public string? Comment { get; set; }
  public string? Email   { get; set; }
}

/// <summary>The JSON body of POST /navigate.</summary>
public sealed class NavigateBody
{
  public int? Id { get; set; }
}

/// <summary>Validates the body of POST /elements and makes the element to create.</summary>
public static class ElementRequestValidator
{
  /// <summary>Validates <paramref name="body" />. The parent is left as 0 when the body does not give one.</summary>
  /// <returns>The element with sanitized HTML, and the parent that the body asks for.</returns>
  /// <exception cref="ApiException">400 when the body is not valid.</exception>
  public static (NewElement Element, int? ParentId) Validate(CreateElementBody body, double defaultPriority)
  {
    var kind = body.Type?.Trim().ToLowerInvariant() switch
    {
      "topic" => ElementKind.Topic,
      "item"  => ElementKind.Item,
      _       => throw Invalid("\"type\" must be \"topic\" or \"item\"."),
    };

    if (kind == ElementKind.Topic)
    {
      if (string.IsNullOrWhiteSpace(body.Html))
        throw Invalid("A topic needs \"html\".");
      if (body.Question != null || body.Answer != null)
        throw Invalid("A topic uses \"html\". \"question\" and \"answer\" are for items.");
    }
    else
    {
      if (string.IsNullOrWhiteSpace(body.Question) || string.IsNullOrWhiteSpace(body.Answer))
        throw Invalid("An item needs \"question\" and \"answer\".");
      if (body.Html != null)
        throw Invalid("An item uses \"question\" and \"answer\". \"html\" is for topics.");
    }

    var priority = body.Priority ?? defaultPriority;
    if (double.IsFinite(priority) == false || priority < 0 || priority > 100)
      throw Invalid("\"priority\" must be a number from 0 to 100.");

    if (body.ParentId is <= 0)
      throw Invalid("\"parentId\" must be a positive element number.");

    var element = new NewElement(
      kind,
      string.IsNullOrWhiteSpace(body.Title) ? null : body.Title.Trim(),
      SanitizeOrNull(body.Html),
      SanitizeOrNull(body.Question),
      SanitizeOrNull(body.Answer),
      0,
      priority,
      ToReferences(body.References));

    return (element, body.ParentId);
  }

  /// <summary>Validates the body of POST /navigate and returns the element number.</summary>
  public static int ValidateNavigate(NavigateBody body) =>
    body.Id is > 0 ? body.Id.Value : throw Invalid("\"id\" must be a positive element number.");

  private static ElementReferences? ToReferences(ReferencesBody? refs)
  {
    if (refs == null)
      return null;

    if (refs.Link != null && IsWebLink(refs.Link) == false)
      throw Invalid("\"references.link\" must be an absolute http or https URL.");

    return new ElementReferences(refs.Title, refs.Author, refs.Link, refs.Source, refs.Date, refs.Comment, refs.Email);
  }

  private static bool IsWebLink(string link) =>
    Uri.TryCreate(link, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

  private static string? SanitizeOrNull(string? html) => html == null ? null : HtmlSanitizer.Sanitize(html);

  private static ApiException Invalid(string message) => new(400, message);
}
