using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Models.Store.Customer;

public sealed record CustomerSummaryReportRequest(
	IEnumerable<CustomerSummaryModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	CompanyModel Company,
	LocationModel Location);
