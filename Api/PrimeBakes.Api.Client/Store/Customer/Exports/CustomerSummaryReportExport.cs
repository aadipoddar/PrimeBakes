using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Library.Store.Customer.Exports;

public static class CustomerSummaryReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(CustomerSummaryReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<CustomerSummaryModel> customerSummaryData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		CompanyModel company = null,
		LocationModel location = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new CustomerSummaryReportRequest(customerSummaryData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company, location));
}
