using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Data.Common;

public static class GenerateCodes
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(GenerateCodes));

	public static async Task<string> GenerateAccountingTransactionNo(FinancialAccountingModel accounting) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateAccountingTransactionNo)), accounting);

	public static async Task<string> GeneratePurchaseTransactionNo(PurchaseModel purchase) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GeneratePurchaseTransactionNo)), purchase);

	public static async Task<string> GeneratePurchaseReturnTransactionNo(PurchaseReturnModel purchaseReturn) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GeneratePurchaseReturnTransactionNo)), purchaseReturn);

	public static async Task<string> GeneratePurchaseOrderTransactionNo(PurchaseOrderModel purchaseOrder) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GeneratePurchaseOrderTransactionNo)), purchaseOrder);

	public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel kitchenIssue) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateKitchenIssueTransactionNo)), kitchenIssue);

	public static async Task<string> GenerateKitchenIssueReturnTransactionNo(KitchenIssueReturnModel kitchenIssueReturn) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateKitchenIssueReturnTransactionNo)), kitchenIssueReturn);

	public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel kitchenProduction) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateKitchenProductionTransactionNo)), kitchenProduction);

	public static async Task<string> GenerateKitchenProductionReturnTransactionNo(KitchenProductionReturnModel kitchenProductionReturn) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateKitchenProductionReturnTransactionNo)), kitchenProductionReturn);

	public static async Task<string> GenerateProductStockAdjustmentTransactionNo(DateTime transactionDateTime, int locationId) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateProductStockAdjustmentTransactionNo)), new { }, new { transactionDateTime, locationId });

	public static async Task<string> GenerateRawMaterialStockAdjustmentTransactionNo(DateTime transactionDateTime) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateRawMaterialStockAdjustmentTransactionNo)), new { }, new { transactionDateTime });

	public static async Task<string> GenerateOrderTransactionNo(OrderModel order) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateOrderTransactionNo)), order);

	public static async Task<string> GenerateSaleTransactionNo(SaleModel sale) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateSaleTransactionNo)), sale);

	public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel saleReturn) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateSaleReturnTransactionNo)), saleReturn);

	public static async Task<string> GenerateStockTransferTransactionNo(StockTransferModel stockTransfer) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateStockTransferTransactionNo)), stockTransfer);

	public static async Task<string> GenerateBillTransactionNo(BillModel bill) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GenerateBillTransactionNo)), bill);
}
