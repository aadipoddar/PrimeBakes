using PrimeBakes.Data.Accounts.FinancialAccounting;
using PrimeBakes.Data.Inventory.Kitchen;
using PrimeBakes.Data.Inventory.Purchase;
using PrimeBakes.Data.Restaurant.Bill;
using PrimeBakes.Data.Store.Order;
using PrimeBakes.Data.Store.Sale;
using PrimeBakes.Data.Store.StockTransfer;
using PrimeBakes.Exports.Accounts.FinancialAccounting;
using PrimeBakes.Exports.Accounts.Masters;
using PrimeBakes.Exports.Inventory.Kitchen;
using PrimeBakes.Exports.Inventory.Purchase;
using PrimeBakes.Exports.Inventory.RawMaterial;
using PrimeBakes.Exports.Restaurant.Bill;
using PrimeBakes.Exports.Store.Order;
using PrimeBakes.Exports.Store.Product;
using PrimeBakes.Exports.Store.Sale;
using PrimeBakes.Exports.Store.StockTransfer;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Data.Common;

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
				if (pdf || excel)
				{
					var accountingBundle = await FinancialAccountingData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = FinancialAccountingInvoiceExport.ExportInvoice(accountingBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = FinancialAccountingInvoiceExport.ExportInvoice(accountingBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.Ledger:
				var ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<LedgerModel>(AccountNames.Ledger, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{AccountsRouteNames.LedgerMaster}";
				if (pdf || excel)
				{
					var groups = await CommonData.LoadTableData<GroupModel>(AccountNames.Group);
					var accountTypes = await CommonData.LoadTableData<AccountTypeModel>(AccountNames.AccountType);
					var stateUTs = await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT);
					var ledgerDateTime = await CommonData.LoadCurrentDateTime();
					if (pdf) decodeTransactionNoModel.PDFStream = LedgerExport.ExportMaster(ledgers, groups, accountTypes, stateUTs, ledgerDateTime, ReportExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = LedgerExport.ExportMaster(ledgers, groups, accountTypes, stateUTs, ledgerDateTime, ReportExportType.Excel);
				}
				break;
			#endregion

			#region Inventory
			case CodeType.Purchase:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<PurchaseModel>(InventoryNames.Purchase, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.Purchase}/{(decodeTransactionNoModel.TransactionModel as PurchaseModel).Id}";
				if (pdf || excel)
				{
					var purchaseBundle = await PurchaseData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as PurchaseModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = PurchaseInvoiceExport.ExportInvoice(purchaseBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = PurchaseInvoiceExport.ExportInvoice(purchaseBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.PurchaseReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<PurchaseReturnModel>(InventoryNames.PurchaseReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.PurchaseReturn}/{(decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id}";
				if (pdf || excel)
				{
					var purchaseReturnBundle = await PurchaseReturnData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as PurchaseReturnModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = PurchaseReturnInvoiceExport.ExportInvoice(purchaseReturnBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = PurchaseReturnInvoiceExport.ExportInvoice(purchaseReturnBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.KitchenIssue:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenIssueModel>(InventoryNames.KitchenIssue, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.KitchenIssue}/{(decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id}";
				if (pdf || excel)
				{
					var kitchenIssueBundle = await KitchenIssueData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as KitchenIssueModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = KitchenIssueInvoiceExport.ExportInvoice(kitchenIssueBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = KitchenIssueInvoiceExport.ExportInvoice(kitchenIssueBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.KitchenIssueReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenIssueReturnModel>(InventoryNames.KitchenIssueReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.KitchenIssueReturn}/{(decodeTransactionNoModel.TransactionModel as KitchenIssueReturnModel).Id}";
				if (pdf || excel)
				{
					var kitchenIssueReturnBundle = await KitchenIssueReturnData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as KitchenIssueReturnModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = KitchenIssueReturnInvoiceExport.ExportInvoice(kitchenIssueReturnBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = KitchenIssueReturnInvoiceExport.ExportInvoice(kitchenIssueReturnBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.KitchenProduction:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenProductionModel>(InventoryNames.KitchenProduction, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.KitchenProduction}/{(decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id}";
				if (pdf || excel)
				{
					var kitchenProductionBundle = await KitchenProductionData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as KitchenProductionModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = KitchenProductionInvoiceExport.ExportInvoice(kitchenProductionBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = KitchenProductionInvoiceExport.ExportInvoice(kitchenProductionBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.KitchenProductionReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<KitchenProductionReturnModel>(InventoryNames.KitchenProductionReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.KitchenProductionReturn}/{(decodeTransactionNoModel.TransactionModel as KitchenProductionReturnModel).Id}";
				if (pdf || excel)
				{
					var kitchenProductionReturnBundle = await KitchenProductionReturnData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as KitchenProductionReturnModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = KitchenProductionReturnInvoiceExport.ExportInvoice(kitchenProductionReturnBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = KitchenProductionReturnInvoiceExport.ExportInvoice(kitchenProductionReturnBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.RawMaterial:
				var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(InventoryNames.RawMaterial);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<RawMaterialModel>(InventoryNames.RawMaterial, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{InventoryRouteNames.RawMaterial}";
				if (pdf || excel)
				{
					var rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(InventoryNames.RawMaterialCategory);
					var rawMaterialTaxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);
					var rawMaterialDateTime = await CommonData.LoadCurrentDateTime();
					if (pdf) decodeTransactionNoModel.PDFStream = RawMaterialExport.ExportMaster(rawMaterials, rawMaterialCategories, rawMaterialTaxes, rawMaterialDateTime, ReportExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = RawMaterialExport.ExportMaster(rawMaterials, rawMaterialCategories, rawMaterialTaxes, rawMaterialDateTime, ReportExportType.Excel);
				}
				break;
			#endregion

			#region Store
			case CodeType.Order:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<OrderModel>(StoreNames.Order, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.Order}/{(decodeTransactionNoModel.TransactionModel as OrderModel).Id}";
				if (pdf || excel)
				{
					var orderBundle = await OrderData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as OrderModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = OrderInvoiceExport.ExportInvoice(orderBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = OrderInvoiceExport.ExportInvoice(orderBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.Sale:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<SaleModel>(StoreNames.Sale, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.Sale}/{(decodeTransactionNoModel.TransactionModel as SaleModel).Id}";
				if (pdf || excel)
				{
					var saleBundle = await SaleData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as SaleModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = SaleInvoiceExport.ExportInvoice(saleBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = SaleInvoiceExport.ExportInvoice(saleBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.SaleReturn:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<SaleReturnModel>(StoreNames.SaleReturn, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.SaleReturn}/{(decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id}";
				if (pdf || excel)
				{
					var saleReturnBundle = await SaleReturnData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as SaleReturnModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = SaleReturnInvoiceExport.ExportInvoice(saleReturnBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = SaleReturnInvoiceExport.ExportInvoice(saleReturnBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.StockTransfer:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<StockTransferModel>(StoreNames.StockTransfer, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.StockTransfer}/{(decodeTransactionNoModel.TransactionModel as StockTransferModel).Id}";
				if (pdf || excel)
				{
					var stockTransferBundle = await StockTransferData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as StockTransferModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = StockTransferInvoiceExport.ExportInvoice(stockTransferBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = StockTransferInvoiceExport.ExportInvoice(stockTransferBundle, InvoiceExportType.Excel);
				}
				break;
			case CodeType.FinishedProduct:
				var products = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<ProductModel>(StoreNames.Product, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{StoreRouteNames.Product}";
				if (pdf || excel)
				{
					var productCategories = await CommonData.LoadTableData<ProductCategoryModel>(StoreNames.ProductCategory);
					var productKOTCategories = await CommonData.LoadTableData<KOTCategoryModel>(StoreNames.KOTCategory);
					var productTaxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);
					var productDateTime = await CommonData.LoadCurrentDateTime();
					if (pdf) decodeTransactionNoModel.PDFStream = ProductExport.ExportMaster(products, productCategories, productKOTCategories, productTaxes, productDateTime, ReportExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = ProductExport.ExportMaster(products, productCategories, productKOTCategories, productTaxes, productDateTime, ReportExportType.Excel);
				}
				break;
			#endregion

			#region Restuarant
			case CodeType.Bill:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<BillModel>(RestaurantNames.Bill, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{RestaurantRouteNames.Bill}/{(decodeTransactionNoModel.TransactionModel as BillModel).Id}";
				if (pdf || excel)
				{
					var billBundle = await BillData.LoadInvoiceBundle((decodeTransactionNoModel.TransactionModel as BillModel).Id);
					if (pdf) decodeTransactionNoModel.PDFStream = BillInvoiceExport.ExportInvoice(billBundle, InvoiceExportType.PDF);
					if (excel) decodeTransactionNoModel.ExcelStream = BillInvoiceExport.ExportInvoice(billBundle, InvoiceExportType.Excel);
				}
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
