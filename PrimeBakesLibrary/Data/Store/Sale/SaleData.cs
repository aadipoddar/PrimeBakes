using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Store.Order;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Sale;

namespace PrimeBakesLibrary.Data.Store.Sale;

public static class SaleData
{
    private static async Task<int> InsertSale(SaleModel sale, SqlDataAccessTransaction? sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSale, sale, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertSaleDetail(SaleDetailModel saleDetail, SqlDataAccessTransaction? sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleDetail, saleDetail, sqlDataAccessTransaction)).FirstOrDefault();

    private static List<SaleDetailModel> ConvertCartToDetails(List<SaleItemCartModel> cart, int masterId) =>
        [.. cart.Select(item => new SaleDetailModel
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

    public static async Task DeleteTransaction(SaleModel sale)
    {
        await FinancialYearData.ValidateFinancialYear(sale.TransactionDateTime);

        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            if (sale.OrderId is not null && sale.OrderId > 0)
                await OrderData.UnlinkOrderFromSale(sale, sqlDataAccessTransaction);

            sale.OrderId = null;
            sale.Status = false;
            await InsertSale(sale, sqlDataAccessTransaction);

            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Sale), sale.Id, sale.LocationId, sqlDataAccessTransaction);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Sale), sale.Id, sqlDataAccessTransaction);

            if (sale.PartyId is not null and > 0)
            {
                var location = await LocationData.LoadLocationByLedgerId(sale.PartyId.Value, sqlDataAccessTransaction);
                if (location is not null)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Purchase), sale.Id, location.Id, sqlDataAccessTransaction);
            }

            var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(saleVoucher.Value), sale.Id, sale.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = sale.LastModifiedBy;
                existingAccounting.LastModifiedAt = sale.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = sale.LastModifiedFromPlatform;

                await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }

            sqlDataAccessTransaction.CommitTransaction();

            await SaleNotify.Notify(sale.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(SaleModel sale)
    {
        sale.Status = true;
        var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);

        await SaveTransaction(sale, null, transactionDetails, false);
        await SaleNotify.Notify(sale.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(SaleModel sale, List<SaleItemCartModel> cart, List<SaleDetailModel> saleDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = sale.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await SaleInvoiceExport.ExportInvoice(sale.Id, InvoiceExportType.PDF);

            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                sale.Id = await SaveTransaction(sale, cart, saleDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await SaleNotify.Notify(sale.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return sale.Id;
        }

        var existingSale = sale;

        if (update)
        {
            existingSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, sale.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingSale.TransactionDateTime, sqlDataAccessTransaction);
            sale.TransactionNo = existingSale.TransactionNo;
        }
        else
            sale.TransactionNo = await GenerateCodes.GenerateSaleTransactionNo(sale, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(sale.TransactionDateTime, sqlDataAccessTransaction);

        sale.Id = await InsertSale(sale, sqlDataAccessTransaction);
        saleDetails ??= ConvertCartToDetails(cart, sale.Id);
        await SaveTransactionDetail(sale, saleDetails, update, sqlDataAccessTransaction);
        await SaveProductStock(sale, saleDetails, existingSale, update, sqlDataAccessTransaction);
        await SaveRawMaterialStockByRecipe(sale, saleDetails, existingSale, update, sqlDataAccessTransaction);
        await UpdateOrder(sale, existingSale, update, sqlDataAccessTransaction);
        await SaveAccounting(sale, update, sqlDataAccessTransaction);

        return sale.Id;
    }

    private static async Task SaveTransactionDetail(SaleModel sale, List<SaleDetailModel> saleDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (saleDetails is null || saleDetails.Count != sale.TotalItems || saleDetails.Sum(d => d.Quantity) != sale.TotalQuantity)
            throw new InvalidOperationException("Sale details do not match the transaction summary.");

        if (saleDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Sale detail items must be active.");

        if (update)
        {
            var existingSaleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id, sqlDataAccessTransaction);
            foreach (var item in existingSaleDetails)
            {
                item.Status = false;
                await InsertSaleDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in saleDetails)
        {
            item.MasterId = sale.Id;
            var id = await InsertSaleDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save sale detail item.");
        }
    }

    private static async Task SaveProductStock(SaleModel sale, List<SaleDetailModel> saleDetails, SaleModel existingSale, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Sale), existingSale.Id, existingSale.LocationId, sqlDataAccessTransaction);

            if (existingSale.PartyId is not null and > 0)
            {
                var location = await LocationData.LoadLocationByLedgerId(existingSale.PartyId.Value, sqlDataAccessTransaction);
                if (location is not null)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Purchase), existingSale.Id, location.Id, sqlDataAccessTransaction);
            }
        }

        foreach (var item in saleDetails)
        {
            var id = await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ProductId,
                Quantity = -item.Quantity,
                NetRate = item.NetRate,
                TransactionId = sale.Id,
                Type = nameof(StockType.Sale),
                TransactionNo = sale.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime),
                LocationId = sale.LocationId
            }, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save product stock for sale.");
        }

        if (sale.PartyId is not null and > 0)
        {
            var location = await LocationData.LoadLocationByLedgerId(sale.PartyId.Value, sqlDataAccessTransaction);
            if (location is not null)
                foreach (var item in saleDetails)
                {
                    var id = await ProductStockData.InsertProductStock(new()
                    {
                        Id = 0,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        NetRate = item.NetRate,
                        TransactionId = sale.Id,
                        Type = nameof(StockType.Purchase),
                        TransactionNo = sale.TransactionNo,
                        TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime),
                        LocationId = location.Id
                    }, sqlDataAccessTransaction);

                    if (id <= 0)
                        throw new InvalidOperationException("Failed to save product stock for party location.");
                }
        }
    }

    private static async Task SaveRawMaterialStockByRecipe(SaleModel sale, List<SaleDetailModel> saleDetails, SaleModel existingSale, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Sale), existingSale.Id, sqlDataAccessTransaction);

        if (sale.LocationId != 1)
            return;

        var recipes = await CommonData.LoadTableDataByStatus<RecipeModel>(TableNames.Recipe, true, sqlDataAccessTransaction);
        var recipeDetails = await CommonData.LoadTableDataByStatus<RecipeDetailModel>(TableNames.RecipeDetail, true, sqlDataAccessTransaction);

        foreach (var product in saleDetails)
        {
            var recipe = recipes.FirstOrDefault(_ => _.ProductId == product.ProductId);
            var recipeItems = recipe is null ? [] : recipeDetails.Where(_ => _.MasterId == recipe.Id).ToList();

            foreach (var recipeItem in recipeItems)
            {
                var id = await RawMaterialStockData.InsertRawMaterialStock(new()
                {
                    Id = 0,
                    RawMaterialId = recipeItem.RawMaterialId,
                    Quantity = -recipeItem.Quantity * product.Quantity,
                    NetRate = product.NetRate / recipeItem.Quantity,
                    TransactionId = sale.Id,
                    TransactionNo = sale.TransactionNo,
                    Type = nameof(StockType.Sale),
                    TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime)
                }, sqlDataAccessTransaction);

                if (id <= 0)
                    throw new InvalidOperationException("Failed to save raw material stock for sale.");
            }
        }
    }

    private static async Task UpdateOrder(SaleModel sale, SaleModel previousSale, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await OrderData.UnlinkOrderFromSale(previousSale, sqlDataAccessTransaction);

        if (sale.OrderId is not null)
            await OrderData.LinkOrderToSale(sale, sqlDataAccessTransaction);
    }

    private static async Task SaveAccounting(SaleModel sale, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(saleVoucher.Value), sale.Id, sale.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = sale.LastModifiedBy;
                existingAccounting.LastModifiedAt = sale.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = sale.LastModifiedFromPlatform;

                await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }
        }

        var saleOverview = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, sale.Id, sqlDataAccessTransaction);
        if (saleOverview is null)
            return;

        if (saleOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<FinancialAccountingItemCartModel>();

        if (sale.LocationId == 1)
        {
            if (saleOverview.Cash + saleOverview.UPI + saleOverview.Card > 0)
            {
                var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
                accountingCart.Add(new()
                {
                    ReferenceId = saleOverview.Id,
                    ReferenceType = nameof(ReferenceTypes.Sale),
                    ReferenceNo = saleOverview.TransactionNo,
                    LedgerId = int.Parse(cashLedger.Value),
                    Debit = saleOverview.Cash + saleOverview.UPI + saleOverview.Card,
                    Credit = null,
                    Remarks = $"Cash Account Posting For Sale Bill {saleOverview.TransactionNo}",
                });
            }

            if (saleOverview.Credit > 0)
                accountingCart.Add(new()
                {
                    ReferenceId = saleOverview.Id,
                    ReferenceType = nameof(ReferenceTypes.Sale),
                    ReferenceNo = saleOverview.TransactionNo,
                    LedgerId = saleOverview.PartyId.Value,
                    Debit = saleOverview.Credit,
                    Credit = null,
                    Remarks = $"Party Account Posting For Sale Bill {saleOverview.TransactionNo}",
                });
        }
        else
        {
            if (saleOverview.Cash + saleOverview.UPI + saleOverview.Card + saleOverview.Credit > 0)
            {
                var ledger = await LedgerData.LoadLedgerByLocationId(sale.LocationId, sqlDataAccessTransaction);
                accountingCart.Add(new()
                {
                    ReferenceId = saleOverview.Id,
                    ReferenceType = nameof(ReferenceTypes.Sale),
                    ReferenceNo = saleOverview.TransactionNo,
                    LedgerId = ledger.Id,
                    Debit = saleOverview.Cash + saleOverview.UPI + saleOverview.Card + saleOverview.Credit,
                    Credit = null,
                    Remarks = $"Location Account Posting For Sale Bill {saleOverview.TransactionNo}",
                });
            }
        }

        if (saleOverview.TotalAmount - saleOverview.TotalExtraTaxAmount > 0)
        {
            var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = saleOverview.Id,
                ReferenceType = nameof(ReferenceTypes.Sale),
                ReferenceNo = saleOverview.TransactionNo,
                LedgerId = int.Parse(saleLedger.Value),
                Debit = null,
                Credit = saleOverview.TotalAmount - saleOverview.TotalExtraTaxAmount,
                Remarks = $"Sale Account Posting For Sale Bill {saleOverview.TransactionNo}",
            });
        }

        if (saleOverview.TotalExtraTaxAmount > 0)
        {
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = saleOverview.Id,
                ReferenceType = nameof(ReferenceTypes.Sale),
                ReferenceNo = saleOverview.TransactionNo,
                LedgerId = int.Parse(gstLedger.Value),
                Debit = null,
                Credit = saleOverview.TotalExtraTaxAmount,
                Remarks = $"GST Account Posting For Sale Bill {saleOverview.TransactionNo}",
            });
        }

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId, sqlDataAccessTransaction);
        var accounting = new FinancialAccountingModel
        {
            Id = 0,
            TransactionNo = "",
            CompanyId = saleOverview.CompanyId,
            VoucherId = int.Parse(voucher.Value),
            ReferenceId = saleOverview.Id,
            ReferenceNo = saleOverview.TransactionNo,
            TransactionDateTime = saleOverview.TransactionDateTime,
            FinancialYearId = saleOverview.FinancialYearId,
            TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
            TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
            TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
            TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
            Remarks = saleOverview.Remarks,
            CreatedBy = saleOverview.CreatedBy,
            CreatedAt = saleOverview.CreatedAt,
            CreatedFromPlatform = saleOverview.CreatedFromPlatform,
            Status = true
        };

        await FinancialAccountingData.SaveTransaction(accounting, accountingCart, null, false, sqlDataAccessTransaction);
    }
}
