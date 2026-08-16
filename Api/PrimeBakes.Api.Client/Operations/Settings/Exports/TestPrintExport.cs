using PrimeBakes.Models.Common;

namespace PrimeBakes.Library.Operations.Settings.Exports;

public static class TestPrintExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(TestPrintExport));

	public static async Task<byte[]> GenerateTestReceipt(string printerName, string printerAddress, string platform) =>
		await ApiClient.Get<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateTestReceipt)), new { printerName, printerAddress, platform });

	public static async Task<byte[]> GenerateTestReceiptPng(string printerName, string printerAddress, string platform) =>
		await ApiClient.Get<byte[]>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateTestReceiptPng)), new { printerName, printerAddress, platform });
}
