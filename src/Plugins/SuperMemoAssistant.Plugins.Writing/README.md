# SuperMemo Assistant Writing plugin

This plugin supports incremental writing in SuperMemo. It compiles a branch of the knowledge tree into one Markdown or HTML document. It also imports a Markdown outline as a branch of topics.

This plugin is part of [SuperMemoAssistant 20 Community](../../../README.md), which runs on SuperMemo 20 (64-bit) and .NET 10. It is bundled with the installer.

In incremental writing, you write in small pieces over many topics. Then you must join the pieces into linear text, which is the [stated weak point](https://supermemo.guru/wiki/Advantages_of_incremental_writing) of the method. This plugin does that step for you.

## Compile the current branch (Ctrl+Alt+Shift+W)

1. In the element window, go to the root element of the branch.
2. Push Ctrl+Alt+Shift+W.
3. Select the options, and then select **Compile**.
4. Select the output file.

The plugin walks the branch in tree order, and the children keep their SuperMemo order. Each topic becomes a section. The title of the topic becomes a heading, and the heading level follows the depth. Levels deeper than 6 become bold paragraphs. The HTML content of the topic becomes the body of the section.

| Option | Default | Effect |
| --- | --- | --- |
| Output format | Markdown | Markdown is CommonMark with GitHub pipe tables. HTML is one standalone HTML5 file. |
| Include items | No | Items are questions and answers, so they are usually not part of the text. An excluded item is left out with its children. |
| Include a title page | Yes | The document starts with the root title and the root references. Without a title page, the root title is not written, and the children start at level 1. |
| Skip branches with these titles | TO-DO, TODO | A branch with a matching title is left out with all its children. Case and outer spaces are ignored. The root is never skipped. |

Use the skip list for a to-do branch next to the article branch.

The plugin converts the old Internet Explorer HTML of SuperMemo to clean HTML first. It removes FONT tags, styles, and the SuperMemo reference block of each element. A SPAN with a bold, italic, or strikethrough style becomes the matching emphasis.

The plugin copies local images to the folder "&lt;document name&gt;_files" next to the output file. The links in the document are relative. Web images keep their address. The summary tells you if an image file did not exist.

A progress window shows the progress. If you select **Cancel**, the compilation stops and no file is written. The plugin reads the tree in batches of 50 elements. It only reads elements and never changes them.

## Import a Markdown outline (Ctrl+Alt+Shift+M)

1. In the element window, go to the parent of the new topics.
2. Push Ctrl+Alt+Shift+M, and then select a Markdown file.
3. Type the priority of the first topic.

The headings define the tree. Each heading becomes a topic with the heading text as its title. The content up to the next heading becomes the HTML content of the topic.

- A heading becomes a child of the nearest previous heading with a lower level. Thus a skipped level, for example "#" and then "###", still nests.
- ATX headings ("# Title") and setext headings (text with a "===" or "---" line below it) are headings. A "#" in a code block, a list, or a quote is content.
- The content before the first heading becomes a first topic with the title "Introduction".
- The plugin adds the topics under the current element, in document order. Existing elements do not change.

The priorities ascend in document order from the priority that you type, in steps of 0.1. In SuperMemo, a larger percentage is a lower priority. If the steps would go past 100, the plugin makes them smaller.

The references of each topic are Title and Source. Title is the "title" of the YAML front matter. If there is no front matter title, Title is the only top-level heading, or else the file name. Source is the file name.

SuperMemo limits the number of children per element. If a parent would get too many children, the plugin puts them in part topics with the titles "[1] Title", "[2] Title", and so on. Part topics are dismissed, so they do not come into the learning queue.

The report tells you how many topics were created. If you select **Cancel**, the import stops before the next parent, and the topics created until then stay.

## Settings

The settings are the default output format, the item option, the title page option, the skipped titles, and the default import priority (30%). To change them, open the plugin settings in SMA.

## Libraries

- [Markdig](https://github.com/xoofx/markdig) (BSD-2-Clause) parses the Markdown outline and converts it to HTML.
- [ReverseMarkdown](https://github.com/mysticmind/reversemarkdown-net) (MIT) converts the cleaned HTML to Markdown. It handles nested lists, tables, code, and escaping.
- [AngleSharp](https://github.com/AngleSharp/AngleSharp) (MIT) parses the SuperMemo HTML with the HTML5 algorithm. Unclosed P and LI tags are closed as a browser closes them.

## License

[MIT](LICENSE)
