using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;

namespace PrimeBakes.Exports.Restaurant.Menu;

public static class MenuQRExport
{
	private const string _logoResourceName = "PrimeBakes.Exports.Utils.Resources.logo.png";

	// Builds a QR code (with the company logo) linking to the public guest menu for the given location.
	public static (MemoryStream stream, string fileName) ExportMenuQRCode(int locationId, string locationName, DateTime timestamp)
	{
		var menuUrl = $"{CommonSecrets.AppWebsite}{RestaurantRouteNames.Menu}/{locationId}";

		using var logo = new MemoryStream();
		using (var resource = typeof(MenuQRExport).Assembly.GetManifestResourceStream(_logoResourceName))
			resource?.CopyTo(logo);

		var png = QRCodeExportUtil.CreateQrCodeWithLogo(menuUrl, logo.ToArray());

		var safeName = string.Concat((locationName ?? "").Split(Path.GetInvalidFileNameChars()));
		var fileName = $"Menu_QR_{safeName}_{timestamp:yyyyMMdd_HHmmss}.png";

		return (new MemoryStream(png), fileName);
	}
}
