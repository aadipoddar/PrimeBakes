using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Library.Store.StockTransfer.Data;

public static class StockTransferData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(StockTransferData));

	public static async Task DeleteTransaction(StockTransferModel stockTransfer) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), stockTransfer);

	public static async Task RecoverTransaction(StockTransferModel stockTransfer) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), stockTransfer);

	public static async Task<int> SaveTransaction(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new StockTransferSaveRequest(stockTransfer, stockTransferDetails, recover));
}
