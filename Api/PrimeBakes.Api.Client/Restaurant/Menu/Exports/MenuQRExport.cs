using PrimeBakes.Models.Common;

namespace PrimeBakes.Library.Restaurant.Menu.Exports;

public static class MenuQRExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(MenuQRExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMenuQRCode(int locationId, string locationName) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMenuQRCode)), new { }, new { locationId, locationName });
}
