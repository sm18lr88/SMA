# 0002. Use PDFium directly for the PDF plugin

- Status: accepted
- Date: 2026-10-07

## Context

The PDF plugin needs these capabilities:

- Rendering, including progressive rendering that yields to the UI thread.
- Text extraction, character positions, and text search.
- Links, URLs in the page text, the document outline, and named destinations.
- Page objects (text runs with font data, images, form XObjects) for the HTML extract.
- Interactive form fields.

The plugin used a commercial SDK. Its license allowed only a time-limited evaluation, and releases could not include it. As a result, the installer could not include the PDF plugin.

PDFium, the PDF engine of Chromium, provides all of these capabilities through its C API. Its license is BSD-3-Clause. The project [bblanchon/pdfium-binaries](https://github.com/bblanchon/pdfium-binaries) publishes current Windows builds as the NuGet package `bblanchon.PDFium.Win32` (Apache-2.0).

## Decision

- `SuperMemoAssistant.Pdfium` is a small managed library over the PDFium C API. It uses `LibraryImport` bindings and has no wrapper dependency. The library is under `src/Plugins/SuperMemoAssistant.Plugins.PDF/libs`.
- The object model fits the plugin: `PdfDocument`, `PdfPage`, `PdfText`, `PdfBitmap`, `PdfBookmark`, `PdfAction`, `PdfForms`, and the page objects.
- PDFium does not have bitmap blending and rotation helpers. The library implements them in managed code.
- `SuperMemoAssistant.Pdfium.Wpf` is the WPF viewer control. Its code derives from an Apache-2.0 WPF viewer (see `NOTICE` in that folder).
- The installer includes the PDF plugin.

## Alternatives considered

- **PDFiumCore** (generated bindings). Its generated callback structures are awkward for the form engine and the progressive-render pause. The plugin needs bindings for fewer than 120 functions.
- **Docnet.Core** or **PDFtoImage**. These cover rendering and plain text. They do not expose page objects, the form engine, or progressive rendering.
- **WebView2 with PDF.js**. This would change how the plugin reads and extracts content completely.

## Consequences

- Releases include the PDF plugin with no license key and no evaluation limits.
- Updates to PDFium come from a version change of `bblanchon.PDFium.Win32` in `Directory.Packages.props`.
- PDFium is not thread-safe. The plugin must call it from one thread at a time.
- The viewer no longer has the print and file-open toolbars, which the plugin did not use.
- Unit tests in `src/Tests/SuperMemoAssistant.Tests/Pdfium` build a PDF in memory. They check the API against the real `pdfium.dll` and render the viewer off-screen.
