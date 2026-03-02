using System.Text;

using PrimeBakesLibrary.Models.Accounts.Masters;

using SkiaSharp;

namespace PrimeBakesLibrary.Exporting.Utils;

/// <summary>
/// Utility for building ESC/POS byte payloads for thermal receipt printers.
/// Provides helper methods for text formatting, image rasterisation, and
/// common receipt structures (headers, tables, footers).
/// </summary>
public static class ThermalPrintUtil
{
	#region Constants

	/// <summary>Standard line width for 80 mm paper with Font A (12×24).</summary>
	public const int LineWidth80mm = 48;

	/// <summary>Standard line width for 58 mm paper with Font A (12×24).</summary>
	public const int LineWidth58mm = 32;

	/// <summary>Maximum raster image width in dots for 80 mm paper at 203 DPI.</summary>
	public const int MaxImageDots80mm = 384;

	/// <summary>Maximum raster image width in dots for 58 mm paper at 203 DPI.</summary>
	public const int MaxImageDots58mm = 384;

	#endregion

	#region ESC/POS Command Helpers

	/// <summary>
	/// Initialises the printer (ESC @).
	/// Should be the first command in every print job.
	/// </summary>
	public static void Initialize(MemoryStream ms)
		=> ms.Write([0x1B, 0x40]);

	/// <summary>
	/// Sets text alignment.
	/// </summary>
	/// <param name="ms">Target stream.</param>
	/// <param name="alignment">0 = Left, 1 = Centre, 2 = Right.</param>
	public static void SetAlignment(MemoryStream ms, int alignment)
		=> ms.Write([0x1B, 0x61, (byte)alignment]);

	/// <summary>
	/// Turns bold (emphasised) mode on or off (ESC E n).
	/// </summary>
	public static void SetBold(MemoryStream ms, bool on)
		=> ms.Write([0x1B, 0x45, (byte)(on ? 0x01 : 0x00)]);

	/// <summary>
	/// Sets character size via GS ! n.
	/// 0x00 = normal, 0x11 = double width + double height.
	/// </summary>
	public static void SetCharacterSize(MemoryStream ms, byte size)
		=> ms.Write([0x1D, 0x21, size]);

	/// <summary>
	/// Resets character size to normal and bold to off.
	/// Useful after headers or emphasis blocks.
	/// </summary>
	public static void ResetFont(MemoryStream ms)
	{
		SetCharacterSize(ms, 0x00);
		SetBold(ms, false);
	}

	/// <summary>
	/// Sets horizontal tab positions using ESC D.
	/// Positions are absolute column numbers; the list is NUL-terminated automatically.
	/// </summary>
	public static void SetTabPositions(MemoryStream ms, params byte[] positions)
	{
		ms.Write([0x1B, 0x44]);
		ms.Write(positions);
		ms.WriteByte(0x00); // NUL terminator
	}

	/// <summary>
	/// Writes a horizontal tab character (HT, 0x09) to advance to the next tab stop.
	/// </summary>
	public static void WriteTab(MemoryStream ms)
		=> ms.WriteByte(0x09);

	/// <summary>
	/// Writes a UTF-8 encoded text string to the stream.
	/// </summary>
	public static void WriteText(MemoryStream ms, string text)
		=> ms.Write(Encoding.UTF8.GetBytes(text));

	/// <summary>
	/// Writes one or more line-feed characters (0x0A) to the stream.
	/// </summary>
	public static void WriteLf(MemoryStream ms, int count = 1)
	{
		for (var i = 0; i < count; i++)
			ms.WriteByte(0x0A);
	}

	/// <summary>
	/// Feeds n lines using the proper ESC d command, then performs a partial paper cut.
	/// This ensures text is not cut off by the cutter blade.
	/// </summary>
	/// <param name="ms">Target stream.</param>
	/// <param name="feedLines">Number of lines to feed before cutting (default 6).</param>
	public static void FeedAndCut(MemoryStream ms, byte feedLines = 5)
	{
		// ESC d n — Print and feed n lines
		ms.Write([0x1B, 0x64, feedLines]);

		// GS V 1 — Partial cut
		ms.Write([0x1D, 0x56, 0x01]);
	}

	/// <summary>
	/// Writes a full-width separator line.
	/// Default character is '_' which prints as a continuous straight line on thermal printers.
	/// </summary>
	public static void WriteSeparator(MemoryStream ms, int lineWidth = LineWidth80mm, char character = '-')
	{
		SetBold(ms, true);
		WriteText(ms, new string(character, lineWidth));
		SetBold(ms, false);
		WriteLf(ms);
	}

	#endregion

	#region Text Formatting Helpers

	/// <summary>
	/// Right-pads a string to the specified width, truncating if longer.
	/// </summary>
	public static string PadRight(string text, int width) =>
		text.Length >= width ? text[..width] : text + new string(' ', width - text.Length);

	/// <summary>
	/// Left-pads a string to the specified width, truncating if longer.
	/// </summary>
	public static string PadLeft(string text, int width) =>
		text.Length >= width ? text[..width] : new string(' ', width - text.Length) + text;

	/// <summary>
	/// Word-wraps text to fit within the given character width,
	/// breaking on spaces when possible.
	/// </summary>
	public static List<string> WordWrap(string text, int maxWidth)
	{
		var lines = new List<string>();
		if (string.IsNullOrEmpty(text) || maxWidth <= 0)
		{
			lines.Add(string.Empty);
			return lines;
		}

		while (text.Length > 0)
		{
			if (text.Length <= maxWidth)
			{
				lines.Add(text);
				break;
			}

			var breakAt = text.LastIndexOf(' ', maxWidth - 1);
			if (breakAt <= 0)
				breakAt = maxWidth;

			lines.Add(text[..breakAt].TrimEnd());
			text = text[breakAt..].TrimStart();
		}

		return lines;
	}

