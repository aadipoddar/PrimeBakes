using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Models.Inventory.Purchase;

public sealed record PurchaseSaveRequest(
	PurchaseModel Purchase,
	List<PurchaseDetailModel> Details,
	bool Recover);

public sealed record PurchaseReportRequest(
	IEnumerable<PurchaseOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	LedgerModel Party,
	CompanyModel Company);

public sealed record PurchaseItemReportRequest(
	IEnumerable<PurchaseItemOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	RawMaterialModel RawMaterial,
	RawMaterialCategoryModel RawMaterialCategory,
	CompanyModel Company,
	LedgerModel Party);
