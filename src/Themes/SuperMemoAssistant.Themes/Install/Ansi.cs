// The Windows ANSI code page, in which SuperMemo reads and writes its ini and css files. Decoding is strict so a rewrite never corrupts a file.
using System.Runtime.InteropServices;
using System.Text;

namespace SuperMemoAssistant.Themes;

internal static partial class Ansi
{
  [LibraryImport("kernel32")]
  private static partial uint GetACP();

  private static readonly Lazy<Encoding> Strict = new(() =>
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    return Encoding.GetEncoding((int)GetACP(), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
  });

  /// <summary>The Windows ANSI code page, strict: invalid bytes and unencodable characters throw.</summary>
  public static Encoding Default => Strict.Value;

  /// <summary>A strict encoding for a given code page, for tests that must not depend on the machine.</summary>
  public static Encoding ForCodePage(int codePage)
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    return Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
  }

  public static string Decode(byte[] bytes, string what)
  {
    try
    {
      return Strict.Value.GetString(bytes);
    }
    catch (DecoderFallbackException ex)
    {
      throw new ThemeException($"{what} has bytes that are not valid in the Windows code page; it was left unchanged.", ex);
    }
  }

  public static byte[] Encode(string text, string what)
  {
    try
    {
      return Strict.Value.GetBytes(text);
    }
    catch (EncoderFallbackException ex)
    {
      throw new ThemeException($"{what} would need characters that the Windows code page cannot store; it was left unchanged.", ex);
    }
  }

  public static string ReadFile(string path) => Decode(File.ReadAllBytes(path), path);
}