	/// <summary>
	/// Writes a label-value pair with proper indented wrapping.
	/// The first line prints "Label : Value" and any continuation lines
	/// are indented to align under the value, not the label.
	/// </summary>
	/// <param name="ms">Target stream.</param>
	/// <param name="label">Field label (e.g. "Printer").</param>
	/// <param name="value">Field value which may be long and need wrapping.</param>
	/// <param name="labelWidth">Total width of the label area including separator (default 10).</param>
	/// <param name="lineWidth">Total line width for wrapping (default 48 for 80 mm paper).</param>
	public static void WriteLabelValue(MemoryStream ms, string label, string value, int labelWidth = 10, int lineWidth = LineWidth80mm)
	{
		var prefix = PadRight($"{label}: ", labelWidth);
		var valueWidth = lineWidth - labelWidth;
		var valueLines = WordWrap(value ?? string.Empty, valueWidth);

		// First line: label + value
		WriteText(ms, prefix + valueLines[0]);
		WriteLf(ms);

		// Continuation lines: indent to align under the value
		var indent = new string(' ', labelWidth);
		for (var i = 1; i < valueLines.Count; i++)
		{
			WriteText(ms, indent + valueLines[i]);
			WriteLf(ms);
		}
	}

	#endregion

	#region Company Header

	/// <summary>
	/// Embedded resource name for the company logo.
	/// </summary>
	private const string LogoResourceName = "PrimeBakesLibrary.Exporting.Resources.logo_full.png";

	/// <summary>
	/// Writes the full company header to the print stream: logo + company info.
	/// This is mandatory for all receipts. The logo is loaded from an embedded
	/// resource; company details (name, alias, GST, address, email, phone) are
	/// printed centre-aligned below the logo. Long addresses are word-wrapped.
	/// </summary>
	/// <param name="ms">Target stream.</param>
	/// <param name="company">Company data to print. If null, only the logo / text fallback is printed.</param>
	/// <param name="lineWidth">Character line width for address wrapping (default 48 for 80 mm paper).</param>
	public static void WriteCompanyHeader(MemoryStream ms, CompanyModel company = null, int lineWidth = LineWidth80mm)
	{
		// --- Logo ---
		bool logoWritten = false;
		try
		{
			var assembly = typeof(ThermalPrintUtil).Assembly;
			using var logoStream = assembly.GetManifestResourceStream(LogoResourceName);

			if (logoStream is not null)
			{
				var logoRaster = BuildRasterImage(logoStream);
				if (logoRaster is not null)
				{
					ms.Write(logoRaster);
					WriteLf(ms);
					logoWritten = true;
				}
			}
		}
		catch
		{
			// Fall through to text header
		}

		// Centre-align the entire header block
		SetAlignment(ms, 1);

		// Text fallback if logo resource is unavailable
		if (!logoWritten)
		{
			SetBold(ms, true);
			SetCharacterSize(ms, 0x11);
			WriteText(ms, company?.Name ?? "PRIME BAKES");
			WriteLf(ms);
			ResetFont(ms);
		}

		// --- Company info (bold) ---
		if (company is not null)
		{
			SetBold(ms, true);

			if (!string.IsNullOrEmpty(company.Alias))
			{
				WriteText(ms, company.Alias);
				WriteLf(ms);
			}

			if (!string.IsNullOrEmpty(company.GSTNo))
			{
				WriteText(ms, $"GSTIN: {company.GSTNo}");
				WriteLf(ms);
			}

			if (!string.IsNullOrEmpty(company.Phone))
			{
				WriteText(ms, $"Ph: {company.Phone}");
				WriteLf(ms);
			}

			if (!string.IsNullOrEmpty(company.Email))
			{
				WriteText(ms, company.Email);
				WriteLf(ms);
			}

			if (!string.IsNullOrEmpty(company.Address))
			{
				var addressLines = WordWrap(company.Address, lineWidth);
				foreach (var line in addressLines)
				{
					WriteText(ms, line);
					WriteLf(ms);
				}
			}

			SetBold(ms, false);
		}

		WriteSeparator(ms, lineWidth);
	}

	/// <summary>
	/// Writes the standard receipt footer: separator, thank-you message, and branding.
	/// This is mandatory for all receipts.
	/// </summary>
	public static void WriteFooter(MemoryStream ms, int lineWidth = LineWidth80mm)
	{
		SetAlignment(ms, 1);
		WriteText(ms, "Thanks. Visit Again");
		WriteLf(ms);
		WriteText(ms, "A Product of");
		WriteLf(ms);
		SetBold(ms, true);
		WriteText(ms, "aadisoft.vercel.app");
		SetBold(ms, false);
		WriteLf(ms);
	}

	#endregion

	#region Image / Logo Rasterisation

	/// <summary>
	/// Converts a PNG/JPEG image stream into ESC/POS raster image bytes (GS v 0 command).
	/// The image is resized to fit the paper width and converted to monochrome.
	/// Returns null if the image cannot be decoded.
	/// </summary>
	/// <param name="imageStream">Readable stream containing the source image (PNG, JPEG, etc.).</param>
	/// <param name="maxWidthDots">Maximum image width in dots (default 384 for 80 mm paper at 203 DPI).</param>
	/// <param name="threshold">Luminance threshold (0-255) below which a pixel is printed as black. Default 128.</param>
	/// <param name="center">Whether to prepend an ESC a 1 (centre align) command before the image.</param>
	public static byte[] BuildRasterImage(
		Stream imageStream,
		int maxWidthDots = MaxImageDots80mm,
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
		int maxWidthDots = MaxImageDots80mm,
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
