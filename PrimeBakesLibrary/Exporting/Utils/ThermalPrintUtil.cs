using PrimeBakesLibrary.Models.Accounts.Masters;

using SkiaSharp;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace PrimeBakesLibrary.Exporting.Utils;

/// <summary>Unified utility for rendering thermal receipts as SkiaSharp raster images and converting to ESC/POS bytes.</summary>
public static class ThermalPrintUtil
{
	// ── Constants ────────────────────────────────────────────────────────────

	public const int PrinterDpi = 203;
	public const int PaperDots80mm = 576;
	public const int PaperDots58mm = 384;
	public const int Margin = 4;
	public const int LineGap = 1;
	public const int SectionGap = 12;
	public const string LogoResourceName = "PrimeBakesLibrary.Exporting.Resources.logo_full.png";

	public const float FontSizeTitle = 40f;
	public const float FontSizeHeader = 30f;
	public const float FontSizeNormal = 26f;
	public const float FontSizeSmall = 22f;

	private const float ColGap = 12f;

	// ── Font helpers ─────────────────────────────────────────────────────────

	private static SKTypeface BoldTypeface() =>
		SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold);

	private static SKTypeface SemiBoldTypeface() =>
		SKTypeface.FromFamilyName("sans-serif",
			new SKFontStyle(SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright));

	// ── Drawing primitives ───────────────────────────────────────────────────

	/// <summary>Draws the embedded logo centred; returns updated Y.</summary>
	public static float DrawLogo(SKCanvas canvas, int paperWidth, float y)
	{
		try
		{
			using var stream = typeof(ThermalPrintUtil).Assembly.GetManifestResourceStream(LogoResourceName);
			if (stream is null) return y;

			using var logoBitmap = SKBitmap.Decode(stream);
			if (logoBitmap is null) return y;

			float scale = Math.Min(
				Math.Min((float)(paperWidth - 2 * Margin) / logoBitmap.Width, 140f / logoBitmap.Height),
				1f);

			int logoW = (int)(logoBitmap.Width * scale);
			int logoH = (int)(logoBitmap.Height * scale);
			float logoX = (paperWidth - logoW) / 2f;

			using var paint = new SKPaint { IsAntialias = true };
			canvas.DrawBitmap(logoBitmap, new SKRect(logoX, y, logoX + logoW, y + logoH), paint);
			return y + logoH + SectionGap;
		}
		catch { return y; }
	}

	/// <summary>Draws company name, GSTIN, phone, email, address centred; returns updated Y.</summary>
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

	/// <summary>Draws word-wrapped centred text; returns updated Y.</summary>
	public static float DrawCenteredText(SKCanvas canvas, string text, int paperWidth, float y, float fontSize, bool bold)
	{
		using var typeface = bold ? BoldTypeface() : SemiBoldTypeface();
		using var font = new SKFont(typeface, fontSize);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		var metrics = font.Metrics;
		float lineH = metrics.Descent - metrics.Ascent;

		foreach (var line in WrapText(text, font, paperWidth - 2 * Margin))
		{
			canvas.DrawText(line, (paperWidth - font.MeasureText(line)) / 2f, y - metrics.Ascent, font, paint);
			y += lineH + LineGap;
		}
		return y;
	}

	/// <summary>
	/// Draws left-aligned label-value pairs. Labels are bold, values semi-bold.
	/// All values start at the same X column (widest label). Long values word-wrap.
	/// </summary>
	public static float DrawLabelValueBlock(SKCanvas canvas, List<(string Label, string Value)> pairs,
		int paperWidth, float y, float fontSize = FontSizeNormal)
	{
		if (pairs is null || pairs.Count == 0) return y;

		using var boldTf = BoldTypeface();
		using var semiTf = SemiBoldTypeface();
		using var labelFont = new SKFont(boldTf, fontSize);
		using var valueFont = new SKFont(semiTf, fontSize);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		var metrics = labelFont.Metrics;
		float lineH = metrics.Descent - metrics.Ascent;

		float maxLabelW = 0;
		foreach (var (label, _) in pairs)
		{
			float w = labelFont.MeasureText($"{label}: ");
			if (w > maxLabelW) maxLabelW = w;
		}

		float valueX = Margin + maxLabelW + 4;
		float maxValueW = paperWidth - valueX - Margin;

		foreach (var (label, value) in pairs)
		{
			canvas.DrawText($"{label}: ", Margin, y - metrics.Ascent, labelFont, paint);
			foreach (var line in WrapText(value ?? string.Empty, valueFont, maxValueW))
			{
				canvas.DrawText(line, valueX, y - metrics.Ascent, valueFont, paint);
				y += lineH + LineGap;
			}
		}
		return y;
	}

	/// <summary>Draws a full-width solid separator with equal spacing above and below; returns updated Y.</summary>
	public static float DrawSeparator(SKCanvas canvas, int paperWidth, float y)
	{
		using var paint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2f, Style = SKPaintStyle.Stroke };
		float half = SectionGap / 2f;
		float lineY = y + half;
		canvas.DrawLine(0, lineY, paperWidth, lineY, paint);
		return lineY + half;
	}

	/// <summary>
	/// Draws a table with bold headers and semi-bold rows.
	/// Column 0 word-wraps with a 12px continuation indent.
	/// </summary>
	public static float DrawTable(SKCanvas canvas, string[] headers, SKTextAlign[] alignments,
		float[] columnPercents, List<string[]> rows, int paperWidth, float y,
		float headerFontSize = FontSizeHeader, float rowFontSize = FontSizeNormal)
	{
		(var colX, var colW) = BuildColumns(columnPercents, paperWidth);

		using var boldTf = BoldTypeface();
		using var semiTf = SemiBoldTypeface();
		using var headerFont = new SKFont(boldTf, headerFontSize);
		using var rowFont = new SKFont(semiTf, rowFontSize);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

		var hm = headerFont.Metrics;
		float hLineH = hm.Descent - hm.Ascent;
		for (int i = 0; i < headers.Length; i++)
		{
			float tw = headerFont.MeasureText(headers[i]);
			canvas.DrawText(headers[i], AlignX(alignments[i], colX[i], colW[i], tw), y - hm.Ascent, headerFont, paint);
		}
		y += hLineH + LineGap;
		y = DrawSeparator(canvas, paperWidth, y);

		var rm = rowFont.Metrics;
		float rLineH = rm.Descent - rm.Ascent;
		foreach (var row in rows)
		{
			float maxCellH = rLineH;
			for (int i = 0; i < Math.Min(row.Length, headers.Length); i++)
			{
				string cell = row[i] ?? string.Empty;
				if (i == 0)
				{
					var lines = WrapText(cell, rowFont, colW[i] - 4);
					float cellY = y;
					for (int li = 0; li < lines.Count; li++)
					{
						canvas.DrawText(lines[li], li == 0 ? colX[i] : colX[i] + 12f, cellY - rm.Ascent, rowFont, paint);
						cellY += rLineH + LineGap;
					}
					maxCellH = Math.Max(maxCellH, (rLineH + LineGap) * lines.Count - LineGap);
				}
				else
				{
					float tw = rowFont.MeasureText(cell);
					canvas.DrawText(cell, AlignX(alignments[i], colX[i], colW[i], tw), y - rm.Ascent, rowFont, paint);
				}
			}
			y += maxCellH + LineGap;
		}
		return y;
	}

	/// <summary>
	/// Draws "Total Qty" label+value aligned under a table column on the left,
	/// and a right-aligned two-column block (Sub Total, taxes, etc.) on the right.
	/// All labels share a right-edge column; all values share a right-edge column.
	/// </summary>
	public static float DrawTableTotals(SKCanvas canvas, float[] columnPercents, SKTextAlign[] alignments,
		int paperWidth, float y, string leftLabel, string columnValue, int columnIndex,
		(string Label, string Value) rightPair,
		List<(string Label, string Value)>? additionalRightRows = null,
		float fontSize = FontSizeNormal)
	{
		using var boldTf = BoldTypeface();
		using var semiTf = SemiBoldTypeface();
		using var labelFont = new SKFont(semiTf, fontSize);
		using var boldFont = new SKFont(boldTf, fontSize);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		var metrics = labelFont.Metrics;
		float lineH = metrics.Descent - metrics.Ascent;

		(var colX, var colW) = BuildColumns(columnPercents, paperWidth);

		// Left: label sits just before the column value
		float vw = boldFont.MeasureText(columnValue);
		float colValueX = AlignX(alignments[columnIndex], colX[columnIndex], colW[columnIndex], vw);
		canvas.DrawText(columnValue, colValueX, y - metrics.Ascent, boldFont, paint);
		float leftLabelW = labelFont.MeasureText(leftLabel);
		canvas.DrawText(leftLabel, Math.Max(colValueX - leftLabelW - 4, (float)Margin), y - metrics.Ascent, labelFont, paint);

		// Right: all rows aligned
		var allRows = new List<(string Label, string Value)> { rightPair };
		if (additionalRightRows is not null) allRows.AddRange(additionalRightRows);

		float rightEdge = paperWidth - Margin;
		float labelColRight = rightEdge - MaxWidth(boldFont, allRows.Select(r => r.Value)) - ColGap;

		foreach (var (lbl, val) in allRows)
		{
			DrawAlignedRightRow(canvas, lbl, val, labelColRight, rightEdge, y, metrics, labelFont, boldFont, paint);
			y += lineH + LineGap;
		}
		return y;
	}

	/// <summary>
	/// Draws a right-aligned block: labels share a right-edge column (semi-bold + colon),
	/// values share the paper right edge (bold).
	/// </summary>
	public static float DrawAlignedBlock(SKCanvas canvas, List<(string Label, string Value)> pairs,
		int paperWidth, float y, float fontSize = FontSizeNormal)
	{
		if (pairs is null || pairs.Count == 0) return y;

		using var boldTf = BoldTypeface();
		using var semiTf = SemiBoldTypeface();
		using var labelFont = new SKFont(semiTf, fontSize);
		using var boldFont = new SKFont(boldTf, fontSize);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		var metrics = labelFont.Metrics;
		float lineH = metrics.Descent - metrics.Ascent;
		float rightEdge = paperWidth - Margin;
		float labelColRight = rightEdge - MaxWidth(boldFont, pairs.Select(p => p.Value)) - ColGap;

		foreach (var (label, value) in pairs)
		{
			DrawAlignedRightRow(canvas, label, value, labelColRight, rightEdge, y, metrics, labelFont, boldFont, paint);
			y += lineH + LineGap;
		}
		return y;
	}

	/// <summary>Draws a single right-aligned label+value pair. Delegates to <see cref="DrawAlignedBlock"/>.</summary>
	public static float DrawRightLabelValue(SKCanvas canvas, string label, string value,
		int paperWidth, float y, float fontSize = FontSizeNormal)
		=> DrawAlignedBlock(canvas, [(label, value)], paperWidth, y, fontSize);

	// ── Private layout helpers ────────────────────────────────────────────────

	private static (float[] colX, float[] colW) BuildColumns(float[] columnPercents, int paperWidth)
	{
		float totalW = paperWidth - 2 * Margin;
		var colX = new float[columnPercents.Length];
		var colW = new float[columnPercents.Length];
		float x = Margin;
		for (int i = 0; i < columnPercents.Length; i++)
		{
			colX[i] = x;
			colW[i] = totalW * columnPercents[i];
			x += colW[i];
		}
		return (colX, colW);
	}

	private static float AlignX(SKTextAlign align, float colX, float colW, float textW) => align switch
	{
		SKTextAlign.Center => colX + (colW - textW) / 2,
		SKTextAlign.Right => colX + colW - textW,
		_ => colX
	};

	private static float MaxWidth(SKFont font, IEnumerable<string> texts)
	{
		float max = 0;
		foreach (var t in texts) { float w = font.MeasureText(t); if (w > max) max = w; }
		return max;
	}

	private static void DrawAlignedRightRow(SKCanvas canvas, string label, string value,
		float labelColRight, float rightEdge, float y, SKFontMetrics metrics,
		SKFont labelFont, SKFont boldFont, SKPaint paint)
	{
		string lbl = $"{label}:";
		canvas.DrawText(lbl, labelColRight - labelFont.MeasureText(lbl), y - metrics.Ascent, labelFont, paint);
		canvas.DrawText(value, rightEdge - boldFont.MeasureText(value), y - metrics.Ascent, boldFont, paint);
	}

	// ── Text utilities ───────────────────────────────────────────────────────

	/// <summary>Word-wraps text to fit within maxWidth using the given font.</summary>
	public static List<string> WrapText(string text, SKFont font, float maxWidth)
	{
		var lines = new List<string>();
		if (string.IsNullOrEmpty(text)) { lines.Add(string.Empty); return lines; }

		string currentLine = string.Empty;
		foreach (var word in text.Split(' '))
		{
			if (font.MeasureText(word) > maxWidth)
			{
				if (!string.IsNullOrEmpty(currentLine)) { lines.Add(currentLine); currentLine = string.Empty; }
				string chunk = string.Empty;
				foreach (char c in word)
				{
					string test = chunk + c;
					if (font.MeasureText(test) > maxWidth && chunk.Length > 0) { lines.Add(chunk); chunk = c.ToString(); }
					else chunk = test;
				}
				currentLine = chunk;
				continue;
			}
			string tryLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
			if (font.MeasureText(tryLine) > maxWidth && !string.IsNullOrEmpty(currentLine))
			{ lines.Add(currentLine); currentLine = word; }
			else currentLine = tryLine;
		}
		if (!string.IsNullOrEmpty(currentLine)) lines.Add(currentLine);
		if (lines.Count == 0) lines.Add(string.Empty);
		return lines;
	}

	// ── Bitmap / ESC-POS utilities ────────────────────────────────────────────

	/// <summary>Crops bitmap to the specified height.</summary>
	public static SKBitmap CropBitmap(SKBitmap source, int width, int height)
	{
		height = Math.Min(height, source.Height);
		var cropped = new SKBitmap(width, height);
		using var canvas = new SKCanvas(cropped);
		var rect = new SKRect(0, 0, width, height);
		canvas.DrawBitmap(source, rect, rect);
		return cropped;
	}

	/// <summary>Converts a bitmap to ESC/POS raster bytes (initialise + raster image + feed + cut).</summary>
	public static byte[] ConvertBitmapToThermalBytes(SKBitmap bitmap, int maxWidthDots)
	{
		using var ms = new MemoryStream();
		ms.Write([0x1B, 0x40]); // ESC @ — initialise

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image?.Encode(SKEncodedImageFormat.Png, 100);
		if (data is not null)
		{
			var raster = BuildRasterImage(data.ToArray(), maxWidthDots, center: true);
			if (raster is not null) ms.Write(raster);
		}

		ms.Write([0x1B, 0x64, 5]);    // ESC d 5 — feed 5 lines
		ms.Write([0x1D, 0x56, 0x01]); // GS V 1 — partial cut
		return ms.ToArray();
	}

	/// <summary>Wraps a bitmap into a Syncfusion PDF sized to thermal paper dimensions.</summary>
	public static MemoryStream WrapBitmapInPdf(SKBitmap bitmap, int paperWidthDots)
	{
		float pdfW = paperWidthDots / (float)PrinterDpi * 72f;
		float pdfH = bitmap.Height / (float)PrinterDpi * 72f;

		var ms = new MemoryStream();
		using var doc = new PdfDocument();
		doc.PageSettings.Size = new SizeF(pdfW, pdfH);
		doc.PageSettings.Margins.All = 0;

		var page = doc.Pages.Add();
		using var encoded = SKImage.FromBitmap(bitmap)?.Encode(SKEncodedImageFormat.Png, 100);
		if (encoded is not null)
		{
			using var imgStream = new MemoryStream(encoded.ToArray());
			page.Graphics.DrawImage(new PdfBitmap(imgStream), 0, 0, pdfW, pdfH);
		}
		doc.Save(ms);
		ms.Position = 0;
		return ms;
	}

	/// <summary>Converts raw image bytes to ESC/POS raster bytes (GS v 0 command).</summary>
	public static byte[] BuildRasterImage(byte[] imageBytes, int maxWidthDots = PaperDots80mm,
		int threshold = 128, bool center = true)
	{
		try
		{
			using var original = SKBitmap.Decode(imageBytes);
			if (original is null) return null;

			float scale = Math.Min((float)maxWidthDots / original.Width, 1f);
			int newWidth = ((int)(original.Width * scale) + 7) / 8 * 8;
			int newHeight = (int)(original.Height * scale);

			using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
			if (resized is null) return null;

			int widthBytes = newWidth / 8;
			var rasterData = new byte[widthBytes * newHeight];
			for (int y = 0; y < newHeight; y++)
			{
				for (int x = 0; x < newWidth; x++)
				{
					var px = resized.GetPixel(x, y);
					float alpha = px.Alpha / 255f;
					float lum = (0.299f * px.Red + 0.587f * px.Green + 0.114f * px.Blue) * alpha + 255f * (1f - alpha);
					if (lum < threshold)
						rasterData[y * widthBytes + x / 8] |= (byte)(1 << (7 - x % 8));
				}
			}

			using var ms = new MemoryStream();
			if (center) ms.Write([0x1B, 0x61, 0x01]); // ESC a 1 — centre align
			byte xL = (byte)(widthBytes & 0xFF), xH = (byte)(widthBytes >> 8);
			byte yL = (byte)(newHeight & 0xFF), yH = (byte)(newHeight >> 8);
			ms.Write([0x1D, 0x76, 0x30, 0x00, xL, xH, yL, yH]);
			ms.Write(rasterData);
			return ms.ToArray();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Raster image conversion failed: {ex.Message}");
			return null;
		}
	}

	// Kept public for external callers
	public static void Initialize(MemoryStream ms) => ms.Write([0x1B, 0x40]);
	public static void FeedAndCut(MemoryStream ms, byte feedLines = 5)
	{
		ms.Write([0x1B, 0x64, feedLines]);
		ms.Write([0x1D, 0x56, 0x01]);
	}
}
