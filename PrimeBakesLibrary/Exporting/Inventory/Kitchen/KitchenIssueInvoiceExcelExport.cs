using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenIssueInvoiceExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
		var kitchen = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.KitchenId);
		if (company is null || kitchen is null)
			throw new InvalidOperationException("Company or kitchen information is missing.");

		var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

		var cartItems = transactionDetails.Select(detail =>
		{
			var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
			return new KitchenIssueItemCartModel
			{
				ItemId = detail.RawMaterialId,
				ItemName = item?.Name ?? $"Item #{detail.RawMaterialId}",
				Quantity = detail.Quantity,
				UnitOfMeasurement = detail.UnitOfMeasurement,
				Rate = detail.Rate,
				Total = detail.Total,
				Remarks = detail.Remarks
			};
		}).ToList();

		// Convert LocationModel to LedgerModel for display
		var kitchenAsLedger = new LedgerModel
		{
			Name = kitchen.Name,
		};

		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			Company = company,
			BillTo = kitchenAsLedger,
			InvoiceType = "KITCHEN ISSUE INVOICE",
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			TotalAmount = transaction.TotalAmount,
			Remarks = transaction.Remarks,
			Status = transaction.Status,
			PaymentModes = null
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
		};

		var columnSettings = new List<ExcelInvoiceExportUtil.InvoiceColumnSetting>
		{
			new("#", "#", 5, Syncfusion.XlsIO.ExcelHAlign.HAlignCenter),
			new(nameof(KitchenIssueItemCartModel.ItemName), "Item", 35, Syncfusion.XlsIO.ExcelHAlign.HAlignLeft),
			new(nameof(KitchenIssueItemCartModel.UnitOfMeasurement), "UOM", 10, Syncfusion.XlsIO.ExcelHAlign.HAlignCenter),
			new(nameof(KitchenIssueItemCartModel.Quantity), "Qty", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
			new(nameof(KitchenIssueItemCartModel.Rate), "Rate", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
			new(nameof(KitchenIssueItemCartModel.Total), "Total", 15, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00")
		};

		var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			cartItems,
			columnSettings,
			null,
			summaryFields
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"KITCHEN_ISSUE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}
