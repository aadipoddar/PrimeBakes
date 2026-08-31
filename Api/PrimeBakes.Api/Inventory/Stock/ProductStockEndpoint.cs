using PrimeBakes.Data.Inventory.Stock;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Api.Inventory.Stock;

public class ProductStockEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductStockEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductStockData.DeleteProductStockByTransactionNo),
			(string TransactionNo) => ProductStockData.DeleteProductStockByTransactionNo(TransactionNo));

		group.MapGet(nameof(ProductStockData.LoadProductOpeningStockByDateLocationId),
			(DateTime FromDate, int LocationId) => ProductStockData.LoadProductOpeningStockByDateLocationId(FromDate, LocationId));

		group.MapGet(nameof(ProductStockData.LoadProductStockSummaryByDateLocationId),
			(DateTime FromDate, DateTime ToDate, int LocationId) => ProductStockData.LoadProductStockSummaryByDateLocationId(FromDate, ToDate, LocationId));

		group.MapPost(nameof(ProductStockData.DeleteProductStockAdjustment),
			(int id, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				ProductStockData.DeleteProductStockAdjustment(id, userId, formFactor, platform, latitude, longitude));

		group.MapPost(nameof(ProductStockData.RecalculateStockByDateLocation),
			(DateTime fromDate, DateTime toDate, int locationId, bool deleteAdjustments, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				ProductStockData.RecalculateStockByDateLocation(fromDate, toDate, locationId, deleteAdjustments, userId, formFactor, platform, latitude, longitude));

		group.MapPost(nameof(ProductStockData.SaveProductStockAdjustment),
			(ProductStockAdjustmentRequest request, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				ProductStockData.SaveProductStockAdjustment(request.TransactionDateTime, request.LocationId, request.Cart, userId, formFactor, platform, latitude, longitude));
	}
}
