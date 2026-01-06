using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Sales.Order;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Sale;

public static class SaleData
{
    private static async Task<int> InsertSale(SaleModel sale) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSale, sale)).FirstOrDefault();

    private static async Task<int> InsertSaleDetail(SaleDetailModel saleDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleDetail, saleDetail)).FirstOrDefault();

    public static async Task DeleteTransaction(SaleModel sale)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, sale.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        if (sale.OrderId is not null && sale.OrderId > 0)
            await OrderData.UnlinkOrderFromSale(sale);

        sale.OrderId = null;
        sale.Status = false;
        await InsertSale(sale);

        await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Sale), sale.Id, sale.LocationId);
        await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Sale), sale.Id);

        if (sale.PartyId is not null and > 0)
        {
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
            if (party.LocationId is > 0)
                await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Purchase), sale.Id, party.LocationId.Value);
        }

        var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
        var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleVoucher.Value), sale.Id, sale.TransactionNo);
        if (existingAccounting is not null && existingAccounting.Id > 0)
        {
            existingAccounting.Status = false;
            existingAccounting.LastModifiedBy = sale.LastModifiedBy;
            existingAccounting.LastModifiedAt = sale.LastModifiedAt;
            existingAccounting.LastModifiedFromPlatform = sale.LastModifiedFromPlatform;

            await AccountingData.DeleteTransaction(existingAccounting);
        }

        await SendNotification.SaleNotification(sale.Id, NotifyType.Deleted);
    }

    public static async Task RecoverTransaction(SaleModel sale)
    {
        var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);
        List<SaleItemCartModel> transactionItemCarts = [];
        transactionItemCarts.AddRange(transactionDetails.Select(item => new SaleItemCartModel()
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

        await SaveTransaction(sale, transactionItemCarts, false);
        await SendNotification.SaleNotification(sale.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(SaleModel sale, List<SaleItemCartModel> saleDetails, bool showNotification = true)
    {
        bool update = sale.Id > 0;
        var existingSale = sale;

        if (update)
        {
            existingSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, sale.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingSale.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            sale.TransactionNo = existingSale.TransactionNo;
        }
        else
            sale.TransactionNo = await GenerateCodes.GenerateSaleTransactionNo(sale);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, sale.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        sale.Id = await InsertSale(sale);
        await SaveTransactionDetail(sale, saleDetails, update);
        await SaveProductStock(sale, saleDetails, existingSale, update);
        await SaveRawMaterialStockByRecipe(sale, saleDetails, existingSale, update);
        await UpdateOrder(sale, existingSale, update);
        await SaveAccounting(sale, update);

        if (showNotification)
            await SendNotification.SaleNotification(sale.Id, update ? NotifyType.Updated : NotifyType.Created);

        return sale.Id;
    }

    private static async Task SaveTransactionDetail(SaleModel sale, List<SaleItemCartModel> saleDetails, bool update)
    {
        if (update)
        {
            var existingSaleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);
            foreach (var item in existingSaleDetails)
            {
                item.Status = false;
                await InsertSaleDetail(item);
            }
        }

        foreach (var item in saleDetails)
            await InsertSaleDetail(new()
            {
                Id = 0,
                MasterId = sale.Id,
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

    private static async Task SaveProductStock(SaleModel sale, List<SaleItemCartModel> cart, SaleModel existingSale, bool update)
    {
        if (update)
        {
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Sale), existingSale.Id, existingSale.LocationId);

            if (existingSale.PartyId is not null and > 0)
            {
                var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, existingSale.PartyId.Value);
                if (party.LocationId is > 0)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Purchase), existingSale.Id, party.LocationId.Value);
            }
        }

        // Location Stock Update
        foreach (var item in cart)
            await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ItemId,
                Quantity = -item.Quantity,
                NetRate = item.NetRate,
                TransactionId = sale.Id,
                Type = nameof(StockType.Sale),
                TransactionNo = sale.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime),
                LocationId = sale.LocationId
            });

        // Party Location Stock Update
        if (sale.PartyId is not null and > 0)
        {
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
            if (party.LocationId is > 0)
                foreach (var item in cart)
                    await ProductStockData.InsertProductStock(new()
                    {
                        Id = 0,
                        ProductId = item.ItemId,
                        Quantity = item.Quantity,
                        NetRate = item.NetRate,
                        TransactionId = sale.Id,
                        Type = nameof(StockType.Purchase),
                        TransactionNo = sale.TransactionNo,
                        TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime),
                        LocationId = party.LocationId.Value
                    });
        }
    }

    private static async Task SaveRawMaterialStockByRecipe(SaleModel sale, List<SaleItemCartModel> cart, SaleModel existingSale, bool update)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Sale), existingSale.Id);

        if (sale.LocationId != 1)
            return;

        foreach (var product in cart)
        {
            var recipe = await RecipeData.LoadRecipeByProduct(product.ItemId);
            var recipeItems = recipe is null ? [] : await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(TableNames.RecipeDetail, recipe.Id);

            foreach (var recipeItem in recipeItems)
                await RawMaterialStockData.InsertRawMaterialStock(new()
                {
                    Id = 0,
                    RawMaterialId = recipeItem.RawMaterialId,
                    Quantity = -recipeItem.Quantity * product.Quantity,
                    NetRate = product.NetRate / recipeItem.Quantity,
                    TransactionId = sale.Id,
                    TransactionNo = sale.TransactionNo,
                    Type = nameof(StockType.Sale),
                    TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime)
                });
        }
    }

    private static async Task UpdateOrder(SaleModel sale, SaleModel previousSale, bool update)
    {
        if (update)
            await OrderData.UnlinkOrderFromSale(previousSale);

        if (sale.OrderId is not null)
            await OrderData.LinkOrderToSale(sale);
    }

    private static async Task SaveAccounting(SaleModel sale, bool update)
    {
        if (update)
        {
            var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleVoucher.Value), sale.Id, sale.TransactionNo);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = sale.LastModifiedBy;
                existingAccounting.LastModifiedAt = sale.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = sale.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting);
            }
        }

        var saleOverview = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, sale.Id);
        if (saleOverview is null)
            return;

        if (saleOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<AccountingItemCartModel>();

        if (sale.LocationId == 1)
        {
            if (saleOverview.Cash + saleOverview.UPI + saleOverview.Card > 0)
            {
                var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId);
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
                var ledger = await LedgerData.LoadLedgerByLocation(sale.LocationId);
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
            var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId);
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
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
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

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
        var accounting = new AccountingModel
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

        await AccountingData.SaveTransaction(accounting, accountingCart);
    }
}
