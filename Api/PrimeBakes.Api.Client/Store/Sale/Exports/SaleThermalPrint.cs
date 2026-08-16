using PrimeBakes.Models.Common;

namespace PrimeBakes.Library.Store.Sale.Exports;

public static class SaleThermalPrint
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(SaleThermalPrint));

	public static async Task<byte[]> GenerateThermalBill(int saleId) =>
		await ApiClient.Get<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateThermalBill)), new { saleId });

	public static async Task<byte[]> GenerateThermalBillPng(int saleId) =>
		await ApiClient.Get<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateThermalBillPng)), new { saleId });
}
