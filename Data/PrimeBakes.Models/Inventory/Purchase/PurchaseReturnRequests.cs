using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Models.Inventory.Purchase;

public sealed record PurchaseReturnSaveRequest(
	PurchaseReturnModel PurchaseReturn,
	List<PurchaseReturnDetailModel> Details,
	bool Recover);

public sealed record PurchaseReturnReportRequest(
	IEnumerable<PurchaseReturnOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	LedgerModel Party,
	CompanyModel Company);

public sealed record PurchaseReturnItemReportRequest(
	IEnumerable<PurchaseReturnItemOverviewModel> Data,
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
