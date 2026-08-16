using PrimeBakes.Library.Inventory.Stock.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Api.Inventory.Stock.Data;

public class RawMaterialStockDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialStockDataEndpoint));
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
			(int id, int userId, string platform) => RawMaterialStockData.DeleteRawMaterialStockAdjustment(id, userId, platform));

		group.MapPost(nameof(RawMaterialStockData.RecalculateStockByDate),
			(DateTime fromDate, DateTime toDate, bool deleteAdjustments, int userId, string platform) =>
				RawMaterialStockData.RecalculateStockByDate(fromDate, toDate, deleteAdjustments, userId, platform));

		group.MapPost(nameof(RawMaterialStockData.SaveRawMaterialStockAdjustment),
			(RawMaterialStockAdjustmentRequest request, int userId, string platform) =>
				RawMaterialStockData.SaveRawMaterialStockAdjustment(request.TransactionDateTime, request.Cart, userId, platform));
	}
}
