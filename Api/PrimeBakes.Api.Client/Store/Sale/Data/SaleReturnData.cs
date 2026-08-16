using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Library.Store.Sale.Data;

public static class SaleReturnData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(SaleReturnData));

	public static async Task DeleteTransaction(SaleReturnModel saleReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), saleReturn);

	public static async Task RecoverTransaction(SaleReturnModel saleReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), saleReturn);

	public static async Task<int> SaveTransaction(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, CustomerModel customer = null, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new SaleReturnSaveRequest(saleReturn, saleReturnDetails, customer, recover));
}
