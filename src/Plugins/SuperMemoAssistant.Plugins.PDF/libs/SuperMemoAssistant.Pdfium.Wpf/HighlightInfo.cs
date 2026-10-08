using System.Windows.Media;

namespace SuperMemoAssistant.Pdfium.Wpf
{
	/// <summary>
	/// Represents information about highlighted text in the control
	/// </summary>
	public struct HighlightInfo
	{
		/// <summary>
		/// The starting character index of the highlighted text.
		/// </summary>
		public int CharIndex;

		/// <summary>
		/// The number of highlighted characters in the text
		/// </summary>
		public int CharsCount;

        /// <summary>
        /// Text highlighted color
        /// </summary>
        public Color Color;

        /// <summary>
        /// Gets or sets delta values for each edge of the rectangles of the highlighted text.
        /// </summary>
        public PdfRect Inflate { get; set; }
    }
}
