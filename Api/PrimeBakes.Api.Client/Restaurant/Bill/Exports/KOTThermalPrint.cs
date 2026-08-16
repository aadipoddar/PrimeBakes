using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Bill;

namespace PrimeBakes.Library.Restaurant.Bill.Exports;

public static class KOTThermalPrint
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KOTThermalPrint));

	public static async Task<byte[]> GenerateThermalBill(int billId, int kotCategoryId, List<BillItemCartModel> kotItems) =>
		await ApiClient.Post<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateThermalBill)),
			new KOTThermalRequest(billId, kotCategoryId, kotItems));

	public static async Task<byte[]> GenerateThermalBillPng(int billId, int kotCategoryId, List<BillItemCartModel> kotItems) =>
		await ApiClient.Post<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateThermalBillPng)),
			new KOTThermalRequest(billId, kotCategoryId, kotItems));
}
