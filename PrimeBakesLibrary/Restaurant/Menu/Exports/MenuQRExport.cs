using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Restaurant.Menu.Exports;

public static class MenuQRExport
{
	private const string _logoResourceName = "PrimeBakesLibrary.Utils.Resources.logo.png";

	// Builds a QR code (with the company logo) linking to the public guest menu for the given location.
	public static async Task<(MemoryStream stream, string fileName)> ExportMenuQRCode(int locationId, string locationName)
	{
		var menuUrl = $"{Secrets.AppWebsite}{RestaurantRouteNames.Menu}/{locationId}";

		using var logo = new MemoryStream();
		using (var resource = typeof(MenuQRExport).Assembly.GetManifestResourceStream(_logoResourceName))
			resource?.CopyTo(logo);

		var png = QRCodeExportUtil.CreateQrCodeWithLogo(menuUrl, logo.ToArray());

		var timestamp = await CommonData.LoadCurrentDateTime();
		var safeName = string.Concat((locationName ?? "").Split(Path.GetInvalidFileNameChars()));
		var fileName = $"Menu_QR_{safeName}_{timestamp:yyyyMMdd_HHmmss}.png";

		return (new MemoryStream(png), fileName);
	}
}
