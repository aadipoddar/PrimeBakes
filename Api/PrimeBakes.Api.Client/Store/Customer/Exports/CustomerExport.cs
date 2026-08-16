using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Library.Store.Customer.Exports;

public static class CustomerExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(CustomerExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<CustomerModel> customerData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), customerData, new { exportType });
}
