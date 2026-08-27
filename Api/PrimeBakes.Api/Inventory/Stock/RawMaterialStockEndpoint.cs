using PrimeBakes.Data.Inventory.Stock;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Api.Inventory.Stock;

public class RawMaterialStockEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialStockEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialStockData.InsertRawMaterialStock),
			(RawMaterialStockModel stock) => RawMaterialStockData.InsertRawMaterialStock(stock));

		group.MapPost(nameof(RawMaterialStockData.DeleteRawMaterialStockByTransactionNo),
			(string TransactionNo) => RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(TransactionNo));

		group.MapGet(nameof(RawMaterialStockData.LoadRawMaterialOpeningStockByDate),
			(DateTime FromDate) => RawMaterialStockData.LoadRawMaterialOpeningStockByDate(FromDate));

		group.MapGet(nameof(RawMaterialStockData.LoadRawMaterialStockSummaryByDate),
			(DateTime FromDate, DateTime ToDate) => RawMaterialStockData.LoadRawMaterialStockSummaryByDate(FromDate, ToDate));

		group.MapPost(nameof(RawMaterialStockData.DeleteRawMaterialStockAdjustment),
			(int id, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				RawMaterialStockData.DeleteRawMaterialStockAdjustment(id, userId, formFactor, platform, latitude, longitude));

		group.MapPost(nameof(RawMaterialStockData.RecalculateStockByDate),
			(DateTime fromDate, DateTime toDate, bool deleteAdjustments, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				RawMaterialStockData.RecalculateStockByDate(fromDate, toDate, deleteAdjustments, userId, formFactor, platform, latitude, longitude));

		group.MapPost(nameof(RawMaterialStockData.SaveRawMaterialStockAdjustment),
			(RawMaterialStockAdjustmentRequest request, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				RawMaterialStockData.SaveRawMaterialStockAdjustment(request.TransactionDateTime, request.Cart, userId, formFactor, platform, latitude, longitude));
	}
}
