using PrimeBakes.Data.Common;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Api.Common;

public class GenerateCodesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(GenerateCodesEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(GenerateCodes.GenerateAccountingTransactionNo),
			(FinancialAccountingModel accounting) => GenerateCodes.GenerateAccountingTransactionNo(accounting));

		group.MapPost(nameof(GenerateCodes.GeneratePurchaseTransactionNo),
			(PurchaseModel purchase) => GenerateCodes.GeneratePurchaseTransactionNo(purchase));

		group.MapPost(nameof(GenerateCodes.GeneratePurchaseReturnTransactionNo),
			(PurchaseReturnModel purchaseReturn) => GenerateCodes.GeneratePurchaseReturnTransactionNo(purchaseReturn));

		group.MapPost(nameof(GenerateCodes.GenerateKitchenIssueTransactionNo),
			(KitchenIssueModel kitchenIssue) => GenerateCodes.GenerateKitchenIssueTransactionNo(kitchenIssue));

		group.MapPost(nameof(GenerateCodes.GenerateKitchenIssueReturnTransactionNo),
			(KitchenIssueReturnModel kitchenIssueReturn) => GenerateCodes.GenerateKitchenIssueReturnTransactionNo(kitchenIssueReturn));

		group.MapPost(nameof(GenerateCodes.GenerateKitchenProductionTransactionNo),
			(KitchenProductionModel kitchenProduction) => GenerateCodes.GenerateKitchenProductionTransactionNo(kitchenProduction));

		group.MapPost(nameof(GenerateCodes.GenerateKitchenProductionReturnTransactionNo),
			(KitchenProductionReturnModel kitchenProductionReturn) => GenerateCodes.GenerateKitchenProductionReturnTransactionNo(kitchenProductionReturn));

		group.MapPost(nameof(GenerateCodes.GenerateProductStockAdjustmentTransactionNo),
			(DateTime transactionDateTime, int locationId) => GenerateCodes.GenerateProductStockAdjustmentTransactionNo(transactionDateTime, locationId));

		group.MapPost(nameof(GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo),
			(DateTime transactionDateTime) => GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(transactionDateTime));

		group.MapPost(nameof(GenerateCodes.GenerateOrderTransactionNo),
			(OrderModel order) => GenerateCodes.GenerateOrderTransactionNo(order));

		group.MapPost(nameof(GenerateCodes.GenerateSaleTransactionNo),
			(SaleModel sale) => GenerateCodes.GenerateSaleTransactionNo(sale));

		group.MapPost(nameof(GenerateCodes.GenerateSaleReturnTransactionNo),
			(SaleReturnModel saleReturn) => GenerateCodes.GenerateSaleReturnTransactionNo(saleReturn));

		group.MapPost(nameof(GenerateCodes.GenerateStockTransferTransactionNo),
			(StockTransferModel stockTransfer) => GenerateCodes.GenerateStockTransferTransactionNo(stockTransfer));

		group.MapPost(nameof(GenerateCodes.GenerateBillTransactionNo),
			(BillModel bill) => GenerateCodes.GenerateBillTransactionNo(bill));
	}
}
