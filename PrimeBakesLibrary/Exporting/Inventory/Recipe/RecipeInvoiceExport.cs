using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Masters;
using PrimeBakesLibrary.Models.Store.Order;
using PrimeBakesLibrary.Models.Store.Product;
using PrimeBakesLibrary.Models.Store.Sale;

namespace PrimeBakesLibrary.Exporting.Inventory.Recipe;

public static class RecipeInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType, DateTime? recipeDateTime = null)
	{
		var transaction = await CommonData.LoadTableDataById<RecipeModel>(TableNames.Recipe, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(TableNames.RecipeDetail, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		recipeDateTime ??= await CommonData.LoadCurrentDateTime();

		var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, transaction.ProductId);
		var rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, recipeDateTime.Value);

		var lineItems = transactionDetails.Select(detail =>
		{
			var rawMaterial = rawMaterials.FirstOrDefault(r => r.Id == detail.RawMaterialId);
			var amount = detail.Quantity * (rawMaterial?.Rate ?? 0);
			return new
			{
				ItemId = detail.RawMaterialId,
				ItemName = rawMaterial?.Name ?? $"Raw Material #{detail.RawMaterialId}",
				detail.Quantity,
				Rate = rawMaterial?.Rate ?? 0,
				Amount = amount,
				PerUnit = transaction.Quantity > 0 ? amount / transaction.Quantity : 0m
			};
		}).ToList();

		var invoiceData = new InvoiceData
		{
			Company = new() { Name = product.Name, Address = $"Quantity: {transaction.Quantity}" },
			InvoiceType = "Recipe",
			TransactionDateTime = recipeDateTime.Value,
			TotalAmount = lineItems.Sum(i => i.Amount),
			Status = transaction.Status
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Quantity"] = lineItems.Count.ToString(),
			["Per Unit Total"] = lineItems.Sum(i => i.PerUnit).FormatIndianCurrency(),
			["Grand Total"] = lineItems.Sum(i => i.Amount).FormatIndianCurrency()
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(RecipeItemCartModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 30),
			new(nameof(RecipeItemCartModel.Quantity), "Qty", exportType, CellAlignment.Right, 40, 10, "#,##0.00"),
			new(nameof(RecipeItemCartModel.Rate), "Rate", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(RecipeItemCartModel.Amount), "Amount", exportType, CellAlignment.Right, 55, 15, "#,##0.00"),
			new("PerUnit", "Per Unit", exportType, CellAlignment.Right, 55, 15, "#,##0.00")
		};

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"RECIPE_{product.Name}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
				invoiceData,
				lineItems,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
				invoiceData,
				lineItems,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
