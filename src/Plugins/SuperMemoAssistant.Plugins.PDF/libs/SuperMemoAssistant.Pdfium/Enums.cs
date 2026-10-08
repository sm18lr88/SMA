namespace SuperMemoAssistant.Pdfium;

using System;

/// <summary>Clockwise page rotation, as PDFium expects it.</summary>
public enum PageRotate
{
  Normal = 0,
  Rotate90 = 1,
  Rotate180 = 2,
  Rotate270 = 3,
}

/// <summary>Rendering options. Every value except <see cref="Thumbnail" /> is a PDFium FPDF_* render flag.</summary>
[Flags]
public enum RenderFlags
{
  None = 0,
  Annotations = 0x01,
  LcdText = 0x02,
  Grayscale = 0x08,
  Printing = 0x800,

  /// <summary>Viewer hint: draw a scaled-down full render. PDFium never receives this bit.</summary>
  Thumbnail = 0x4000_0000,
}

/// <summary>State of a progressive render (FPDF_RENDER_*).</summary>
public enum ProgressiveStatus
{
  Ready = 0,
  ToBeContinued = 1,
  Done = 2,
  Failed = 3,
}

/// <summary>How <see cref="PdfBitmap.BlendRect" /> mixes a color into the bitmap.</summary>
public enum BlendMode
{
  Normal,
  Multiply,
}

/// <summary>Page object kinds (FPDF_PAGEOBJ_*).</summary>
public enum PageObjectType
{
  Unknown = 0,
  Text = 1,
  Path = 2,
  Image = 3,
  Shading = 4,
  Form = 5,
}

/// <summary>Action kinds (PDFACTION_*).</summary>
public enum ActionType
{
  Unsupported = 0,
  GoTo = 1,
  RemoteGoTo = 2,
  Uri = 3,
  Launch = 4,
  EmbeddedGoTo = 5,
}

/// <summary>Destination view modes (PDFDEST_VIEW_*).</summary>
public enum DestinationType
{
  Unknown = 0,
  Xyz = 1,
  Fit = 2,
  FitH = 3,
  FitV = 4,
  FitR = 5,
  FitB = 6,
  FitBH = 7,
  FitBV = 8,
}

/// <summary>Text search options (FPDF_MATCHCASE, FPDF_MATCHWHOLEWORD, FPDF_CONSECUTIVE).</summary>
[Flags]
public enum FindFlags
{
  None = 0,
  MatchCase = 1,
  MatchWholeWord = 2,
  Consecutive = 4,
}

/// <summary>Font descriptor flags from the PDF specification (ISO 32000-1, table 123).</summary>
[Flags]
public enum FontFlags
{
  None = 0,
  FixedPitch = 1 << 0,
  Serif = 1 << 1,
  Symbolic = 1 << 2,
  Script = 1 << 3,
  NonSymbolic = 1 << 5,
  Italic = 1 << 6,
  AllCap = 1 << 16,
  SmallCap = 1 << 17,
  ForceBold = 1 << 18,
}

/// <summary>Standard font weights.</summary>
public enum FontWeight
{
  Thin = 100,
  ExtraLight = 200,
  Light = 300,
  Normal = 400,
  Medium = 500,
  SemiBold = 600,
  Bold = 700,
  ExtraBold = 800,
  Heavy = 900,
}

/// <summary>Interactive form field kinds (FPDF_FORMFIELD_*).</summary>
public enum FormFieldType
{
  None = -1,
  Unknown = 0,
  PushButton = 1,
  CheckBox = 2,
  RadioButton = 3,
  ComboBox = 4,
  ListBox = 5,
  TextField = 6,
  Signature = 7,
}

/// <summary>Cursor shapes that the form engine requests (FXCT_*).</summary>
public enum CursorType
{
  Arrow = 0,
  Nesw = 1,
  Nwse = 2,
  VBeam = 3,
  HBeam = 4,
  Hand = 5,
}

/// <summary>Keyboard and mouse modifiers (FWL_EVENTFLAG_*).</summary>
[Flags]
public enum KeyboardModifiers
{
  None = 0,
  ShiftKey = 1 << 0,
  ControlKey = 1 << 1,
  AltKey = 1 << 2,
}
