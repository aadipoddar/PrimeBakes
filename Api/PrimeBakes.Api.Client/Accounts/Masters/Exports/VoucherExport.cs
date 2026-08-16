using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.Masters.Exports;

public static class VoucherExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(VoucherExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<VoucherModel> voucherData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), voucherData, new { exportType });
}
