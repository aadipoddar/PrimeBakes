using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Inventory.Recipe.Exports;

public static class RecipeInvoiceExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RecipeInvoiceExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType, DateTime? costAsOnDateTime = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportInvoice)), new { }, new { transactionId, exportType, costAsOnDateTime });
}
