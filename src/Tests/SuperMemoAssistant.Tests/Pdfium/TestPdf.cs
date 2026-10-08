namespace SuperMemoAssistant.Tests.Pdfium;

using System.Text;

/// <summary>
///   Builds a small two-page PDF in memory. Page 1 has text, a URL, a link to page 2, a 2x2 image and a text field. The
///   outline has "Chapter 1" (page 1) with the child "Section 1.1" (page 2). The named destination "intro" is page 2.
/// </summary>
internal static class TestPdf
{
  public const string Title = "Test document";
  public const string Author = "SMA tests";
  public const string Sentence = "Hello PDFium world";
  public const string Url = "https://example.com/page";

  // Page-space boxes that the content stream below draws into.
  public const float TextX = 72, TextY = 700;
  public static readonly (float Left, float Bottom, float Right, float Top) LinkBox = (70, 690, 250, 720);
  public static readonly (float Left, float Bottom, float Right, float Top) ImageBox = (300, 500, 400, 600);
  public static readonly (float Left, float Bottom, float Right, float Top) FieldBox = (72, 400, 272, 430);

  public static byte[] Build()
  {
    var content = $"BT /F1 24 Tf {TextX} {TextY} Td ({Sentence}) Tj ET\n" +
                  $"BT /F1 12 Tf 72 650 Td ({Url}) Tj ET\n" +
                  $"q 100 0 0 100 {ImageBox.Left} {ImageBox.Bottom} cm /Im1 Do Q\n";

    string[] objects =
    [
      "<< /Type /Catalog /Pages 2 0 R /Outlines 9 0 R /Names << /Dests 12 0 R >> /AcroForm << /Fields [14 0 R] >> >>",
      "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
      "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 5 0 R " +
      "/Resources << /Font << /F1 6 0 R >> /XObject << /Im1 7 0 R >> >> /Annots [8 0 R 14 0 R] >>",
      "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 13 0 R /Resources << /Font << /F1 6 0 R >> >> >>",
      Stream(content),
      "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
      "<< /Type /XObject /Subtype /Image /Width 2 /Height 2 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Length 12 >>\n" +
      "stream\n\u00FF\0\0\0\u00FF\0\0\0\u00FF\u00FF\u00FF\0\nendstream",
      $"<< /Type /Annot /Subtype /Link /Rect [{LinkBox.Left} {LinkBox.Bottom} {LinkBox.Right} {LinkBox.Top}] " +
      "/Border [0 0 0] /Dest [4 0 R /Fit] >>",
      "<< /Type /Outlines /First 10 0 R /Last 10 0 R /Count 2 >>",
      "<< /Title (Chapter 1) /Parent 9 0 R /First 11 0 R /Last 11 0 R /Count 1 /Dest [3 0 R /XYZ 72 720 2] >>",
      "<< /Title (Section 1.1) /Parent 10 0 R /Dest [4 0 R /Fit] >>",
      "<< /Names [(intro) [4 0 R /FitH 500]] >>",
      Stream("BT /F1 12 Tf 72 700 Td (Second page) Tj ET\n"),
      $"<< /Type /Annot /Subtype /Widget /FT /Tx /T (name) /V (value) /DA (/Helv 12 Tf 0 g) /P 3 0 R " +
      $"/Rect [{FieldBox.Left} {FieldBox.Bottom} {FieldBox.Right} {FieldBox.Top}] /F 4 >>",
      $"<< /Title ({Title}) /Author ({Author}) /CreationDate (D:20260101120000Z) >>",
    ];

    return Serialize(objects, infoObject: objects.Length);
  }

  private static string Stream(string data) => $"<< /Length {data.Length} >>\nstream\n{data}endstream";

  private static byte[] Serialize(string[] objects, int infoObject)
  {
    // Latin-1 keeps every char as one byte, so string lengths are byte offsets.
    var latin1 = Encoding.Latin1;
    var pdf = new StringBuilder("%PDF-1.7\n%\u00E2\u00E3\u00CF\u00D3\n");
    var offsets = new int[objects.Length];

    for (var i = 0; i < objects.Length; i++)
    {
      offsets[i] = pdf.Length;
      pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
    }

    var xref = pdf.Length;
    pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
    foreach (var offset in offsets)
      pdf.Append($"{offset:D10} 00000 n \n");

    pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R /Info {infoObject} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    return latin1.GetBytes(pdf.ToString());
  }
}
