namespace SuperMemoAssistant.Pdfium;

using System;

/// <summary>PDFium error codes (FPDF_ERR_*).</summary>
public enum PdfiumError
{
  Success = 0,
  Unknown = 1,
  File = 2,
  Format = 3,
  Password = 4,
  Security = 5,
  Page = 6,
}

/// <summary>PDFium could not complete an operation.</summary>
public class PdfiumException : Exception
{
  public PdfiumException(PdfiumError error)
    : base(Describe(error)) => Error = error;

  public PdfiumException(string message)
    : base(message) => Error = PdfiumError.Unknown;

  public PdfiumException(string message, Exception innerException)
    : base(message, innerException) => Error = PdfiumError.Unknown;

  public PdfiumError Error { get; }

  private static string Describe(PdfiumError error) => error switch
  {
    PdfiumError.File => "The file could not be found or opened.",
    PdfiumError.Format => "The file is not a PDF document, or it is damaged.",
    PdfiumError.Password => "The document needs a password, or the password is not correct.",
    PdfiumError.Security => "The document uses an unsupported security scheme.",
    PdfiumError.Page => "The page could not be found, or its content is damaged.",
    _ => "PDFium reported an unknown error.",
  };
}

/// <summary>The document needs a password, or the password is not correct.</summary>
public sealed class InvalidPasswordException : PdfiumException
{
  public InvalidPasswordException()
    : base(PdfiumError.Password) { }
}
