
using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Sale;

public static class SaleReturnData
{
    private static async Task<int> InsertSaleReturn(SaleReturnModel saleReturn, SqlDataAccessTransaction sqlDataAccessTransaction) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturn, saleReturn, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertSaleReturnDetail(SaleReturnDetailModel saleReturnDetail, SqlDataAccessTransaction sqlDataAccessTransaction) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturnDetail, saleReturnDetail, sqlDataAccessTransaction)).FirstOrDefault();

    private static List<SaleReturnDetailModel> ConvertCartToDetails(List<SaleReturnItemCartModel> cart, int masterId) =>
        [.. cart.Select(item => new SaleReturnDetailModel
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

    public static async Task DeleteTransaction(SaleReturnModel saleReturn)
    {
        await FinancialYearData.ValidateFinancialYear(saleReturn.TransactionDateTime);

        using SqlDataAccessTransaction sqlDataAccessTransaction = new();
        try
        {
            sqlDataAccessTransaction.StartTransaction();

            saleReturn.Status = false;
            await InsertSaleReturn(saleReturn, sqlDataAccessTransaction);

            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.SaleReturn), saleReturn.Id, saleReturn.LocationId, sqlDataAccessTransaction);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.SaleReturn), saleReturn.Id, sqlDataAccessTransaction);

            if (saleReturn.PartyId is not null and > 0)
            {
                var location = await LocationData.LoadLocationByLedgerId(saleReturn.PartyId.Value, sqlDataAccessTransaction);
                if (location is not null)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.PurchaseReturn), saleReturn.Id, location.Id, sqlDataAccessTransaction);
            }

            var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleReturnVoucher.Value), saleReturn.Id, saleReturn.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = saleReturn.LastModifiedBy;
                existingAccounting.LastModifiedAt = saleReturn.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = saleReturn.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }

            sqlDataAccessTransaction.CommitTransaction();

            await SaleReturnNotify.Notify(saleReturn.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(SaleReturnModel saleReturn)
    {
        saleReturn.Status = true;
        var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturn.Id);

        await SaveTransaction(saleReturn, null, transactionDetails, false);
        await SaleReturnNotify.Notify(saleReturn.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> cart, List<SaleReturnDetailModel> saleReturnDetails = null, bool showNotification = true, SqlDataAccessTransaction? sqlDataAccessTransaction = null)
    {
        bool update = saleReturn.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await SaleReturnInvoiceExport.ExportInvoice(saleReturn.Id, InvoiceExportType.PDF);

            using SqlDataAccessTransaction newTransaction = new();

            try
            {
                newTransaction.StartTransaction();
                saleReturn.Id = await SaveTransaction(saleReturn, cart, saleReturnDetails, showNotification, newTransaction);
                newTransaction.CommitTransaction();
            }
            catch
            {
                newTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await SaleReturnNotify.Notify(saleReturn.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return saleReturn.Id;
        }

        var existingSaleReturn = saleReturn;

        if (update)
        {
            existingSaleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturn.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingSaleReturn.TransactionDateTime, sqlDataAccessTransaction);
            saleReturn.TransactionNo = existingSaleReturn.TransactionNo;
        }
        else
            saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(saleReturn, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(saleReturn.TransactionDateTime, sqlDataAccessTransaction);

        saleReturn.Id = await InsertSaleReturn(saleReturn, sqlDataAccessTransaction);
        saleReturnDetails ??= ConvertCartToDetails(cart, saleReturn.Id);
        await SaveTransactionDetail(saleReturn, saleReturnDetails, update, sqlDataAccessTransaction);
        await SaveProductStock(saleReturn, saleReturnDetails, existingSaleReturn, update, sqlDataAccessTransaction);
        await SaveRawMaterialStockByRecipe(saleReturn, saleReturnDetails, existingSaleReturn, update, sqlDataAccessTransaction);
        await SaveAccounting(saleReturn, update, sqlDataAccessTransaction);

        return saleReturn.Id;
    }

    private static async Task SaveTransactionDetail(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (saleReturnDetails is null || saleReturnDetails.Count != saleReturn.TotalItems || saleReturnDetails.Sum(d => d.Quantity) != saleReturn.TotalQuantity)
            throw new InvalidOperationException("Sale return details do not match the transaction summary.");

        if (saleReturnDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Sale return detail items must be active.");

        if (update)
        {
            var existingTransactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturn.Id, sqlDataAccessTransaction);
            foreach (var item in existingTransactionDetails)
            {
                item.Status = false;
                await InsertSaleReturnDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in saleReturnDetails)
        {
            item.MasterId = saleReturn.Id;
            var id = await InsertSaleReturnDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save sale return detail.");
        }
    }

    private static async Task SaveProductStock(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, SaleReturnModel existingSaleReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.SaleReturn), existingSaleReturn.Id, existingSaleReturn.LocationId, sqlDataAccessTransaction);

            if (existingSaleReturn.PartyId is not null and > 0)
            {
                var location = await LocationData.LoadLocationByLedgerId(existingSaleReturn.PartyId.Value, sqlDataAccessTransaction);
                if (location is not null)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.PurchaseReturn), existingSaleReturn.Id, location.Id, sqlDataAccessTransaction);
            }
        }

        // Location Stock Update (positive quantity - product returns to location)
        foreach (var item in saleReturnDetails)
        {
            var id = await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                NetRate = item.NetRate,
                TransactionId = saleReturn.Id,
                Type = nameof(StockType.SaleReturn),
                TransactionNo = saleReturn.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
                LocationId = saleReturn.LocationId
            }, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save product stock for sale return location.");
        }

        // Party Location Stock Update (negative quantity - product leaves party's location)
        if (saleReturn.PartyId is not null and > 0)
        {
            var location = await LocationData.LoadLocationByLedgerId(saleReturn.PartyId.Value, sqlDataAccessTransaction);
            if (location is not null)
                foreach (var item in saleReturnDetails)
                {
                    var id = await ProductStockData.InsertProductStock(new()
                    {
                        Id = 0,
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity,
                        NetRate = item.NetRate,
                        TransactionId = saleReturn.Id,
                        Type = nameof(StockType.PurchaseReturn),
                        TransactionNo = saleReturn.TransactionNo,
                        TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
                        LocationId = location.Id
                    }, sqlDataAccessTransaction);

                    if (id <= 0)
                        throw new InvalidOperationException("Failed to save product stock for party location.");
                }
        }
    }

    private static async Task SaveRawMaterialStockByRecipe(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, SaleReturnModel existingSaleReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.SaleReturn), existingSaleReturn.Id, sqlDataAccessTransaction);

        if (saleReturn.LocationId != 1)
            return;

        foreach (var product in saleReturnDetails)
        {
            var recipe = await RecipeData.LoadRecipeByProduct(product.ProductId);
            var recipeItems = recipe is null ? [] : await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(TableNames.RecipeDetail, recipe.Id, sqlDataAccessTransaction);

            foreach (var recipeItem in recipeItems)
            {
                var id = await RawMaterialStockData.InsertRawMaterialStock(new()
                {
                    Id = 0,
                    RawMaterialId = recipeItem.RawMaterialId,
                    Quantity = recipeItem.Quantity * product.Quantity,
                    NetRate = product.NetRate / recipeItem.Quantity,
                    TransactionId = saleReturn.Id,
                    TransactionNo = saleReturn.TransactionNo,
                    Type = nameof(StockType.SaleReturn),
                    TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime)
                }, sqlDataAccessTransaction);

                if (id <= 0)
                    throw new InvalidOperationException("Failed to save raw material stock for sale return.");
            }
        }
    }

    private static async Task SaveAccounting(SaleReturnModel saleReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleReturnVoucher.Value), saleReturn.Id, saleReturn.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = saleReturn.LastModifiedBy;
                existingAccounting.LastModifiedAt = saleReturn.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = saleReturn.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }
        }

        var saleReturnOverview = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(ViewNames.SaleReturnOverview, saleReturn.Id, sqlDataAccessTransaction);
        if (saleReturnOverview is null)
            return;

        if (saleReturnOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<AccountingItemCartModel>();

        if (saleReturn.LocationId == 1)
        {
            if (saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card > 0)
            {
                var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
                accountingCart.Add(new()
                {
                    ReferenceId = saleReturnOverview.Id,
                    ReferenceType = nameof(ReferenceTypes.SaleReturn),
                    ReferenceNo = saleReturnOverview.TransactionNo,
                    LedgerId = int.Parse(cashLedger.Value),
                    Debit = null,
                    Credit = saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card,
                    Remarks = $"Cash Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
                });
            }

            if (saleReturnOverview.Credit > 0)
                accountingCart.Add(new()
                {
                    ReferenceId = saleReturnOverview.Id,
                    ReferenceType = nameof(ReferenceTypes.SaleReturn),
                    ReferenceNo = saleReturnOverview.TransactionNo,
                    LedgerId = saleReturnOverview.PartyId.Value,
                    Debit = null,
                    Credit = saleReturnOverview.Credit,
                    Remarks = $"Party Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
                });
        }
        else
        {
            if (saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card + saleReturnOverview.Credit > 0)
            {
                var ledger = await LedgerData.LoadLedgerByLocationId(saleReturnOverview.LocationId, sqlDataAccessTransaction);
                accountingCart.Add(new()
                {
                    ReferenceId = saleReturnOverview.Id,
                    ReferenceType = nameof(ReferenceTypes.SaleReturn),
                    ReferenceNo = saleReturnOverview.TransactionNo,
                    LedgerId = ledger.Id,
                    Debit = null,
                    Credit = saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card + saleReturnOverview.Credit,
                    Remarks = $"Location Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
                });
            }
        }

        if (saleReturnOverview.TotalAmount - saleReturnOverview.TotalExtraTaxAmount > 0)
        {
            var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = saleReturnOverview.Id,
                ReferenceType = nameof(ReferenceTypes.SaleReturn),
                ReferenceNo = saleReturnOverview.TransactionNo,
                LedgerId = int.Parse(saleLedger.Value),
                Debit = saleReturnOverview.TotalAmount - saleReturnOverview.TotalExtraTaxAmount,
                Credit = null,
                Remarks = $"Sale Return Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
            });
        }

        if (saleReturnOverview.TotalExtraTaxAmount > 0)
        {
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = saleReturnOverview.Id,
                ReferenceType = nameof(ReferenceTypes.SaleReturn),
                ReferenceNo = saleReturnOverview.TransactionNo,
                LedgerId = int.Parse(gstLedger.Value),
                Debit = saleReturnOverview.TotalExtraTaxAmount,
                Credit = null,
                Remarks = $"GST Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
            });
        }

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId, sqlDataAccessTransaction);
        var accounting = new AccountingModel
        {
            Id = 0,
            TransactionNo = "",
            CompanyId = saleReturnOverview.CompanyId,
            VoucherId = int.Parse(voucher.Value),
            ReferenceId = saleReturnOverview.Id,
            ReferenceNo = saleReturnOverview.TransactionNo,
            TransactionDateTime = saleReturnOverview.TransactionDateTime,
            FinancialYearId = saleReturnOverview.FinancialYearId,
            TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
            TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
            TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
            TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
            Remarks = saleReturnOverview.Remarks,
            CreatedBy = saleReturnOverview.CreatedBy,
            CreatedAt = saleReturnOverview.CreatedAt,
            CreatedFromPlatform = saleReturnOverview.CreatedFromPlatform,
            Status = true
        };

        await AccountingData.SaveTransaction(accounting, accountingCart, null, false, sqlDataAccessTransaction);
    }
}
