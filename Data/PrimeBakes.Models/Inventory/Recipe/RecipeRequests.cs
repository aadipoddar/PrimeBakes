using PrimeBakes.Models.Exports;

namespace PrimeBakes.Models.Inventory.Recipe;

public sealed record RecipeSaveRequest(
	RecipeModel Recipe,
	List<RecipeDetailModel> Details);

public sealed record RecipeReportRequest(
	IEnumerable<RecipeOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? EffectiveDate,
	DateOnly? CostAsOnDate,
	string Deduct);

public sealed record RecipeItemReportRequest(
	IEnumerable<RecipeItemOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? EffectiveDate,
	DateOnly? CostAsOnDate,
	string Deduct,
	string RawMaterial,
	string Category,
	string Product);
