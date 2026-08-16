using QRCoder;

using SkiaSharp;

namespace PrimeBakes.Library.Utils.Exports;

public static class QRCodeExportUtil
{
	// Generates a "funky" QR code (round dot modules, circular finder eyes, brand colors) with a
	// centered logo, as PNG bytes. ECC level H (~30% recovery) keeps it scannable under the logo.
	public static byte[] CreateQrCodeWithLogo(string data, byte[] logo, int px = 20)
	{
		var dark = SKColor.Parse("#5A4632");   // warm brown modules
		var light = SKColor.Parse("#F7F2EA");  // cream background

		using var qrData = new QRCodeGenerator().CreateQrCode(data, QRCodeGenerator.ECCLevel.H);
		var matrix = qrData.ModuleMatrix;
		var count = matrix.Count;              // includes the 4-module quiet zone each side
		var size = count * px;

		// The three 7x7 finder patterns (top-left, top-right, bottom-left), in module coords.
		(int row, int col)[] finders = [(4, 4), (4, count - 11), (count - 11, 4)];
		bool InFinder(int y, int x) => finders.Any(f => y >= f.row && y < f.row + 7 && x >= f.col && x < f.col + 7);

		// Center area kept clear for the logo (a touch larger than the drawn logo).
		var hasLogo = logo is { Length: > 0 };
		var logoSide = size * 0.24f;
		var pos = (size - logoSide) / 2;
		var logoRect = new SKRect(pos, pos, pos + logoSide, pos + logoSide);
		var clear = new SKRect(pos - px, pos - px, pos + logoSide + px, pos + logoSide + px);

		using var surface = SKSurface.Create(new SKImageInfo(size, size));
		var c = surface.Canvas;
		c.Clear(light);
		using var fill = new SKPaint { Color = dark, IsAntialias = true };

		// Data modules as round dots.
		for (var y = 0; y < count; y++)
			for (var x = 0; x < count; x++)
			{
				if (!matrix[y][x] || InFinder(y, x))
					continue;

				float cx = x * px + px / 2f, cy = y * px + px / 2f;
				if (hasLogo && clear.Contains(cx, cy))
					continue;

				c.DrawCircle(cx, cy, px * 0.46f, fill);
			}

		// Finder eyes: outer ring + filled center dot.
		using var ring = new SKPaint { Color = dark, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = px };
		foreach (var (row, col) in finders)
		{
			float cx = (col + 3.5f) * px, cy = (row + 3.5f) * px;
			c.DrawCircle(cx, cy, 3f * px, ring);
			c.DrawCircle(cx, cy, 1.5f * px, fill);
		}

		// Centered logo.
		if (hasLogo && SKImage.FromEncodedData(logo) is { } image)
			using (image)
				c.DrawImage(image, logoRect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

		using var snapshot = surface.Snapshot();
		using var encoded = snapshot.Encode(SKEncodedImageFormat.Png, 100);
		return encoded.ToArray();
	}
}
