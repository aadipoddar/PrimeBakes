using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Recipe;

namespace PrimeBakes.Exports.Inventory.Recipe;

public static class RecipeInvoiceExport
{
	public static (MemoryStream stream, string fileName) ExportInvoice(RecipeInvoiceBundle bundle, InvoiceExportType exportType, DateTime? costAsOnDateTime = null)
	{
		var (transaction, transactionDetails, product, currentDateTime) = bundle;
		costAsOnDateTime ??= currentDateTime;
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
			CurrentDateTime = currentDateTime,
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

		string fileName = $"RECIPE_{product.Name}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = PDFInvoiceExportUtil.ExportInvoiceToPdf(
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
			var stream = ExcelInvoiceExportUtil.ExportInvoiceToExcel(
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
