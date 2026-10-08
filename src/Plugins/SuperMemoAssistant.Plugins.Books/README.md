# SuperMemoAssistant.Plugins.Books

The Books plugin imports EPUB books and Kindle highlights into SuperMemo as incremental-reading topics. You can then read them in small portions, in the order of their priority.

## Open the import dialog

1. In SuperMemo, display the element that must be the parent, or decide to use the collection root.
2. Press Ctrl+Alt+Shift+K ("Import a book or Kindle highlights"). You can change the hotkey in the plugin settings.
3. Click **Browse...** and select an EPUB file (`.epub`) or a Kindle `My Clippings.txt` file.
4. In the preview, clear the rows that you do not want to import.
5. Set the priority and the parent. Then click **Import**.

The progress bar shows how many elements SuperMemo created. Click **Cancel** to stop the import. The plugin then tells you how many elements it created. Errors are shown in the dialog and written to the SMA log.

## EPUB books

The plugin reads EPUB 2 and EPUB 3 books. It makes one book topic, with one child topic for each chapter, in reading order. The table of contents (`nav.xhtml` or `toc.ncx`) gives the chapter titles. If a chapter has no entry in the table of contents, the plugin uses its first heading.

The plugin skips empty documents and the navigation document. It merges short documents into the previous chapter, for example title pages and copyright pages. The merge threshold is a setting.

SuperMemo limits the number of children of one element. If a book has more chapters than this limit, the plugin puts the chapters in part topics.

The plugin removes scripts, styles, event handlers, forms, and external resources from the chapters. It keeps headings, paragraphs, emphasis, lists, tables, block quotes, and links. Images are embedded in the element as base64 data. If an image is too large (more than 1 MB) or has an unknown format, a placeholder shows its alt text.

## Kindle highlights

The plugin imports highlights and notes from `My Clippings.txt`. It skips bookmarks. It makes one topic for each book, with one child topic for each highlight. A note goes into the highlight that it annotates. If no highlight has the same location, the note gets its own topic.

The plugin keeps a hash of each imported highlight in the collection. When you import the same file again, the preview shows only the new highlights.

## Priorities

The book topic gets the priority from the dialog. In SuperMemo, a larger percentage is a lower priority. Each further chapter or highlight gets a slightly larger percentage, so that SuperMemo shows the earlier chapters first. All values stay between 0 and 100.

## Settings

| Setting | Default | Meaning |
| --- | --- | --- |
| Default priority (%) | 30 | The priority that the dialog shows first. |
| Chapter priority step (%) | 0.2 | The increase of the percentage for each further chapter or highlight. |
| Merge chapters shorter than (characters) | 1500 | Shorter EPUB documents are merged into the previous chapter. Set 0 to keep all documents. |

## References

Each topic gets SuperMemo references. These are the title of the book, the author, the source, and the date. For an EPUB, the source is the file name and the ISBN, if the book has one. For a Kindle highlight, the source is the page and the location, and the date is the date when you added the highlight.
