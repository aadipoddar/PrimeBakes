using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Inventory.RawMaterial;
using PrimeBakesLibrary.Exporting.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Store.Order;
using PrimeBakesLibrary.Exporting.Store.Product;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Store.StockTransfer;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Store.Order;
using PrimeBakesLibrary.Models.Store.Product;
using PrimeBakesLibrary.Models.Store.Sale;
using PrimeBakesLibrary.Models.Store.StockTransfer;

namespace PrimeBakesLibrary.Data.Common;

public static class DecodeCode
{
	public static async Task<DecodeTransactionNoModel> DecodeTransactionNo(string transactionNo, CodeType? codeType = null)
	{
		if (string.IsNullOrWhiteSpace(transactionNo))
			return null;

		DecodeTransactionNoModel decodeTransactionNoModel = new();

		if (codeType is null)
			decodeTransactionNoModel = await DecodeTransactionType(transactionNo);
		else
			decodeTransactionNoModel.CodeType = codeType.Value;

		switch (decodeTransactionNoModel.CodeType)
		{
			case CodeType.Accounting:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<FinancialAccountingModel>(TableNames.FinancialAccounting, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.FinancialAccounting}/{(decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id}";
				decodeTransactionNoModel.PDFStream = await FinancialAccountingInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await FinancialAccountingInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.Ledger:
				var ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<LedgerModel>(TableNames.Ledger, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.LedgerMaster}";
				decodeTransactionNoModel.PDFStream = await LedgerExport.ExportMaster(ledgers, ReportExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await LedgerExport.ExportMaster(ledgers, ReportExportType.Excel);
				break;
			case CodeType.Purchase:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<PurchaseModel>(TableNames.Purchase, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.Purchase}/{(decodeTransactionNoModel.TransactionModel as PurchaseModel).Id}";
				decodeTransactionNoModel.PDFStream = await PurchaseInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await PurchaseInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.PurchaseReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<PurchaseReturnModel>(TableNames.PurchaseReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.PurchaseReturn}/{(decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id}";
				decodeTransactionNoModel.PDFStream = await PurchaseReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await PurchaseReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.KitchenIssue:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenIssueModel>(TableNames.KitchenIssue, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.KitchenIssue}/{(decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id}";
				decodeTransactionNoModel.PDFStream = await KitchenIssueInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await KitchenIssueInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.KitchenProduction:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenProductionModel>(TableNames.KitchenProduction, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.KitchenProduction}/{(decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id}";
				decodeTransactionNoModel.PDFStream = await KitchenProductionInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await KitchenProductionInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.RawMaterial:
				var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<RawMaterialModel>(TableNames.RawMaterial, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.RawMaterial}";
				decodeTransactionNoModel.PDFStream = await RawMaterialExport.ExportMaster(rawMaterials, ReportExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await RawMaterialExport.ExportMaster(rawMaterials, ReportExportType.Excel);
				break;
			case CodeType.FinishedProduct:
				var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<ProductModel>(TableNames.Product, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.Product}";
				decodeTransactionNoModel.PDFStream = await ProductExport.ExportMaster(products, ReportExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await ProductExport.ExportMaster(products, ReportExportType.Excel);
				break;
			case CodeType.Order:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<OrderModel>(TableNames.Order, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.Order}/{(decodeTransactionNoModel.TransactionModel as OrderModel).Id}";
				decodeTransactionNoModel.PDFStream = await OrderInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as OrderModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await OrderInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as OrderModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.Sale:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<SaleModel>(TableNames.Sale, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.Sale}/{(decodeTransactionNoModel.TransactionModel as SaleModel).Id}";
				decodeTransactionNoModel.PDFStream = await SaleInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await SaleInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.SaleReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<SaleReturnModel>(TableNames.SaleReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.SaleReturn}/{(decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id}";
				decodeTransactionNoModel.PDFStream = await SaleReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await SaleReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.StockTransfer:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<StockTransferModel>(TableNames.StockTransfer, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.StockTransfer}/{(decodeTransactionNoModel.TransactionModel as StockTransferModel).Id}";
				decodeTransactionNoModel.PDFStream = await StockTransferInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as StockTransferModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await StockTransferInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as StockTransferModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.Bill:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<BillModel>(TableNames.Bill, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.Bill}/{(decodeTransactionNoModel.TransactionModel as BillModel).Id}";
				decodeTransactionNoModel.PDFStream = await BillInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as BillModel).Id, InvoiceExportType.PDF);
				decodeTransactionNoModel.ExcelStream = await BillInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as BillModel).Id, InvoiceExportType.Excel);
				break;
			default:
				break;
		}

		return decodeTransactionNoModel;
	}

	private static async Task<DecodeTransactionNoModel> DecodeTransactionType(string transactionNo)
	{
		DecodeTransactionNoModel decodeTransactionNoModel = new();

		var beforecodeTypePart = "";
		var codeTypePart = "";

		foreach (var character in transactionNo)
		{
			if (char.IsLetter(character))
				beforecodeTypePart += character;

			if (char.IsDigit(character))
				break;
		}

		foreach (var character in transactionNo[(beforecodeTypePart.Length + 2)..])
		{
			if (char.IsLetter(character))
				codeTypePart += character;

			if (char.IsDigit(character))
				break;
		}

		var settings = await CommonData.LoadTableData<SettingsModel>(TableNames.Settings);

		if (string.IsNullOrWhiteSpace(codeTypePart))
		{
			if (string.IsNullOrWhiteSpace(beforecodeTypePart))
				return decodeTransactionNoModel;

			codeTypePart = beforecodeTypePart;

			var settingsKey = settings.FirstOrDefault(s => s.Value == codeTypePart).Key;
			settingsKey = settingsKey.Replace("CodePrefix", "");
			decodeTransactionNoModel.CodeType = Enum.Parse<CodeType>(settingsKey);
		}

		else
		{
			var settingsKey = settings.FirstOrDefault(s => s.Value == codeTypePart).Key;
			settingsKey = settingsKey.Replace("TransactionPrefix", "");
			decodeTransactionNoModel.CodeType = Enum.Parse<CodeType>(settingsKey);
		}

		return decodeTransactionNoModel;
	}
}
