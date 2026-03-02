using PrimeBakesLibrary.Models.Accounts.Masters;

using SkiaSharp;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace PrimeBakesLibrary.Exporting.Utils;

/// <summary>
/// Unified utility for rendering thermal receipts as high-quality raster images
/// using SkiaSharp, converting to ESC/POS raster commands, and optionally wrapping
/// in a Syncfusion PDF for preview / sharing.
/// <para>
/// Contains reusable drawing primitives, text helpers, ESC/POS conversion, and
/// bitmap-to-PDF wrapping. Specific receipt layouts (bills, test pages, etc.)
/// should compose these helpers from their own classes.
/// </para>
/// </summary>
public static class ThermalPrintUtil
{
	#region Constants

	/// <summary>Standard thermal printer resolution in DPI.</summary>
	public const int PrinterDpi = 203;

	/// <summary>
	/// Full raster image width in dots for 80 mm paper at 203 DPI.
	/// Print head covers ~72 mm → 576 dots.
	/// </summary>
	public const int PaperDots80mm = 576;

	/// <summary>
	/// Full raster image width in dots for 58 mm paper at 203 DPI.
	/// Print head covers ~48 mm → 384 dots.
	/// </summary>
	public const int PaperDots58mm = 384;

	/// <summary>Horizontal margin in dots from each edge.</summary>
	public const int Margin = 4;

	/// <summary>Vertical gap between text lines in dots.</summary>
	public const int LineGap = 1;

	/// <summary>Vertical gap between sections in dots.</summary>
	public const int SectionGap = 12;

	/// <summary>Embedded resource name for the company logo.</summary>
	public const string LogoResourceName = "PrimeBakesLibrary.Exporting.Resources.logo_full.png";

	// Font sizes in pixels (at 203 DPI: 1pt ≈ 2.82px)

	/// <summary>~14pt — company name / large headers.</summary>
	public const float FontSizeTitle = 40f;

	/// <summary>~10.5pt — section titles.</summary>
	public const float FontSizeHeader = 30f;

	/// <summary>~9pt — body text, label-value pairs.</summary>
	public const float FontSizeNormal = 26f;

	/// <summary>~8pt — sub-info (GSTIN, phone, address).</summary>
	public const float FontSizeSmall = 22f;

	#endregion

	#region Drawing Primitives

	/// <summary>
	/// Draws the company logo from the embedded resource, centred horizontally.
	/// </summary>
	/// <returns>Updated Y position after the logo (unchanged if logo is unavailable).</returns>
	public static float DrawLogo(SKCanvas canvas, int paperWidth, float y)
	{
		try
		{
			var assembly = typeof(ThermalPrintUtil).Assembly;
			using var stream = assembly.GetManifestResourceStream(LogoResourceName);
			if (stream is null)
				return y;

			using var logoBitmap = SKBitmap.Decode(stream);
			if (logoBitmap is null)
				return y;

			// Scale to fit within margins, capped at 140 px height, never upscale
			int maxLogoWidth = paperWidth - (2 * Margin);
			float maxLogoHeight = 140f;
			float scale = Math.Min(
				(float)maxLogoWidth / logoBitmap.Width,
				maxLogoHeight / logoBitmap.Height);
			scale = Math.Min(scale, 1f);

			int logoW = (int)(logoBitmap.Width * scale);
			int logoH = (int)(logoBitmap.Height * scale);
			float logoX = (paperWidth - logoW) / 2f;

			var dest = new SKRect(logoX, y, logoX + logoW, y + logoH);
			using var paint = new SKPaint { IsAntialias = true };
			canvas.DrawBitmap(logoBitmap, dest, paint);

			return y + logoH + SectionGap;
		}
		catch
		{
			return y; // Logo unavailable — skip silently
		}
	}

	/// <summary>
	/// Draws the company header block (alias, GSTIN, phone, email, address) centred below the logo.
	/// </summary>
	public static float DrawCompanyHeader(SKCanvas canvas, CompanyModel company, int paperWidth, float y)
	{
		if (!string.IsNullOrWhiteSpace(company.Alias))
			y = DrawCenteredText(canvas, company.Alias, paperWidth, y, FontSizeNormal, bold: true);

		if (!string.IsNullOrWhiteSpace(company.GSTNo))
			y = DrawCenteredText(canvas, $"GSTIN: {company.GSTNo}", paperWidth, y, FontSizeSmall, bold: true);

		if (!string.IsNullOrWhiteSpace(company.Phone))
			y = DrawCenteredText(canvas, $"Ph: {company.Phone}", paperWidth, y, FontSizeSmall, bold: true);

		if (!string.IsNullOrWhiteSpace(company.Email))
			y = DrawCenteredText(canvas, company.Email, paperWidth, y, FontSizeSmall, bold: true);

		if (!string.IsNullOrWhiteSpace(company.Address))
			y = DrawCenteredText(canvas, company.Address, paperWidth, y, FontSizeSmall, bold: true);

		return y + SectionGap;
	}

