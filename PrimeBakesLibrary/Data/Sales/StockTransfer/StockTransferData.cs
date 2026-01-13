using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Sales;
using PrimeBakesLibrary.Exporting.Sales.StockTransfer;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Data.Sales.StockTransfer;

public static class StockTransferData
{
    private static async Task<int> InsertStockTransfer(StockTransferModel stockTransfer, SqlDataAccessTransaction sqlDataAccessTransaction) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertStockTransfer, stockTransfer, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertStockTransferDetail(StockTransferDetailModel stockTransferDetail, SqlDataAccessTransaction sqlDataAccessTransaction) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertStockTransferDetail, stockTransferDetail, sqlDataAccessTransaction)).FirstOrDefault();

    private static List<StockTransferDetailModel> ConvertCartToDetails(List<StockTransferItemCartModel> cart, int masterId) =>
        [.. cart.Select(item => new StockTransferDetailModel
        {
            Id = 0,
            MasterId = masterId,
            ProductId = item.ItemId,
            Quantity = item.Quantity,
            Rate = item.Rate,
            BaseTotal = item.BaseTotal,
            DiscountPercent = item.DiscountPercent,
            DiscountAmount = item.DiscountAmount,
            AfterDiscount = item.AfterDiscount,
            CGSTPercent = item.CGSTPercent,
            CGSTAmount = item.CGSTAmount,
            SGSTPercent = item.SGSTPercent,
            SGSTAmount = item.SGSTAmount,
            IGSTPercent = item.IGSTPercent,
            IGSTAmount = item.IGSTAmount,
            TotalTaxAmount = item.TotalTaxAmount,
            InclusiveTax = item.InclusiveTax,
            NetRate = item.NetRate,
            Total = item.Total,
            Remarks = item.Remarks,
            Status = true
        })];

    public static async Task DeleteTransaction(StockTransferModel stockTransfer)
    {
        await FinancialYearData.ValidateFinancialYear(stockTransfer.TransactionDateTime);

        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            stockTransfer.Status = false;
            await InsertStockTransfer(stockTransfer, sqlDataAccessTransaction);

            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), stockTransfer.Id, stockTransfer.LocationId, sqlDataAccessTransaction);
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), stockTransfer.Id, stockTransfer.ToLocationId, sqlDataAccessTransaction);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.StockTransfer), stockTransfer.Id, sqlDataAccessTransaction);

            var stockTransferVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(stockTransferVoucher.Value), stockTransfer.Id, stockTransfer.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = stockTransfer.LastModifiedBy;
                existingAccounting.LastModifiedAt = stockTransfer.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = stockTransfer.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }

            sqlDataAccessTransaction.CommitTransaction();

            await StockTransferNotify.Notify(stockTransfer.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(StockTransferModel stockTransfer)
    {
        stockTransfer.Status = true;
        var transactionDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(TableNames.StockTransferDetail, stockTransfer.Id);

        await SaveTransaction(stockTransfer, null, transactionDetails, false);
        await StockTransferNotify.Notify(stockTransfer.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(StockTransferModel stockTransfer, List<StockTransferItemCartModel> cart, List<StockTransferDetailModel> stockTransferDetails = null, bool showNotification = true, SqlDataAccessTransaction? sqlDataAccessTransaction = null)
    {
        bool update = stockTransfer.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await StockTransferInvoiceExport.ExportInvoice(stockTransfer.Id, InvoiceExportType.PDF);

            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                stockTransfer.Id = await SaveTransaction(stockTransfer, cart, stockTransferDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await StockTransferNotify.Notify(stockTransfer.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return stockTransfer.Id;
        }

        var existingStockTransfer = stockTransfer;

        if (update)
        {
            existingStockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, stockTransfer.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingStockTransfer.TransactionDateTime, sqlDataAccessTransaction);
            stockTransfer.TransactionNo = existingStockTransfer.TransactionNo;
        }
        else
            stockTransfer.TransactionNo = await GenerateCodes.GenerateStockTransferTransactionNo(stockTransfer, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(stockTransfer.TransactionDateTime, sqlDataAccessTransaction);

        stockTransfer.Id = await InsertStockTransfer(stockTransfer, sqlDataAccessTransaction);
        stockTransferDetails ??= ConvertCartToDetails(cart, stockTransfer.Id);
        await SaveTransactionDetail(stockTransfer, stockTransferDetails, update, sqlDataAccessTransaction);
        await SaveProductStock(stockTransfer, stockTransferDetails, existingStockTransfer, update, sqlDataAccessTransaction);
        await SaveRawMaterialStockByRecipe(stockTransfer, stockTransferDetails, existingStockTransfer, update, sqlDataAccessTransaction);
        await SaveAccounting(stockTransfer, update, sqlDataAccessTransaction);

        return stockTransfer.Id;
    }

    private static async Task SaveTransactionDetail(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (stockTransferDetails is null || stockTransferDetails.Count != stockTransfer.TotalItems || stockTransferDetails.Sum(d => d.Quantity) != stockTransfer.TotalQuantity)
            throw new InvalidOperationException("Stock transfer details do not match the transaction summary.");

        if (stockTransferDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Stock transfer detail items must be active.");

        if (update)
        {
            var existingStockTransferDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(TableNames.StockTransferDetail, stockTransfer.Id, sqlDataAccessTransaction);
            foreach (var item in existingStockTransferDetails)
            {
                item.Status = false;
                await InsertStockTransferDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in stockTransferDetails)
        {
            item.MasterId = stockTransfer.Id;
            var id = await InsertStockTransferDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save stock transfer detail.");
        }
    }

    private static async Task SaveProductStock(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, StockTransferModel existingStockTransfer, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), existingStockTransfer.Id, existingStockTransfer.ToLocationId, sqlDataAccessTransaction);
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), existingStockTransfer.Id, existingStockTransfer.LocationId, sqlDataAccessTransaction);
        }

        // From Location Stock Update (negative quantity - stock leaves)
        foreach (var item in stockTransferDetails)
        {
            var id = await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ProductId,
                Quantity = -item.Quantity,
                NetRate = item.NetRate,
                TransactionId = stockTransfer.Id,
                Type = nameof(StockType.StockTransfer),
                TransactionNo = stockTransfer.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(stockTransfer.TransactionDateTime),
                LocationId = stockTransfer.LocationId
            }, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save product stock for from location.");
        }

        // To Location Stock Update (positive quantity - stock arrives)
        foreach (var item in stockTransferDetails)
        {
            var id = await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                NetRate = item.NetRate,
                TransactionId = stockTransfer.Id,
                Type = nameof(StockType.StockTransfer),
                TransactionNo = stockTransfer.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(stockTransfer.TransactionDateTime),
                LocationId = stockTransfer.ToLocationId
            }, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save product stock for to location.");
        }
    }

    private static async Task SaveRawMaterialStockByRecipe(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, StockTransferModel existingStockTransfer, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.StockTransfer), existingStockTransfer.Id, sqlDataAccessTransaction);

        if (stockTransfer.LocationId != 1 && stockTransfer.ToLocationId != 1)
            return;

        var recipes = await CommonData.LoadTableDataByStatus<RecipeModel>(TableNames.Recipe, true, sqlDataAccessTransaction);
        var recipeDetails = await CommonData.LoadTableDataByStatus<RecipeDetailModel>(TableNames.RecipeDetail, true, sqlDataAccessTransaction);

        foreach (var product in stockTransferDetails)
        {
            var recipe = recipes.FirstOrDefault(_ => _.ProductId == product.ProductId);
            var recipeItems = recipe is null ? [] : recipeDetails.Where(_ => _.MasterId == recipe.Id).ToList();

            foreach (var recipeItem in recipeItems)
            {
                var id = await RawMaterialStockData.InsertRawMaterialStock(new()
                {
                    Id = 0,
                    RawMaterialId = recipeItem.RawMaterialId,
                    Quantity = stockTransfer.LocationId == 1 ? -recipeItem.Quantity * product.Quantity : recipeItem.Quantity * product.Quantity,
                    NetRate = product.NetRate / recipeItem.Quantity,
                    TransactionId = stockTransfer.Id,
                    TransactionNo = stockTransfer.TransactionNo,
                    Type = nameof(StockType.StockTransfer),
                    TransactionDate = DateOnly.FromDateTime(stockTransfer.TransactionDateTime)
                }, sqlDataAccessTransaction);

                if (id <= 0)
                    throw new InvalidOperationException("Failed to save raw material stock for stock transfer.");
            }
        }
    }

    private static async Task SaveAccounting(StockTransferModel stockTransfer, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            var stockTransferVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(stockTransferVoucher.Value), stockTransfer.Id, stockTransfer.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = stockTransfer.LastModifiedBy;
                existingAccounting.LastModifiedAt = stockTransfer.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = stockTransfer.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }
        }

        if (stockTransfer.LocationId != 1 && stockTransfer.ToLocationId != 1)
            return;

        var stockTransferOverview = await CommonData.LoadTableDataById<StockTransferOverviewModel>(ViewNames.StockTransferOverview, stockTransfer.Id, sqlDataAccessTransaction);
        if (stockTransferOverview is null)
            return;

        if (stockTransferOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<AccountingItemCartModel>();

        if (stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card > 0)
        {
            var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = stockTransferOverview.Id,
                ReferenceType = nameof(ReferenceTypes.StockTransfer),
                ReferenceNo = stockTransferOverview.TransactionNo,
                LedgerId = int.Parse(cashLedger.Value),
                Debit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card : null,
                Credit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card : null,
                Remarks = $"Cash Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
            });
        }

        if (stockTransferOverview.Credit > 0)
        {
            var ledger = await LedgerData.LoadLedgerByLocationId(stockTransferOverview.ToLocationId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = stockTransferOverview.Id,
                ReferenceType = nameof(ReferenceTypes.StockTransfer),
                ReferenceNo = stockTransferOverview.TransactionNo,
                LedgerId = ledger.Id,
                Debit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.Credit : null,
                Credit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.Credit : null,
                Remarks = $"Party Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
            });
        }

        if (stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount > 0)
        {
            var stockTransferLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = stockTransferOverview.Id,
                ReferenceType = nameof(ReferenceTypes.StockTransfer),
                ReferenceNo = stockTransferOverview.TransactionNo,
                LedgerId = int.Parse(stockTransferLedger.Value),
                Debit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount : null,
                Credit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount : null,
                Remarks = $"Stock Transfer Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
            });
        }

        if (stockTransferOverview.TotalExtraTaxAmount > 0)
        {
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = stockTransferOverview.Id,
                ReferenceType = nameof(ReferenceTypes.StockTransfer),
                ReferenceNo = stockTransferOverview.TransactionNo,
                LedgerId = int.Parse(gstLedger.Value),
                Debit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.TotalExtraTaxAmount : null,
                Credit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.TotalExtraTaxAmount : null,
                Remarks = $"GST Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
            });
        }

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId, sqlDataAccessTransaction);
        var accounting = new AccountingModel
        {
            Id = 0,
            TransactionNo = "",
            CompanyId = stockTransferOverview.CompanyId,
            VoucherId = int.Parse(voucher.Value),
            ReferenceId = stockTransferOverview.Id,
            ReferenceNo = stockTransferOverview.TransactionNo,
            TransactionDateTime = stockTransferOverview.TransactionDateTime,
            FinancialYearId = stockTransferOverview.FinancialYearId,
            TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
            TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
            TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
            TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
            Remarks = stockTransferOverview.Remarks,
            CreatedBy = stockTransferOverview.CreatedBy,
            CreatedAt = stockTransferOverview.CreatedAt,
            CreatedFromPlatform = stockTransferOverview.CreatedFromPlatform,
            Status = true
        };

        await AccountingData.SaveTransaction(accounting, accountingCart, null, false, sqlDataAccessTransaction);
    }
}
