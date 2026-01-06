
using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Sale;

public static class SaleReturnData
{
    private static async Task<int> InsertSaleReturn(SaleReturnModel saleReturn) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturn, saleReturn)).FirstOrDefault();

    private static async Task<int> InsertSaleReturnDetail(SaleReturnDetailModel saleReturnDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturnDetail, saleReturnDetail)).FirstOrDefault();

    public static async Task DeleteTransaction(SaleReturnModel saleReturn)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        saleReturn.Status = false;
        await InsertSaleReturn(saleReturn);

        await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.SaleReturn), saleReturn.Id, saleReturn.LocationId);
        await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.SaleReturn), saleReturn.Id);

        if (saleReturn.PartyId is not null and > 0)
        {
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
            if (party.LocationId is > 0)
                await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.PurchaseReturn), saleReturn.Id, party.LocationId.Value);
        }

        var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
        var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleReturnVoucher.Value), saleReturn.Id, saleReturn.TransactionNo);
        if (existingAccounting is not null && existingAccounting.Id > 0)
        {
            existingAccounting.Status = false;
            existingAccounting.LastModifiedBy = saleReturn.LastModifiedBy;
            existingAccounting.LastModifiedAt = saleReturn.LastModifiedAt;
            existingAccounting.LastModifiedFromPlatform = saleReturn.LastModifiedFromPlatform;

            await AccountingData.DeleteTransaction(existingAccounting);
        }

        await SendNotification.SaleReturnNotification(saleReturn.Id, NotifyType.Deleted);
    }

    public static async Task RecoverTransaction(SaleReturnModel saleReturn)
    {
        var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturn.Id);
        List<SaleReturnItemCartModel> transactionItemCarts = [];
        transactionItemCarts.AddRange(transactionDetails.Select(item => new SaleReturnItemCartModel()
        {
            ItemId = item.ProductId,
            ItemName = "",
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
            InclusiveTax = item.InclusiveTax,
            TotalTaxAmount = item.TotalTaxAmount,
            Total = item.Total,
            NetRate = item.NetRate,
            Remarks = item.Remarks
        }));

        await SaveTransaction(saleReturn, transactionItemCarts, false);
        await SendNotification.SaleReturnNotification(saleReturn.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> saleReturnDetails, bool showNotification = true)
    {
        bool update = saleReturn.Id > 0;
        var existingSaleReturn = saleReturn;

        if (update)
        {
            existingSaleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturn.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingSaleReturn.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            saleReturn.TransactionNo = existingSaleReturn.TransactionNo;
        }
        else
            saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(saleReturn);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        saleReturn.Id = await InsertSaleReturn(saleReturn);
        await SaveTransactionDetail(saleReturn, saleReturnDetails, update);
        await SaveProductStock(saleReturn, saleReturnDetails, existingSaleReturn, update);
        await SaveRawMaterialStockByRecipe(saleReturn, saleReturnDetails, existingSaleReturn, update);
        await SaveAccounting(saleReturn, update);

        if (showNotification)
            await SendNotification.SaleReturnNotification(saleReturn.Id, update ? NotifyType.Updated : NotifyType.Created);

        return saleReturn.Id;
    }

    private static async Task SaveTransactionDetail(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> saleReturnDetails, bool update)
    {
        if (update)
        {
            var existingTransactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturn.Id);
            foreach (var item in existingTransactionDetails)
            {
                item.Status = false;
                await InsertSaleReturnDetail(item);
            }
        }

        foreach (var item in saleReturnDetails)
            await InsertSaleReturnDetail(new()
            {
                Id = 0,
                MasterId = saleReturn.Id,
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
            });
    }

    private static async Task SaveProductStock(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> cart, SaleReturnModel existingSaleReturn, bool update)
    {
        if (update)
        {
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.SaleReturn), existingSaleReturn.Id, existingSaleReturn.LocationId);

            if (existingSaleReturn.PartyId is not null and > 0)
            {
                var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, existingSaleReturn.PartyId.Value);
                if (party.LocationId is > 0)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.PurchaseReturn), existingSaleReturn.Id, party.LocationId.Value);
            }
        }

        // Location Stock Update
        foreach (var item in cart)
            await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ItemId,
                Quantity = item.Quantity,
                NetRate = item.NetRate,
                TransactionId = saleReturn.Id,
                Type = nameof(StockType.SaleReturn),
                TransactionNo = saleReturn.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
                LocationId = saleReturn.LocationId
            });

        // Party Location Stock Update
        if (saleReturn.PartyId is not null and > 0)
        {
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
            if (party.LocationId is > 0)
                foreach (var item in cart)
                    await ProductStockData.InsertProductStock(new()
                    {
                        Id = 0,
                        ProductId = item.ItemId,
                        Quantity = -item.Quantity,
                        NetRate = item.NetRate,
                        TransactionId = saleReturn.Id,
                        Type = nameof(StockType.PurchaseReturn),
                        TransactionNo = saleReturn.TransactionNo,
                        TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
                        LocationId = party.LocationId.Value
                    });
        }
    }

    private static async Task SaveRawMaterialStockByRecipe(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> cart, SaleReturnModel existingSaleReturn, bool update)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.SaleReturn), existingSaleReturn.Id);

        if (saleReturn.LocationId != 1)
            return;

        foreach (var product in cart)
        {
            var recipe = await RecipeData.LoadRecipeByProduct(product.ItemId);
            var recipeItems = recipe is null ? [] : await RecipeData.LoadRecipeDetailByRecipe(recipe.Id);

            foreach (var recipeItem in recipeItems)
                await RawMaterialStockData.InsertRawMaterialStock(new()
                {
                    Id = 0,
                    RawMaterialId = recipeItem.RawMaterialId,
                    Quantity = recipeItem.Quantity * product.Quantity,
                    NetRate = product.NetRate / recipeItem.Quantity,
                    TransactionId = saleReturn.Id,
                    TransactionNo = saleReturn.TransactionNo,
                    Type = nameof(StockType.SaleReturn),
                    TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime)
                });
        }
    }

    private static async Task SaveAccounting(SaleReturnModel saleReturn, bool update)
    {
        if (update)
        {
            var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleReturnVoucher.Value), saleReturn.Id, saleReturn.TransactionNo);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = saleReturn.LastModifiedBy;
                existingAccounting.LastModifiedAt = saleReturn.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = saleReturn.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting);
            }
        }

        var saleReturnOverview = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(ViewNames.SaleReturnOverview, saleReturn.Id);
        if (saleReturnOverview is null)
            return;

        if (saleReturnOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<AccountingItemCartModel>();

        if (saleReturn.LocationId == 1)
        {
            if (saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card > 0)
            {
                var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId);
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
                var ledger = await LedgerData.LoadLedgerByLocation(saleReturnOverview.LocationId);
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
            var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId);
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
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
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

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
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

        await AccountingData.SaveTransaction(accounting, accountingCart);
    }
}
