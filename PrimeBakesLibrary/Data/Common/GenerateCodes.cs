using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Data.Common;

public static class GenerateCodes
{
    public enum CodeType
    {
        Purchase,
        PurchaseReturn,
        KitchenIssue,
        KitchenProduction,
        Order,
        Sale,
        SaleReturn,
        StockTransfer,
        Accounting,
        RawMaterial,
        FinishedProduct,
        Ledger,
    }

    private static async Task<string> CheckDuplicateCode(string code, int numberLength, CodeType type, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var isDuplicate = true;
        while (isDuplicate)
        {
            switch (type)
            {
                case CodeType.Purchase:
                    var purchase = await CommonData.LoadTableDataByTransactionNo<PurchaseModel>(TableNames.Purchase, code, sqlDataAccessTransaction);
                    isDuplicate = purchase is not null;
                    break;
                case CodeType.PurchaseReturn:
                    var purchaseReturn = await CommonData.LoadTableDataByTransactionNo<PurchaseReturnModel>(TableNames.PurchaseReturn, code, sqlDataAccessTransaction);
                    isDuplicate = purchaseReturn is not null;
                    break;
                case CodeType.KitchenIssue:
                    var kitchenIssue = await CommonData.LoadTableDataByTransactionNo<KitchenIssueModel>(TableNames.KitchenIssue, code, sqlDataAccessTransaction);
                    isDuplicate = kitchenIssue is not null;
                    break;
                case CodeType.KitchenProduction:
                    var kitchenProduction = await CommonData.LoadTableDataByTransactionNo<KitchenProductionModel>(TableNames.KitchenProduction, code, sqlDataAccessTransaction);
                    isDuplicate = kitchenProduction is not null;
                    break;
                case CodeType.Sale:
                    var sale = await CommonData.LoadTableDataByTransactionNo<SaleModel>(TableNames.Sale, code, sqlDataAccessTransaction);
                    isDuplicate = sale is not null;
                    break;
                case CodeType.SaleReturn:
                    var saleReturn = await CommonData.LoadTableDataByTransactionNo<SaleReturnModel>(TableNames.SaleReturn, code, sqlDataAccessTransaction);
                    isDuplicate = saleReturn is not null;
                    break;
                case CodeType.StockTransfer:
                    var stockTransfer = await CommonData.LoadTableDataByTransactionNo<StockTransferModel>(TableNames.StockTransfer, code, sqlDataAccessTransaction);
                    isDuplicate = stockTransfer is not null;
                    break;
                case CodeType.Order:
                    var order = await CommonData.LoadTableDataByTransactionNo<OrderModel>(TableNames.Order, code, sqlDataAccessTransaction);
                    isDuplicate = order is not null;
                    break;
                case CodeType.Accounting:
                    var accounting = await CommonData.LoadTableDataByTransactionNo<AccountingModel>(TableNames.Accounting, code, sqlDataAccessTransaction);
                    isDuplicate = accounting is not null;
                    break;
                case CodeType.RawMaterial:
                    var rawMaterial = await CommonData.LoadTableDataByCode<RawMaterialModel>(TableNames.RawMaterial, code, sqlDataAccessTransaction);
                    isDuplicate = rawMaterial is not null;
                    break;
                case CodeType.FinishedProduct:
                    var product = await CommonData.LoadTableDataByCode<ProductModel>(TableNames.Product, code, sqlDataAccessTransaction);
                    isDuplicate = product is not null;
                    break;
                case CodeType.Ledger:
                    var ledger = await CommonData.LoadTableDataByCode<LedgerModel>(TableNames.Ledger, code, sqlDataAccessTransaction);
                    isDuplicate = ledger is not null;
                    break;
            }

            if (!isDuplicate)
                return code;

            var prefix = code[..(code.Length - numberLength)];
            var lastNumberPart = code[(code.Length - numberLength)..];
            if (int.TryParse(lastNumberPart, out int lastNumber))
            {
                int nextNumber = lastNumber + 1;
                code = $"{prefix}{nextNumber.ToString($"D{numberLength}")}";
            }
            else
                code = $"{prefix}{1.ToString($"D{numberLength}")}";
        }
        return code;
    }

