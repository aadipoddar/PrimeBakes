using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Purchase;

public static class PurchaseData
{
    private static async Task<int> InsertPurchase(PurchaseModel purchase) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchase, purchase)).FirstOrDefault();

    private static async Task<int> InsertPurchaseDetail(PurchaseDetailModel purchaseDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseDetail, purchaseDetail)).FirstOrDefault();

    public static async Task<List<RawMaterialModel>> LoadRawMaterialByPartyPurchaseDateTime(int PartyId, DateTime PurchaseDateTime, bool OnlyActive = true) =>
        await SqlDataAccess.LoadData<RawMaterialModel, dynamic>(StoredProcedureNames.LoadRawMaterialByPartyPurchaseDateTime, new { PartyId, PurchaseDateTime, OnlyActive });

    public static async Task DeleteTransaction(PurchaseModel purchase)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchase.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        purchase.Status = false;
        await InsertPurchase(purchase);
        await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Purchase), purchase.Id);

        var purchaseVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId);
        var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(purchaseVoucher.Value), purchase.Id, purchase.TransactionNo);

        if (existingAccounting is not null && existingAccounting.Id > 0)
        {
            existingAccounting.Status = false;
            existingAccounting.LastModifiedBy = purchase.LastModifiedBy;
            existingAccounting.LastModifiedAt = purchase.LastModifiedAt;
            existingAccounting.LastModifiedFromPlatform = purchase.LastModifiedFromPlatform;

            await AccountingData.DeleteTransaction(existingAccounting);
        }

        await SendNotification.PurchaseNotification(purchase.Id, NotifyType.Deleted);
    }

    public static async Task RecoverTransaction(PurchaseModel purchase)
    {
        var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(TableNames.PurchaseDetail, purchase.Id);
        List<PurchaseItemCartModel> purchaseItemCarts = [];

        foreach (var item in transactionDetails)
            purchaseItemCarts.Add(new()
            {
                ItemId = item.RawMaterialId,
                ItemName = "",
                UnitOfMeasurement = item.UnitOfMeasurement,
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
            });

        await SaveTransaction(purchase, purchaseItemCarts, false);
        await SendNotification.PurchaseNotification(purchase.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(PurchaseModel purchase, List<PurchaseItemCartModel> purchaseDetails, bool showNotification = true)
    {
        bool update = purchase.Id > 0;

        if (update)
        {
            var existingPurchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, purchase.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingPurchase.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");
        }

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchase.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        purchase.Id = await InsertPurchase(purchase);
        await SaveTransactionDetail(purchase, purchaseDetails, update);
        await SaveRawMaterialStock(purchase, purchaseDetails, update);
        await SaveAccounting(purchase, update);
        await UpdateRawMaterialRateAndUOMOnPurchase(purchaseDetails);

        if (showNotification)
            await SendNotification.PurchaseNotification(purchase.Id, update ? NotifyType.Updated : NotifyType.Created);

        return purchase.Id;
    }

    private static async Task SaveTransactionDetail(PurchaseModel purchase, List<PurchaseItemCartModel> purchaseDetails, bool update)
    {
        if (update)
        {
            var existingPurchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(TableNames.PurchaseDetail, purchase.Id);
            foreach (var item in existingPurchaseDetails)
            {
                item.Status = false;
                await InsertPurchaseDetail(item);
            }
        }

        foreach (var item in purchaseDetails)
            await InsertPurchaseDetail(new()
            {
                Id = 0,
                MasterId = purchase.Id,
                RawMaterialId = item.ItemId,
                Quantity = item.Quantity,
                UnitOfMeasurement = item.UnitOfMeasurement,
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

    private static async Task SaveRawMaterialStock(PurchaseModel purchase, List<PurchaseItemCartModel> cart, bool update)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Purchase), purchase.Id);

        foreach (var item in cart)
            await RawMaterialStockData.InsertRawMaterialStock(new()
            {
                Id = 0,
                RawMaterialId = item.ItemId,
                Quantity = item.Quantity,
                NetRate = item.NetRate,
                Type = nameof(StockType.Purchase),
                TransactionId = purchase.Id,
                TransactionNo = purchase.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(purchase.TransactionDateTime)
            });
    }

    private static async Task SaveAccounting(PurchaseModel purchase, bool update)
    {
        if (update)
        {
            var purchaseVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(purchaseVoucher.Value), purchase.Id, purchase.TransactionNo);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = purchase.LastModifiedBy;
                existingAccounting.LastModifiedAt = purchase.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = purchase.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting);
            }
        }

        var purchaseOverview = await CommonData.LoadTableDataById<PurchaseOverviewModel>(ViewNames.PurchaseOverview, purchase.Id);
        if (purchaseOverview is null)
            return;

        if (purchaseOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<AccountingItemCartModel>();

        if (purchaseOverview.TotalAmount > 0)
            accountingCart.Add(new()
            {
                ReferenceId = purchaseOverview.Id,
                ReferenceType = nameof(ReferenceTypes.Purchase),
                ReferenceNo = purchaseOverview.TransactionNo,
                LedgerId = purchaseOverview.PartyId,
                Debit = null,
                Credit = purchaseOverview.TotalAmount,
                Remarks = $"Party Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
            });

        if (purchaseOverview.TotalAmount - purchaseOverview.TotalExtraTaxAmount > 0)
        {
            var purchaseLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId);
            accountingCart.Add(new()
            {
                ReferenceId = purchaseOverview.Id,
                ReferenceType = nameof(ReferenceTypes.Purchase),
                ReferenceNo = purchaseOverview.TransactionNo,
                LedgerId = int.Parse(purchaseLedger.Value),
                Debit = purchaseOverview.TotalAmount - purchaseOverview.TotalExtraTaxAmount,
                Credit = null,
                Remarks = $"Purchase Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
            });
        }

        if (purchaseOverview.TotalExtraTaxAmount > 0)
        {
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
            accountingCart.Add(new()
            {
                ReferenceId = purchaseOverview.Id,
                ReferenceType = nameof(ReferenceTypes.Purchase),
                ReferenceNo = purchaseOverview.TransactionNo,
                LedgerId = int.Parse(gstLedger.Value),
                Debit = purchaseOverview.TotalExtraTaxAmount,
                Credit = null,
                Remarks = $"GST Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
            });
        }

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId);
        var accounting = new AccountingModel
        {
            Id = 0,
            TransactionNo = "",
            CompanyId = purchaseOverview.CompanyId,
            VoucherId = int.Parse(voucher.Value),
            ReferenceId = purchaseOverview.Id,
            ReferenceNo = purchaseOverview.TransactionNo,
            TransactionDateTime = purchaseOverview.TransactionDateTime,
            FinancialYearId = purchaseOverview.FinancialYearId,
            TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
            TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
            TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
            TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
            Remarks = purchaseOverview.Remarks,
            CreatedBy = purchaseOverview.CreatedBy,
            CreatedAt = purchaseOverview.CreatedAt,
            CreatedFromPlatform = purchaseOverview.CreatedFromPlatform,
            Status = true
        };

        await AccountingData.SaveTransaction(accounting, accountingCart);
    }

    private static async Task UpdateRawMaterialRateAndUOMOnPurchase(List<PurchaseItemCartModel> purchaseDetails)
    {
        var isUpdateItemRateOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterRateOnPurchase)).Value);
        var isUpdateItemUOMOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterUOMOnPurchase)).Value);

        var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        foreach (var purchaseItem in purchaseDetails)
        {
            var rawMaterial = rawMaterials.FirstOrDefault(i => i.Id == purchaseItem.ItemId);
            if (rawMaterial is not null)
            {
                if (isUpdateItemRateOnPurchaseEnabled)
                    rawMaterial.Rate = purchaseItem.Rate;
                if (isUpdateItemUOMOnPurchaseEnabled)
                    rawMaterial.UnitOfMeasurement = purchaseItem.UnitOfMeasurement;

                await RawMaterialData.InsertRawMaterial(rawMaterial);
            }
        }
    }
}