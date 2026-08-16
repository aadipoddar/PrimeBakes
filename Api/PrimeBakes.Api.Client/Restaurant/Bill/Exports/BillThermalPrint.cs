using PrimeBakes.Models.Common;

namespace PrimeBakes.Library.Restaurant.Bill.Exports;

public static class BillThermalPrint
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(BillThermalPrint));

	public static async Task<byte[]> GenerateThermalBill(int billId) =>
		await ApiClient.Get<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateThermalBill)), new { billId });

	public static async Task<byte[]> GenerateThermalBillPng(int billId) =>
		await ApiClient.Get<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateThermalBillPng)), new { billId });
}
