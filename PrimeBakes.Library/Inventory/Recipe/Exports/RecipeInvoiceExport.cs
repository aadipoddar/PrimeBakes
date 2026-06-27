using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.Recipe.Models;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Inventory.Recipe.Exports;

public static class RecipeInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType, DateTime? costAsOnDateTime = null)
	{
		var transaction = await CommonData.LoadTableDataById<RecipeModel>(InventoryNames.Recipe, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<RecipeItemOverviewModel>(InventoryNames.RecipeItemOverview, transaction.Id);
		transactionDetails = [.. transactionDetails.OrderBy(detail => detail.ItemName)];
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		costAsOnDateTime ??= await CommonData.LoadCurrentDateTime();

		var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, transaction.ProductId);

		var lineItems = transactionDetails.Select(detail => new
		{
			detail.ItemName,
			detail.Quantity,
			detail.Rate,
			detail.Amount,
			detail.PerUnit
		}).ToList();

		var invoiceData = new InvoiceData
		{
			Company = new() { Name = product.Name, Address = $"Quantity: {transaction.Quantity} | Effective: {transaction.FromDate:dd-MMM-yyyy}" },
			InvoiceType = "Recipe",
			TransactionDateTime = costAsOnDateTime.Value,
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
			new(nameof(RecipeItemOverviewModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 30),
			new(nameof(RecipeItemOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 40, 10, "#,##0.00"),
			new(nameof(RecipeItemOverviewModel.Rate), "Rate", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(RecipeItemOverviewModel.Amount), "Amount", exportType, CellAlignment.Right, 55, 15, "#,##0.00"),
			new(nameof(RecipeItemOverviewModel.PerUnit), "Per Unit", exportType, CellAlignment.Right, 55, 15, "#,##0.00")
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
