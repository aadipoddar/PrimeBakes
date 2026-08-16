using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.FinancialAccounting.Exports;

public static class FinancialAccountingInvoiceExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(FinancialAccountingInvoiceExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportInvoice)), new { }, new { transactionId, exportType });
}
