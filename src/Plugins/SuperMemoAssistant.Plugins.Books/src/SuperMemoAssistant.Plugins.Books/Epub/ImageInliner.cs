namespace SuperMemoAssistant.Plugins.Books.Epub
{
  using System;
  using System.Buffers.Binary;
  using System.Globalization;
  using System.Net;

  /// <summary>
  ///   Embeds EPUB images in chapter HTML as base64 data, with the same markup as the shared
  ///   <c>HtmlUtils.HtmlInlineImage</c> helper (an img element with a data URI background). The element then has no
  ///   external resource. Images that cannot be embedded become a visible placeholder with their alt text.
  /// </summary>
  public static class ImageInliner
  {
    /// <summary>Images larger than this are replaced by a placeholder, so that elements stay small.</summary>
    public const int MaxImageBytes = 1024 * 1024;

    /// <summary>Wider images are scaled down to this width.</summary>
    public const int MaxWidth = 800;

    /// <summary>Returns the HTML for one image.</summary>
    public static string Render(EpubResource? image, string alt)
    {
      if (image == null || image.Data.Length > MaxImageBytes || TryReadSize(image, out var width, out var height) == false)
        return Placeholder(alt);

      if (width > MaxWidth)
      {
        height = Math.Max(1, (int)Math.Round(height * (double)MaxWidth / width));
        width  = MaxWidth;
      }

      var data = Convert.ToBase64String(image.Data);

      return string.Create(
        CultureInfo.InvariantCulture,
        $"<img width=\"{width}\" height=\"{height}\" alt=\"{WebUtility.HtmlEncode(alt)}\" style=\"background-image:url('data:{image.MediaType};base64,{data}'); background-repeat:no-repeat; background-size:100% 100%\" />");
    }

    /// <summary>A visible text placeholder that keeps the alt text of an image.</summary>
    public static string Placeholder(string alt) =>
      string.IsNullOrWhiteSpace(alt)
        ? "<span class=\"sma-books-image\">[Image]</span>"
        : $"<span class=\"sma-books-image\">[Image: {WebUtility.HtmlEncode(alt.Trim())}]</span>";

    /// <summary>Reads the pixel size of a PNG, GIF, JPEG, or BMP image from its header.</summary>
    public static bool TryReadSize(EpubResource image, out int width, out int height)
    {
      var d = image.Data.AsSpan();
      width = height = 0;

      switch (image.MediaType)
      {
        case "image/png" when d.Length >= 24 && d[1] == 'P' && d[2] == 'N' && d[3] == 'G':
          width  = BinaryPrimitives.ReadInt32BigEndian(d[16..]);
          height = BinaryPrimitives.ReadInt32BigEndian(d[20..]);
          break;

        case "image/gif" when d.Length >= 10 && d[0] == 'G' && d[1] == 'I' && d[2] == 'F':
          width  = BinaryPrimitives.ReadUInt16LittleEndian(d[6..]);
          height = BinaryPrimitives.ReadUInt16LittleEndian(d[8..]);
          break;

        case "image/bmp" when d.Length >= 26 && d[0] == 'B' && d[1] == 'M':
          width  = BinaryPrimitives.ReadInt32LittleEndian(d[18..]);
          height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(d[22..]));
          break;

        case "image/jpeg":
        case "image/jpg":
          (width, height) = ReadJpegSize(d);
          break;
      }

      return width > 0 && height > 0;
    }

    private static (int, int) ReadJpegSize(ReadOnlySpan<byte> d)
    {
      if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8)
        return (0, 0);

      var i = 2;
      while (i + 9 < d.Length)
      {
        if (d[i] != 0xFF)
          return (0, 0);

        var marker = d[i + 1];
        var length = BinaryPrimitives.ReadUInt16BigEndian(d[(i + 2)..]);
        var isFrameHeader = marker is >= 0xC0 and <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC);

        if (isFrameHeader)
          return (BinaryPrimitives.ReadUInt16BigEndian(d[(i + 7)..]), BinaryPrimitives.ReadUInt16BigEndian(d[(i + 5)..]));

        if (length < 2)
          return (0, 0);

        i += 2 + length;
      }

      return (0, 0);
    }
  }
}