	/// <summary>
	/// Draws horizontally centred text, word-wrapping if the text exceeds the printable width.
	/// </summary>
	/// <returns>Updated Y position after the text.</returns>
	public static float DrawCenteredText(
		SKCanvas canvas, string text, int paperWidth, float y, float fontSize, bool bold)
	{
		using var typeface = SKTypeface.FromFamilyName("sans-serif", bold ? SKFontStyle.Bold : SKFontStyle.Normal);
		using var font = new SKFont(typeface, fontSize);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

		float maxWidth = paperWidth - (2 * Margin);
		var lines = WrapText(text, font, maxWidth);
		var metrics = font.Metrics;
		float lineHeight = metrics.Descent - metrics.Ascent;

		foreach (var line in lines)
		{
			float textWidth = font.MeasureText(line);
			float x = (paperWidth - textWidth) / 2f;
			canvas.DrawText(line, x, y - metrics.Ascent, font, paint);
			y += lineHeight + LineGap;
		}

		return y;
	}

	/// <summary>
	/// Draws a block of left-aligned label-value pairs with all values starting at the same
	/// X position (determined by the widest label). Long values are word-wrapped and continuation
	/// lines are indented to the value column.
	/// </summary>
	/// <param name="canvas">Target drawing canvas.</param>
	/// <param name="pairs">Ordered list of (label, value) tuples to render.</param>
	/// <param name="paperWidth">Total paper width in dots.</param>
	/// <param name="y">Starting Y position.</param>
	/// <param name="fontSize">Font size for both labels and values (default <see cref="FontSizeNormal"/>).</param>
	/// <returns>Updated Y position after all rows.</returns>
	public static float DrawLabelValueBlock(
		SKCanvas canvas,
		List<(string Label, string Value)> pairs,
		int paperWidth,
		float y,
		float fontSize = FontSizeNormal)
	{
		if (pairs is null || pairs.Count == 0)
			return y;

		using var boldTypeface = SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold);
		using var normalTypeface = SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal);
		using var labelFont = new SKFont(boldTypeface, fontSize);
		using var valueFont = new SKFont(normalTypeface, fontSize);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

		var metrics = labelFont.Metrics;
		float lineHeight = metrics.Descent - metrics.Ascent;

		// Measure the widest label to determine the shared value-start column
		float maxLabelWidth = 0;
		foreach (var (label, _) in pairs)
		{
			float w = labelFont.MeasureText($"{label}: ");
			if (w > maxLabelWidth)
				maxLabelWidth = w;
		}

		// Add a small gap after the colon column
		float valueX = Margin + maxLabelWidth + 4;
		float maxValueWidth = paperWidth - valueX - Margin;

		// Draw each pair
		foreach (var (label, value) in pairs)
		{
			// Bold label
			canvas.DrawText($"{label}: ", Margin, y - metrics.Ascent, labelFont, paint);

			// Value (word-wrapped, aligned to the value column)
			var lines = WrapText(value ?? string.Empty, valueFont, maxValueWidth);
			for (int i = 0; i < lines.Count; i++)
			{
				canvas.DrawText(lines[i], valueX, y - metrics.Ascent, valueFont, paint);
				y += lineHeight + LineGap;
			}
		}

		return y;
	}

	/// <summary>
	/// Draws a solid separator line spanning the full paper width.
	/// </summary>
	public static float DrawSeparator(SKCanvas canvas, int paperWidth, float y)
	{
		using var paint = new SKPaint
		{
			Color = SKColors.Black,
			StrokeWidth = 2f,
			Style = SKPaintStyle.Stroke
		};

		float lineY = y + 6;
		canvas.DrawLine(0, lineY, paperWidth, lineY, paint);
		return lineY + SectionGap;
	}

	#endregion

	#region Text Utilities

	/// <summary>
	/// Word-wraps text to fit within a maximum pixel width using the specified font metrics.
	/// </summary>
	public static List<string> WrapText(string text, SKFont font, float maxWidth)
	{
		var lines = new List<string>();
		if (string.IsNullOrEmpty(text))
		{
			lines.Add(string.Empty);
			return lines;
		}

		var words = text.Split(' ');
		string currentLine = string.Empty;

		foreach (var word in words)
		{
			// If a single word is wider than maxWidth, break it character-by-character
			if (font.MeasureText(word) > maxWidth)
			{
				// Flush any pending line first
				if (!string.IsNullOrEmpty(currentLine))
				{
					lines.Add(currentLine);
					currentLine = string.Empty;
				}

				// Break the long word at character boundaries
				string chunk = string.Empty;
				foreach (char c in word)
				{
					string test = chunk + c;
					if (font.MeasureText(test) > maxWidth && chunk.Length > 0)
					{
						lines.Add(chunk);
						chunk = c.ToString();
					}
					else
					{
						chunk = test;
					}
				}

				currentLine = chunk;
				continue;
			}

			var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
			float testWidth = font.MeasureText(testLine);

			if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
			{
				lines.Add(currentLine);
				currentLine = word;
			}
			else
			{
				currentLine = testLine;
			}
		}

		if (!string.IsNullOrEmpty(currentLine))
			lines.Add(currentLine);

		if (lines.Count == 0)
			lines.Add(string.Empty);

		return lines;
	}

	#endregion

	#region Bitmap Utilities

	/// <summary>
	/// Crops an <see cref="SKBitmap"/> to the specified height, discarding empty space below content.
	/// </summary>
	public static SKBitmap CropBitmap(SKBitmap source, int width, int height)
	{
		height = Math.Min(height, source.Height);
		var cropped = new SKBitmap(width, height);
		using var canvas = new SKCanvas(cropped);
		var rect = new SKRect(0, 0, width, height);
		canvas.DrawBitmap(source, rect, rect);
		return cropped;
	}

	#endregion

	#region Conversion — SkiaSharp Bitmap → ESC/POS Raster

	/// <summary>
	/// Converts an <see cref="SKBitmap"/> to ESC/POS raster bytes ready for thermal printing.
	/// The bitmap is encoded as PNG, then converted to monochrome raster data (GS v 0 command).
	/// The output includes printer initialisation (ESC @), the raster image, line feed, and paper cut.
	/// </summary>
	public static byte[] ConvertBitmapToThermalBytes(SKBitmap bitmap, int maxWidthDots)
	{
		using var ms = new MemoryStream();
		Initialize(ms);

		// Encode the bitmap as PNG bytes
		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);

		if (data is null)
		{
			Console.WriteLine("ThermalPrintUtil: Failed to encode bitmap as PNG.");
			FeedAndCut(ms);
			return ms.ToArray();
		}

		var imageBytes = data.ToArray();

		// Convert to ESC/POS monochrome raster image
		var rasterBytes = BuildRasterImage(imageBytes, maxWidthDots, center: true);
		if (rasterBytes is not null)
			ms.Write(rasterBytes);

		FeedAndCut(ms);
		return ms.ToArray();
	}

	#endregion

	#region Conversion — SkiaSharp Bitmap → Syncfusion PDF

	/// <summary>
	/// Wraps an <see cref="SKBitmap"/> into a Syncfusion PDF document sized to thermal paper dimensions.
	/// Useful for previewing the thermal receipt on screen or sharing as a PDF file.
	/// </summary>
	public static MemoryStream WrapBitmapInPdf(SKBitmap bitmap, int paperWidthDots)
	{
		// Convert dots to PDF points: points = dots / DPI × 72
		float pdfWidth = paperWidthDots / (float)PrinterDpi * 72f;
		float pdfHeight = bitmap.Height / (float)PrinterDpi * 72f;

		var ms = new MemoryStream();
		using var pdfDoc = new PdfDocument();

		pdfDoc.PageSettings.Size = new SizeF(pdfWidth, pdfHeight);
		pdfDoc.PageSettings.Margins.All = 0;

		var page = pdfDoc.Pages.Add();
		var graphics = page.Graphics;

		// Embed the rendered bitmap as a full-page image
		using var encoded = SKImage.FromBitmap(bitmap)?.Encode(SKEncodedImageFormat.Png, 100);
		if (encoded is not null)
		{
			using var imageStream = new MemoryStream(encoded.ToArray());
			var pdfImage = new PdfBitmap(imageStream);
			graphics.DrawImage(pdfImage, 0, 0, pdfWidth, pdfHeight);
		}

		pdfDoc.Save(ms);
		ms.Position = 0;
		return ms;
	}

	#endregion

	#region ESC/POS Command Helpers

	/// <summary>
	/// Initialises the printer (ESC @).
	/// Should be the first command in every print job.
	/// </summary>
	public static void Initialize(MemoryStream ms)
		=> ms.Write([0x1B, 0x40]);

	/// <summary>
	/// Feeds n lines using the proper ESC d command, then performs a partial paper cut.
	/// This ensures text is not cut off by the cutter blade.
	/// </summary>
	/// <param name="ms">Target stream.</param>
	/// <param name="feedLines">Number of lines to feed before cutting (default 5).</param>
	public static void FeedAndCut(MemoryStream ms, byte feedLines = 5)
	{
		// ESC d n — Print and feed n lines
		ms.Write([0x1B, 0x64, feedLines]);

		// GS V 1 — Partial cut
		ms.Write([0x1D, 0x56, 0x01]);
	}

	/// <summary>
	/// Sets text alignment (ESC a n).
	/// </summary>
	/// <param name="ms">Target stream.</param>
	/// <param name="alignment">0 = Left, 1 = Centre, 2 = Right.</param>
	private static void SetAlignment(MemoryStream ms, int alignment)
		=> ms.Write([0x1B, 0x61, (byte)alignment]);

	#endregion

	#region Image Rasterisation

	/// <summary>
	/// Converts a PNG/JPEG image stream into ESC/POS raster image bytes (GS v 0 command).
	/// The image is resized to fit the paper width and converted to monochrome.
	/// Returns null if the image cannot be decoded.
	/// </summary>
	/// <param name="imageStream">Readable stream containing the source image.</param>
	/// <param name="maxWidthDots">Maximum image width in dots (default 576 for 80 mm paper).</param>
	/// <param name="threshold">Luminance threshold (0-255) below which a pixel is printed as black.</param>
	/// <param name="center">Whether to prepend a centre-align command before the image.</param>
	public static byte[] BuildRasterImage(
		Stream imageStream,
		int maxWidthDots = PaperDots80mm,
		int threshold = 128,
		bool center = true)
	{
		try
		{
			using var original = SKBitmap.Decode(imageStream);
			if (original is null)
				return null;

			return BuildRasterImageFromBitmap(original, maxWidthDots, threshold, center);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Raster image conversion failed: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Converts raw image bytes into ESC/POS raster image bytes (GS v 0 command).
	/// </summary>
	/// <param name="imageBytes">Raw image file bytes (PNG, JPEG, etc.).</param>
	/// <param name="maxWidthDots">Maximum image width in dots.</param>
	/// <param name="threshold">Luminance threshold for monochrome conversion.</param>
	/// <param name="center">Whether to centre-align the image.</param>
	public static byte[] BuildRasterImage(
		byte[] imageBytes,
		int maxWidthDots = PaperDots80mm,
		int threshold = 128,
		bool center = true)
	{
		try
		{
			using var original = SKBitmap.Decode(imageBytes);
			if (original is null)
				return null;

			return BuildRasterImageFromBitmap(original, maxWidthDots, threshold, center);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Raster image conversion failed: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Core rasterisation logic shared by the Stream and byte[] overloads.
	/// </summary>
	private static byte[] BuildRasterImageFromBitmap(
		SKBitmap original,
		int maxWidthDots,
		int threshold,
		bool center)
	{
		// Calculate resize dimensions maintaining aspect ratio
		float scale = Math.Min((float)maxWidthDots / original.Width, 1.0f);
		int newWidth = (int)(original.Width * scale);
		int newHeight = (int)(original.Height * scale);

		// Width must be a multiple of 8 for byte alignment in raster data
		newWidth = (newWidth + 7) / 8 * 8;

		// Resize image
		using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
		if (resized is null)
			return null;

		// Convert to monochrome raster data (1 bit per pixel, MSB first)
		int widthBytes = newWidth / 8;
		var rasterData = new byte[widthBytes * newHeight];

		for (int y = 0; y < newHeight; y++)
		{
			for (int x = 0; x < newWidth; x++)
			{
				var pixel = resized.GetPixel(x, y);

				// Handle transparency: transparent pixels → white (not printed)
				float alpha = pixel.Alpha / 255f;
				float luminance = ((0.299f * pixel.Red) + (0.587f * pixel.Green) + (0.114f * pixel.Blue)) * alpha
								+ (255f * (1f - alpha));

				// Dark pixels (luminance < threshold) → bit = 1 (printed)
				if (luminance < threshold)
				{
					int byteIndex = (y * widthBytes) + (x / 8);
					int bitIndex = 7 - (x % 8); // MSB first
					rasterData[byteIndex] |= (byte)(1 << bitIndex);
				}
			}
		}

		// Build the GS v 0 command: $1D $76 $30 m xL xH yL yH d1...dk
		using var ms = new MemoryStream();

		if (center)
			SetAlignment(ms, 1);

		byte xL = (byte)(widthBytes & 0xFF);
		byte xH = (byte)((widthBytes >> 8) & 0xFF);
		byte yL = (byte)(newHeight & 0xFF);
		byte yH = (byte)((newHeight >> 8) & 0xFF);

		ms.Write([0x1D, 0x76, 0x30, 0x00, xL, xH, yL, yH]);
		ms.Write(rasterData);

		return ms.ToArray();
	}

	#endregion
}
