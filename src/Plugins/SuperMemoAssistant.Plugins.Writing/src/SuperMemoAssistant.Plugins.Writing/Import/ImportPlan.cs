using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SuperMemoAssistant.Plugins.Writing.Import
{
  /// <summary>A topic to create. A part is an added folder topic that groups children over the SuperMemo limit.</summary>
  public sealed class ImportTopic(string title, string html, double priority, bool isPart = false)
  {
    public string Title    { get; } = title;
    public string Html     { get; } = html;
    public double Priority { get; set; } = priority;
    public bool   IsPart   { get; } = isPart;

    public List<ImportTopic> Children { get; } = [];

    /// <summary>Creates a part that holds <paramref name="children" /> and takes the priority of the first one.</summary>
    public static ImportTopic Part(string title, IEnumerable<ImportTopic> children)
    {
      var list = children.ToList();

      if (list.Count == 0)
        throw new ArgumentException("A part must hold at least one topic.", nameof(children));

      var part = new ImportTopic(title, string.Empty, list[0].Priority, true);
      part.Children.AddRange(list);

      return part;
    }
  }

  /// <summary>The topics to create from an outline, with their document title and priorities.</summary>
  public sealed record ImportPlan(string DocumentTitle, string SourceName, IReadOnlyList<ImportTopic> Topics, int TopicCount)
  {
    /// <summary>The title of the topic that holds the content before the first heading.</summary>
    public const string IntroductionTitle = "Introduction";

    /// <summary>
    ///   Builds the plan. The document title is the front matter title, else the only top-level heading, else the
    ///   file name. Priorities ascend in document order from <paramref name="startPriority" />.
    /// </summary>
    public static ImportPlan Create(Outline outline, string filePath, double startPriority)
    {
      ArgumentNullException.ThrowIfNull(outline);

      var sourceName = Path.GetFileName(filePath);
      var title = outline.Title
        ?? (outline.Roots.Count == 1 ? outline.Roots[0].Title : Path.GetFileNameWithoutExtension(filePath));

      var topics = outline.Roots.Select(ToTopic).ToList();

      if (string.IsNullOrWhiteSpace(outline.PreambleHtml) == false)
        topics.Insert(0, new ImportTopic(topics.Count == 0 ? title : IntroductionTitle, outline.PreambleHtml, 0));

      var ordered    = PreOrder(topics).ToList();
      var priorities = PrioritySequence.Create(startPriority, ordered.Count);

      for (var i = 0; i < ordered.Count; i++)
        ordered[i].Priority = priorities[i];

      return new ImportPlan(title, sourceName, topics, ordered.Count);
    }

    private static ImportTopic ToTopic(OutlineNode node)
    {
      var topic = new ImportTopic(node.Title, node.Html, 0);
      topic.Children.AddRange(node.Children.Select(ToTopic));

      return topic;
    }

    private static IEnumerable<ImportTopic> PreOrder(IEnumerable<ImportTopic> topics)
    {
      return topics.SelectMany(t => PreOrder(t.Children).Prepend(t));
    }
  }
}
