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




// ReSharper disable CollectionNeverQueried.Global

namespace SuperMemoAssistant.Interop.SuperMemo.Elements.Builders
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using Content.Contents;
  using Models;
  using Registry.Members;
  using Registry.Models;
  using SMA;
  using Types;

  /// <summary>Defines a new element: its properties, its location in the knowledge tree, how to create it, ...</summary>
  /// <seealso cref="IElementRegistry.Add(out System.Collections.Generic.List{SuperMemoAssistant.Interop.SuperMemo.Elements.Models.ElemCreationResult}, ElemCreationFlags, ElementBuilder[])" />
  [Serializable]
  public class ElementBuilder
  {
    #region Properties & Fields - Non-Public

    private int? _parentId;

    #endregion




    #region Constructors

    /// <summary>Defines a new element</summary>
    /// <param name="type">What type of element</param>
    /// <param name="layoutName">The <see cref="ISuperMemoAssistant.Layouts" /> to use when arranging components</param>
    /// <param name="contents">The content (components) to add in this element</param>
    public ElementBuilder(ElementType          type,
                          string               layoutName = null,
                          params ContentBase[] contents)
    {
      Type = type;

      Layout = layoutName;

      if (contents != null)
        Contents.AddRange(contents);

      ContentType = Contents.Aggregate(
        ContentTypeFlags.None,
        (typeAcc, content) => typeAcc | content.ContentType
      );

      Status        = ElementStatus.Memorized;
      ShouldDisplay = true;
      Title         = null;
    }

    /// <summary>Defines a new element. Uses the default layout.</summary>
    /// <param name="type">What type of element</param>
    /// <param name="contents">The content (components) to add in this element</param>
    public ElementBuilder(ElementType          type,
                          params ContentBase[] contents)
      : this(type,
             null,
             contents) { }

    /// <summary>Defines a new element. Uses the default layout.</summary>
    /// <param name="type">What type of element</param>
    /// <param name="content">The text content</param>
    /// <param name="html">Whether the text content is html. Used for escaping content</param>
    public ElementBuilder(ElementType type,
                          string      content,
                          bool        html = true)
      : this(type,
             new TextContent(html,
                             content)) { }

    /// <summary>Defines a new element. Uses the default layout.</summary>
    /// <param name="type">What type of element</param>
    /// <param name="content">The text content</param>
    /// <param name="html">Whether the text content is html. Used for escaping content</param>
    /// <param name="encoding">The text encoding (e.g. UTF-8)</param>
    public ElementBuilder(ElementType type,
                          string      content,
                          bool        html,
                          Encoding    encoding)
      : this(type,
             new TextContent(html,
                             content,
                             encoding)) { }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The kind of element (e.g. article, item, task, ...)</summary>
    public ElementType Type { get; }

    /// <summary>The content definitions (e.g. text, images, ...)</summary>
    public List<ContentBase> Contents { get; } = new List<ContentBase>();

    /// <summary>
    ///   Aggregates all the defined content types to a single property using flags (i.e. a flag will be set for every type of
    ///   content added to this element)
    /// </summary>
    public ContentTypeFlags ContentType { get; }

    /// <summary>The <see cref="ISuperMemoAssistant.Layouts" /> to use when arranging components</summary>
    public string Layout { get; private set; }

    /// <summary>The element's title</summary>
    public string Title { get; private set; }

    /// <summary>The element's references (e.g. title, source, ...)</summary>
    public References Reference { get; private set; }

    /// <summary>
    ///   Whether to display the element after creating it. If set to <see langword="false" />, the currently displayed element
    ///   will still be displayed
    /// </summary>
    public bool ShouldDisplay { get; private set; }

    /// <summary>Sets the element's priority (for the algorithm to determine the frequency of review)</summary>
    public double Priority { get; private set; }

    /// <summary>Determines the element's parent element -- a.k.a which branch should this element belong to</summary>
    public int? ParentId
    {
      get => _parentId;
      private set => _parentId = value.HasValue ? Math.Max(1, value.Value) : null;
    }

    /// <summary>Determines the element's parent element -- a.k.a which branch should this element belong to</summary>
    public IElement Parent
    {
      set => ParentId = value?.Id;
    }

    /// <summary>Defines the element's concept</summary>
    public int? ConceptId { get; private set; }

    /// <summary>Defines the element's concept</summary>
    public IConcept Concept
    {
      set => ConceptId = value?.Id;
    }

    /// <summary>Defines the element's template</summary>
    public int? TemplateId { get; private set; }

    /// <summary>Defines the element's template</summary>
    public ITemplate Template
    {
      set => TemplateId = value?.Id;
    }

    /// <summary>
    /// The method used for apply <see cref="Template"/>
    /// </summary>
    public TemplateUseMode TemplateApplyMode { get; set; } = TemplateUseMode.Apply;

    /// <summary>Defines in which queue (learning, pending, ...) should the element be inserted</summary>
    public ElementStatus Status { get; private set; }

    /// <summary>Forces SMA to automatically generate a title (i.e. will override the provided reference's title)</summary>
    public bool ForceGenerateTitle { get; private set; }

    /// <summary>Which concepts are associated this element</summary>
    public List<int> LinkedConcepts { get; } = new List<int>();

    #endregion




    #region Methods

    /// <summary>Defines the element reference's title</summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public ElementBuilder WithTitle(string title)
    {
      Title = title;
      return this;
    }

    /// <summary>Defines which layout to use when arrange components</summary>
    /// <see cref="ISuperMemoAssistant.Layouts" />
    /// <param name="layoutName"></param>
    /// <returns></returns>
    public ElementBuilder WithLayout(string layoutName)
    {
      Layout = layoutName;
      return this;
    }

    /// <summary>Defines the element references</summary>
    /// <param name="refBuilder"></param>
    /// <returns></returns>
    public ElementBuilder WithReference(Func<References, References> refBuilder)
    {
      Reference = refBuilder(new References());
      return this;
    }

    /// <summary>
    ///   Whether to display the element after creating it. If set to <see langword="false" />, the currently displayed element
    ///   will still be displayed
    /// </summary>
    /// <returns></returns>
    public ElementBuilder Display()
    {
      ShouldDisplay = true;
      return this;
    }

    /// <summary>
    ///   Do not display this element after creating it, the currently displayed element will still be displayed
    /// </summary>
    /// <returns></returns>
    public ElementBuilder DoNotDisplay()
    {
      ShouldDisplay = false;
      return this;
    }

    /// <summary>Sets the element's priority (for the algorithm to determine the frequency of review)</summary>
    /// <param name="priority"></param>
    /// <returns></returns>
    public ElementBuilder WithPriority(double priority)
    {
      Priority = priority;
      return this;
    }

    /// <summary>Determines the element's parent element -- a.k.a which branch should this element belong to</summary>
    /// <param name="parentId"></param>
    /// <returns></returns>
    public ElementBuilder WithParent(int parentId)
    {
      ParentId = parentId;
      return this;
    }

    /// <summary>Determines the element's parent element -- a.k.a which branch should this element belong to</summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public ElementBuilder WithParent(IElement parent)
    {
      Parent = parent;
      return this;
    }

    /// <summary>Defines the element's concept</summary>
    /// <param name="concept"></param>
    /// <returns></returns>
    public ElementBuilder WithConcept(IConcept concept)
    {
      Concept = concept;
      return this;
    }

    /// <summary>Defines the element's template</summary>
    /// <param name="template"></param>
    /// <returns></returns>
    public ElementBuilder WithTemplate(ITemplate template)
    {
      Template = template;
      return this;
    }

    /// <summary>Defines how to apply <see cref="Template"/></summary>
    /// <param name="applyMode"></param>
    /// <returns></returns>
    public ElementBuilder WithTemplateApplyMode(TemplateUseMode applyMode)
    {
      TemplateApplyMode = applyMode;
      return this;
    }

    /// <summary>Defines in which queue (learning, pending, ...) should the element be inserted</summary>
    /// <param name="status"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">The element cannot be <see cref="ElementStatus.Deleted" /></exception>
    public ElementBuilder WithStatus(ElementStatus status)
    {
      if (status == ElementStatus.Deleted)
        throw new ArgumentException("New element can't be deleted");

      Status = status;
      return this;
    }

    /// <summary>Forces SMA to automatically generate a title (i.e. will override the provided reference's title)</summary>
    /// <returns></returns>
    public ElementBuilder WithForcedGeneratedTitle()
    {
      ForceGenerateTitle = true;
      return this;
    }

    /// <summary>Associates this element with the given concepts</summary>
    /// <param name="concepts"></param>
    /// <returns></returns>
    public ElementBuilder AddLinkedConcepts(IEnumerable<IConcept> concepts)
    {
      LinkedConcepts.AddRange(concepts.Select(c => c.Id));
      return this;
    }

    /// <summary>Associates this element with the given concepts</summary>
    /// <param name="conceptIds"></param>
    /// <returns></returns>
    public ElementBuilder AddLinkedConcepts(IEnumerable<int> conceptIds)
    {
      LinkedConcepts.AddRange(conceptIds);
      return this;
    }

    /// <summary>Associates this element with a concept</summary>
    /// <param name="concept"></param>
    /// <returns></returns>
    public ElementBuilder AddLinkedConcept(IConcept concept)
    {
      LinkedConcepts.Add(concept.Id);
      return this;
    }

    /// <summary>Associates this element with a concept</summary>
    /// <param name="conceptId"></param>
    /// <returns></returns>
    public ElementBuilder AddLinkedConcept(int conceptId)
    {
      LinkedConcepts.Add(conceptId);
      return this;
    }

    #endregion
  }
}
