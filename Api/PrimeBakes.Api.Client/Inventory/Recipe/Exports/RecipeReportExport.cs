using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Recipe;

namespace PrimeBakes.Library.Inventory.Recipe.Exports;

public static class RecipeReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RecipeReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<RecipeOverviewModel> data,
		ReportExportType exportType,
		DateOnly? effectiveDate = null,
		DateOnly? costAsOnDate = null,
		string deduct = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new RecipeReportRequest(data, exportType, effectiveDate, costAsOnDate, deduct));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<RecipeItemOverviewModel> data,
		ReportExportType exportType,
		DateOnly? effectiveDate = null,
		DateOnly? costAsOnDate = null,
		string deduct = null,
		string rawMaterial = null,
		string category = null,
		string product = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportItemReport)),
			new RecipeItemReportRequest(data, exportType, effectiveDate, costAsOnDate, deduct, rawMaterial, category, product));
}