    public static async Task<string> GeneratePurchaseTransactionNo(PurchaseModel purchase, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchase.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1, sqlDataAccessTransaction)).PrefixCode;
        var purchasePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastPurchase = await CommonData.LoadLastTableDataByFinancialYear<PurchaseModel>(TableNames.Purchase, purchase.FinancialYearId, sqlDataAccessTransaction);
        if (lastPurchase is not null)
        {
            var lastTransactionNo = lastPurchase.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{purchasePrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + purchasePrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchasePrefix}{nextNumber:D6}", 6, CodeType.Purchase, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchasePrefix}000001", 6, CodeType.Purchase, sqlDataAccessTransaction);
    }

    public static async Task<string> GeneratePurchaseReturnTransactionNo(PurchaseReturnModel purchaseReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchaseReturn.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1, sqlDataAccessTransaction)).PrefixCode;
        var purchaseReturnPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastPurchase = await CommonData.LoadLastTableDataByFinancialYear<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturn.FinancialYearId);
        if (lastPurchase is not null)
        {
            var lastTransactionNo = lastPurchase.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{purchaseReturnPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + purchaseReturnPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchaseReturnPrefix}{nextNumber:D6}", 6, CodeType.PurchaseReturn, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchaseReturnPrefix}000001", 6, CodeType.PurchaseReturn, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateProductStockAdjustmentTransactionNo(DateTime transactionDateTime, int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId, sqlDataAccessTransaction)).PrefixCode;
        var adjustmentPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.ProductStockAdjustmentTransactionPrefix, sqlDataAccessTransaction)).Value;
        var currentDateTime = await CommonData.LoadCurrentDateTime();

        return $"{locationPrefix}{financialYear.YearNo}{adjustmentPrefix}{currentDateTime:ddMMyy}{currentDateTime:HHmmss}";
    }

    public static async Task<string> GenerateRawMaterialStockAdjustmentTransactionNo(DateTime transactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1, sqlDataAccessTransaction)).PrefixCode;
        var adjustmentPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix, sqlDataAccessTransaction)).Value;
        var currentDateTime = await CommonData.LoadCurrentDateTime();

        return $"{locationPrefix}{financialYear.YearNo}{adjustmentPrefix}{currentDateTime:ddMMyy}{currentDateTime:HHmmss}";
    }

    public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel kitchenIssue, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1, sqlDataAccessTransaction)).PrefixCode;
        var kitchenIssuePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenIssueTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastKitchenIssue = await CommonData.LoadLastTableDataByFinancialYear<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssue.FinancialYearId, sqlDataAccessTransaction);
        if (lastKitchenIssue is not null)
        {
            var lastTransactionNo = lastKitchenIssue.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{kitchenIssuePrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + kitchenIssuePrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenIssuePrefix}{nextNumber:D6}", 6, CodeType.KitchenIssue, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenIssuePrefix}000001", 6, CodeType.KitchenIssue, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel kitchenProduction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenProduction.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1, sqlDataAccessTransaction)).PrefixCode;
        var kitchenProductionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenProductionTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastKitchenProduction = await CommonData.LoadLastTableDataByFinancialYear<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProduction.FinancialYearId, sqlDataAccessTransaction);
        if (lastKitchenProduction is not null)
        {
            var lastTransactionNo = lastKitchenProduction.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{kitchenProductionPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + kitchenProductionPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenProductionPrefix}{nextNumber:D6}", 6, CodeType.KitchenProduction, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenProductionPrefix}000001", 6, CodeType.KitchenProduction, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateSaleTransactionNo(SaleModel sale, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, sale.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, sale.LocationId, sqlDataAccessTransaction)).PrefixCode;
        var salePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SaleTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastSale = await CommonData.LoadLastTableDataByLocationFinancialYear<SaleModel>(TableNames.Sale, sale.LocationId, sale.FinancialYearId);
        if (lastSale is not null)
        {
            var lastTransactionNo = lastSale.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{salePrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + salePrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{salePrefix}{nextNumber:D6}", 6, CodeType.Sale, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{salePrefix}000001", 6, CodeType.Sale, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel saleReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, saleReturn.LocationId, sqlDataAccessTransaction)).PrefixCode;
        var saleReturnPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastSaleReturn = await CommonData.LoadLastTableDataByLocationFinancialYear<SaleReturnModel>(TableNames.SaleReturn, saleReturn.LocationId, saleReturn.FinancialYearId);
        if (lastSaleReturn is not null)
        {
            var lastTransactionNo = lastSaleReturn.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{saleReturnPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + saleReturnPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{saleReturnPrefix}{nextNumber:D6}", 6, CodeType.SaleReturn, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{saleReturnPrefix}000001", 6, CodeType.SaleReturn, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateStockTransferTransactionNo(StockTransferModel stockTransfer, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, stockTransfer.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, stockTransfer.LocationId, sqlDataAccessTransaction)).PrefixCode;
        var stockTransferPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastStockTransfer = await CommonData.LoadLastTableDataByLocationFinancialYear<StockTransferModel>(TableNames.StockTransfer, stockTransfer.LocationId, stockTransfer.FinancialYearId);
        if (lastStockTransfer is not null)
        {
            var lastTransactionNo = lastStockTransfer.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{stockTransferPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + stockTransferPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{stockTransferPrefix}{nextNumber:D6}", 6, CodeType.StockTransfer, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{stockTransferPrefix}000001", 6, CodeType.StockTransfer, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateOrderTransactionNo(OrderModel order, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, order.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, order.LocationId, sqlDataAccessTransaction)).PrefixCode;
        var orderPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.OrderTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastOrder = await CommonData.LoadLastTableDataByLocationFinancialYear<OrderModel>(TableNames.Order, order.LocationId, order.FinancialYearId, sqlDataAccessTransaction);
        if (lastOrder is not null)
        {
            var lastTransactionNo = lastOrder.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{orderPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + orderPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{orderPrefix}{nextNumber:D6}", 6, CodeType.Order, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{orderPrefix}000001", 6, CodeType.Order, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateAccountingTransactionNo(AccountingModel accounting, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId, sqlDataAccessTransaction);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1, sqlDataAccessTransaction)).PrefixCode;
        var accountingPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.AccountingTransactionPrefix, sqlDataAccessTransaction)).Value;

        var lastAccounting = await CommonData.LoadLastTableDataByFinancialYear<AccountingModel>(TableNames.Accounting, accounting.FinancialYearId, sqlDataAccessTransaction);
        if (lastAccounting is not null)
        {
            var lastTransactionNo = lastAccounting.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{accountingPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + accountingPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{accountingPrefix}{nextNumber:D6}", 6, CodeType.Accounting, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{accountingPrefix}000001", 6, CodeType.Accounting, sqlDataAccessTransaction);
    }

    public static async Task<string> GenerateRawMaterialCode()
    {
        var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
        var rawMaterialPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialCodePrefix)).Value;

        var lastRawMaterial = rawMaterials.OrderByDescending(r => r.Id).FirstOrDefault();
        if (lastRawMaterial is not null)
        {
            var lastRawMaterialCode = lastRawMaterial.Code;
            if (lastRawMaterialCode.StartsWith(rawMaterialPrefix))
            {
                var lastNumberPart = lastRawMaterialCode[rawMaterialPrefix.Length..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{rawMaterialPrefix}{nextNumber:D4}", 4, CodeType.RawMaterial);
                }
            }
        }

        return await CheckDuplicateCode($"{rawMaterialPrefix}0001", 4, CodeType.RawMaterial);
    }

    public static async Task<string> GenerateProductCode()
    {
        var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
        var productPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.FinishedProductCodePrefix)).Value;

        var lastProduct = products.OrderByDescending(p => p.Id).FirstOrDefault();
        if (lastProduct is not null)
        {
            var lastProductCode = lastProduct.Code;
            if (lastProductCode.StartsWith(productPrefix))
            {
                var lastNumberPart = lastProductCode[productPrefix.Length..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{productPrefix}{nextNumber:D4}", 4, CodeType.FinishedProduct);
                }
            }
        }

        return await CheckDuplicateCode($"{productPrefix}0001", 4, CodeType.FinishedProduct);
    }

    public static async Task<string> GenerateLedgerCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger, sqlDataAccessTransaction);
        var ledgerPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.LedgerCodePrefix, sqlDataAccessTransaction)).Value;

        var lastLedger = ledgers.OrderByDescending(l => l.Id).FirstOrDefault();
        if (lastLedger is not null)
        {
            var lastLedgerCode = lastLedger.Code;
            if (lastLedgerCode.StartsWith(ledgerPrefix))
            {
                var lastNumberPart = lastLedgerCode[ledgerPrefix.Length..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{ledgerPrefix}{nextNumber:D5}", 5, CodeType.Ledger, sqlDataAccessTransaction);
                }
            }
        }

        return await CheckDuplicateCode($"{ledgerPrefix}00001", 5, CodeType.Ledger, sqlDataAccessTransaction);
    }
}
