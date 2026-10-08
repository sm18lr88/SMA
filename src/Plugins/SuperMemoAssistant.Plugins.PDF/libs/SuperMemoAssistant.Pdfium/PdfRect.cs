namespace SuperMemoAssistant.Pdfium;

using System;

/// <summary>A rectangle in PDF page space, where y grows upward: <see cref="Top" /> is at least <see cref="Bottom" />.</summary>
public readonly record struct PdfRect(float Left, float Top, float Right, float Bottom)
{
  public float Width => Right - Left;

  public float Height => Top - Bottom;

  public bool Contains(float x, float y) => Left <= x && x <= Right && Bottom <= y && y <= Top;

  /// <summary>Returns this rectangle grown outward by the margin that <paramref name="margins" /> gives for each side.</summary>
  public PdfRect Inflate(PdfRect margins) => new(Left - margins.Left,
                                                 Top + margins.Top,
                                                 Right + margins.Right,
                                                 Bottom - margins.Bottom);

  /// <summary>Returns the smallest rectangle that contains both rectangles.</summary>
  public PdfRect Union(PdfRect other) => new(Math.Min(Left, other.Left),
                                             Math.Max(Top, other.Top),
                                             Math.Max(Right, other.Right),
                                             Math.Min(Bottom, other.Bottom));
}
