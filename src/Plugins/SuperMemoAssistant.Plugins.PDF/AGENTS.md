# PDF plugin - reader, extractor, and Pdfium bindings

## OVERVIEW
PDF reading/annotation window with extract-to-SuperMemo, plus the repo's own Pdfium wrapper. ~18k LOC across 3 projects - largest plugin; distinct domain (native PDF rendering).

## STRUCTURE
```
PDF/
├── src/SuperMemoAssistant.Plugins.PDF/
│   ├── PDFPlugin.cs, PDFHotKeys.cs, PDFConst.cs
│   ├── PDF/         # PDFElement (758 LOC), PDFState, PDFWindow(.Bookmarks), Viewer/, ToolBars/
│   ├── Extracts/    # ExtractTitles, OutlineTree, SectionPlanner
│   ├── MathPix/     # MathPixAPI + embedded MathPix.html
│   ├── Utils/Web/HtmlBuilder.cs   # 734 LOC, PDF text -> HTML
│   └── Converters/, Extensions/, Models/
├── libs/SuperMemoAssistant.Pdfium/      # PdfDocument/PdfPage/PdfText...; Native/NativeMethods.*.cs P/Invoke
├── libs/SuperMemoAssistant.Pdfium.Wpf/  # WPF viewer control + ToolBars/ (~5k LOC)
└── assets/images/                       # icons linked into the csproj as Resources
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Text/area selection, extract | PDF/Viewer/IPDFViewer.Selection.cs | 788 LOC, partial of IPDFViewer |
| Mouse/keyboard input | PDF/Viewer/IPDFViewer.Inputs.cs | 534 LOC |
| PDF <-> SuperMemo element binding | PDF/PDFElement.cs | per-element state persisted with the element |
| Extract HTML formatting | Utils/Web/HtmlBuilder.cs | |
| New pdfium API | libs/SuperMemoAssistant.Pdfium/Native/NativeMethods.<Area>.cs, then managed wrapper | errors via PdfiumException / CallbackErrors |
| Word lookup from a selection | references Plugins.Dictionary.Interop | GetService<IDictionaryService> |

## CONVENTIONS
- Nullable OFF (legacy); UseWPF + UseWindowsForms both on.
- Entry: PDFPlugin : SMAPluginBase<PDFPlugin>.
- Large types split into partials by concern (IPDFViewer.*, PdfPage.Forms/Rendering, PdfBitmap.Compositing).
- Pdfium native calls are grouped NativeMethods.{Document,Forms,Text,View}.cs; keep marshaling structs in NativeStructs.cs.

## ANTI-PATTERNS
- Do not P/Invoke pdfium from the plugin project - go through libs/SuperMemoAssistant.Pdfium.
- Don't grow IPDFViewer.Selection.cs/PDFElement.cs further; add a partial.

## TESTS
src/Tests/SuperMemoAssistant.Tests/PDF/ and Pdfium/ (the test project references both PDF and Pdfium.Wpf).
