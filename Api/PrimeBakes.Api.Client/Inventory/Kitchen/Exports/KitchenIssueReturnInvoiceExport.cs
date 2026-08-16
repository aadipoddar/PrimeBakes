using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenIssueReturnInvoiceExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReturnInvoiceExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportInvoice)), new { }, new { transactionId, exportType });
}
