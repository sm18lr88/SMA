# SuperMemo Assistant PDF plugin

Reads PDF files incrementally in SuperMemo and extracts text, images, and page ranges.

This plugin is part of [SuperMemoAssistant 20 Community](../../../README.md), which runs on SuperMemo 20 (64-bit) and .NET 10. The installer includes it. It renders and reads PDF files with PDFium (see [ADR 0002](../../../docs/decisions/0002-pdf-stack.md)).

The original plugin is [supermemo/SuperMemoAssistant.Plugins.PDF](https://github.com/supermemo/SuperMemoAssistant.Plugins.PDF).


### Reading PDF in SuperMemo

##### The *PDF plugin* unlocks the powers of SuperMemo for PDF files and books:

> - Extract text, images & snapshots
> - Built-in OCR (formulas & text)
> - Built-in Dictionary
> - Text format is preserved
> - Multi-selection with <kbd>ctrl</kbd>

<br />

![Incremental PDF Plugin](https://github.com/supermemo/SuperMemoAssistant.Plugins.PDF/raw/master/assets/images/screenshots/SMA-Incremental-PDF.jpg)

### Keyboard & Mouse controls

- Core features
  - Open a PDF: Ctrl+Win+I
  - Knowledge
    - Alt+X: SuperMemo Extract (extract selection as text or image)
    - Ctrl+Alt+X: PDF Extract (extract selection as pdf pages)
    - Ctrl+L: Pass to SuperMemo (Learn)
    - Ctrl+Shift+Delete: Pass to SuperMemo (Delete)
    - Ctrl+Shift+Enter: Pass to SuperMemo (Done)
  - Selection
    - Text selection
      - Mouse selection over text: Select text
      - Double Click on word: Select word
      - Shift+Click on text: If a selection already exists, extend selection to selected character
    - Page selection
      - Double Click on page: Select page
      - Shift+Click on page: If a page selection already exists, extend page selection to selected page
    - Image selection
      - Mouse click on image: Select image
    - Area (snapshot) selection
      - Mouse selection over page: Clip area as image snapshot
  - Bookmarks
    - Enter: Go to bookmark
    - Ctrl+Alt+X: PDF Extract (extract bookmark page range as pdf pages)
    - Right-click > PDF Extract each subsection (N): one PDF extract for each direct child of the bookmark
    - Right-click > PDF Extract this level (N): one PDF extract for the bookmark and each of its siblings (for example, split a book into its chapters)
    - These two items ask for confirmation when N is more than 1. They skip sections that are already PDF extracts of the element, and then show how many extracts they created and skipped.
- Extract titles
  - A text extract gets the beginning of its text as its title (80 characters by default).
  - An image extract gets the bookmark of its first page, the number of images, and the pages.
  - A PDF extract without a bookmark title gets the bookmark of its first page (or the PDF title) and its pages, for example "Chapter 2 (p. 10-14)".
  - In the settings, "Extract titles" selects this behavior or the article title (the earlier behavior). The settings also set the maximum length of a text extract title.
- Knowledge Tree
  - Alt/Ctrl+Arrows: Pass to SuperMemo
  - Ctrl+Alt+Left/Right: Go to previous/next Sibling in KT
  - Ctrl+Alt+Up: Go to Parent in KT
  - Ctrl+Alt+Down: Go to first Child in KT
- PDF
  - Navigation
    - Up/Down: Line up/down
    - Left/Right: Previous/Next page
    - Pg.Up/Down: Previous/Next page
    - Home/End: First/Last page
    - Ctrl+G: Go to page [WIP]
  - Text Selection
    - Shift+Left/Right: Extend text selection (character by character)
    - Ctrl+Shift+Arrows: Extend text selection (word by word)
    - Shift+Pg.Up/Down: Extend text selection (page by page)
  - Misc
    - Ctrl+C: Copy selection
    - Escape: Deselect Area
    
### View Modes

![View Modes](https://github.com/supermemo/SuperMemoAssistant.Plugins.PDF/raw/master/assets/images/screenshots/PDF-ViewModes.png)

- 1: Toggle inter-page margin
- 2: Single page mode
- 3: Continuous vertical page mode
- 4: Continuous horizontal page mode
- 5: Grid mode
- 6: Book mode


## License

MIT. See [LICENSE](LICENSE).
