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
// Created On:   2020/03/29 00:21
// Modified On:  2020/04/07 06:52
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo.Elements.Builders
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using Extensions;

  /// <summary>Represents an element's references</summary>
  [Serializable]
  public class References
  {
    #region Properties & Fields - Public

    /// <summary>
    /// The original author for the given content
    /// </summary>
    public string                    Author  { get; set; }

    /// <summary>
    /// The title for the given content
    /// </summary>
    public string                    Title   { get; set; }

    /// <summary>
    /// The dates relevant to the original content (e.g. creation date, updated date)
    /// </summary>
    public List<(string, DateTime?)> Dates   { get; } = new List<(string, DateTime?)>();

    /// <summary>
    /// The original source for the given content
    /// </summary>
    public string                    Source  { get; set; }

    /// <summary>
    /// The original uri for the given content
    /// </summary>
    public string                    Link    { get; set; }

    /// <summary>
    /// The email from which the given content was extracted
    /// </summary>
    public string                    Email   { get; set; }

    /// <summary>
    /// Notes about the given content
    /// </summary>
    public string                    Comment { get; set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc/>
    public override string ToString()
    {
      List<string> refParts = new List<string>();

      if (string.IsNullOrWhiteSpace(Title) == false)
        refParts.Add($"#Title: {Title.HtmlEncode()}");

      if (string.IsNullOrWhiteSpace(Author) == false)
        refParts.Add(
          string.IsNullOrWhiteSpace(Email)
            ? $"#Author: {Author.HtmlEncode()}"
            : $"#Author: {Author.HtmlEncode()} [mailto:{Email.HtmlEncode()}]"
        );

      if (Dates != null && Dates.Any())
      {
        var dateStrs = Dates.Select(d => string.Format(CultureInfo.InvariantCulture,
                                                       d.Item1,
                                                       d.Item2?.ToString("MMM dd, yyyy, hh:mm:ss", CultureInfo.InvariantCulture)));
        var dateStr = string.Join(" ; ",
                                  dateStrs);

        refParts.Add($"#Date: {dateStr.HtmlEncode()}");
      }

      if (string.IsNullOrWhiteSpace(Source) == false)
        refParts.Add($"#Source: {Source.HtmlEncode()}");

      if (string.IsNullOrWhiteSpace(Link) == false)
        refParts.Add($"#Link: <a href=\"{Link.UrlEncode()}\">{Link.HtmlEncode()}</a>");

      if (string.IsNullOrWhiteSpace(Email) == false)
        refParts.Add($"#E-mail: {Email.HtmlEncode()}");

      if (string.IsNullOrWhiteSpace(Comment) == false)
        refParts.Add($"#Comment: {Comment.HtmlEncode()}");

      if (refParts.Any() == false)
        return string.Empty;

      return string.Format(CultureInfo.InvariantCulture,
                           SMConst.Elements.ReferenceFormat,
                           string.Join("<br>", refParts));
    }

    #endregion




    #region Methods

    /// <summary>Adds an author metadata in the references</summary>
    /// <param name="author"></param>
    /// <returns></returns>
    public References WithAuthor(string author)
    {
      Author = author;
      return this;
    }

    /// <summary>Sets a title metadata in the references</summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public References WithTitle(string title)
    {
      Title = title;
      return this;
    }

    /// <summary>Sets a date metadata in the reference</summary>
    /// <param name="date"></param>
    /// <param name="fmt"></param>
    /// <returns></returns>
    public References WithDate(DateTime date,
                               string   fmt = "{0}")
    {
      Dates.Clear();

      return AddDate(date,
                     fmt);
    }

    /// <summary>Sets a date metadata in the reference</summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public References WithDate(string date)
    {
      Dates.Clear();

      return AddDate(date);
    }

    /// <summary>Adds a date metadata to the reference</summary>
    /// <param name="date"></param>
    /// <param name="fmt"></param>
    /// <returns></returns>
    public References AddDate(DateTime date,
                              string   fmt = "{0}")
    {
      Dates.Add((fmt, date));

      return this;
    }

    /// <summary>Adds a date metadata to the reference</summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public References AddDate(string date)
    {
      if (date != null)
        Dates.Add((date, null));

      return this;
    }

    /// <summary>Sets a source metadata in the references</summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public References WithSource(string source)
    {
      Source = source;
      return this;
    }

    /// <summary>Sets a link metadata in the references</summary>
    /// <param name="link"></param>
    /// <returns></returns>
    public References WithLink(string link)
    {
      Link = link;
      return this;
    }

    /// <summary>Sets an email metadata in the references</summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public References WithEmail(string email)
    {
      Email = email;
      return this;
    }

    /// <summary>Sets a comment metadata in the references</summary>
    /// <param name="comment"></param>
    /// <returns></returns>
    public References WithComment(string comment)
    {
      Comment = comment;
      return this;
    }

    #endregion
  }
}
