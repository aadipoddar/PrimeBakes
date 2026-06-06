using PrimeBakesLibrary.Accounts.FinancialAccounting.Exports;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Exports;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Kitchen.Exports;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Inventory.Purchase.Exports;
using PrimeBakesLibrary.Inventory.Purchase.Models;
using PrimeBakesLibrary.Inventory.RawMaterial.Exports;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Restaurant.Bill.Exports;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Store.Order.Exports;
using PrimeBakesLibrary.Store.Order.Models;
using PrimeBakesLibrary.Store.Product.Exports;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Exports;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Store.StockTransfer.Exports;
using PrimeBakesLibrary.Store.StockTransfer.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Common;

public static class DecodeCode
{
	public static async Task<DecodeTransactionNoModel> DecodeTransactionNo(string transactionNo, bool pdf = true, bool excel = true, CodeType? codeType = null)
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
			#region Accounts
			case CodeType.Accounting:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<FinancialAccountingModel>(AccountNames.FinancialAccounting, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{AccountsRouteNames.FinancialAccounting}/{(decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await FinancialAccountingInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await FinancialAccountingInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.Ledger:
				var ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<LedgerModel>(AccountNames.Ledger, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{AccountsRouteNames.LedgerMaster}";
				if (pdf) decodeTransactionNoModel.PDFStream = await LedgerExport.ExportMaster(ledgers, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await LedgerExport.ExportMaster(ledgers, ReportExportType.Excel);
				break;
			#endregion

			#region Inventory
			case CodeType.Purchase:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<PurchaseModel>(InventoryNames.Purchase, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.Purchase}/{(decodeTransactionNoModel.TransactionModel as PurchaseModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await PurchaseInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await PurchaseInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.PurchaseReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<PurchaseReturnModel>(InventoryNames.PurchaseReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.PurchaseReturn}/{(decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await PurchaseReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await PurchaseReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.KitchenIssue:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenIssueModel>(InventoryNames.KitchenIssue, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.KitchenIssue}/{(decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await KitchenIssueInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await KitchenIssueInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.KitchenProduction:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenProductionModel>(InventoryNames.KitchenProduction, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.KitchenProduction}/{(decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await KitchenProductionInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await KitchenProductionInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.RawMaterial:
				var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(InventoryNames.RawMaterial);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<RawMaterialModel>(InventoryNames.RawMaterial, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.RawMaterial}";
				if (pdf) decodeTransactionNoModel.PDFStream = await RawMaterialExport.ExportMaster(rawMaterials, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await RawMaterialExport.ExportMaster(rawMaterials, ReportExportType.Excel);
				break;
			#endregion

			#region Store
			case CodeType.Order:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<OrderModel>(StoreNames.Order, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.Order}/{(decodeTransactionNoModel.TransactionModel as OrderModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await OrderInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as OrderModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await OrderInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as OrderModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.Sale:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<SaleModel>(StoreNames.Sale, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.Sale}/{(decodeTransactionNoModel.TransactionModel as SaleModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await SaleInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await SaleInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.SaleReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<SaleReturnModel>(StoreNames.SaleReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.SaleReturn}/{(decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await SaleReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await SaleReturnInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.StockTransfer:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<StockTransferModel>(StoreNames.StockTransfer, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.StockTransfer}/{(decodeTransactionNoModel.TransactionModel as StockTransferModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await StockTransferInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as StockTransferModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await StockTransferInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as StockTransferModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.FinishedProduct:
				var products = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<ProductModel>(StoreNames.Product, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.Product}";
				if (pdf) decodeTransactionNoModel.PDFStream = await ProductExport.ExportMaster(products, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await ProductExport.ExportMaster(products, ReportExportType.Excel);
				break;
			#endregion

			#region Restuarant
			case CodeType.Bill:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<BillModel>(RestaurantNames.Bill, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{RestaurnatRouteNames.Bill}/{(decodeTransactionNoModel.TransactionModel as BillModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await BillInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as BillModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await BillInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as BillModel).Id, InvoiceExportType.Excel);
				break;
			#endregion
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

		var settings = await CommonData.LoadTableData<SettingsModel>(OperationNames.Settings);

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
