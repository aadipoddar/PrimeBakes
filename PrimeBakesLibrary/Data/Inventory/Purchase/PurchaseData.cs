using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Data.Inventory.Purchase;

public static class PurchaseData
{
    private static async Task<int> InsertPurchase(PurchaseModel purchase, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchase, purchase, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertPurchaseDetail(PurchaseDetailModel purchaseDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseDetail, purchaseDetail, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<RawMaterialModel>> LoadRawMaterialByPartyPurchaseDateTime(int PartyId, DateTime PurchaseDateTime, bool OnlyActive = true) =>
        await SqlDataAccess.LoadData<RawMaterialModel, dynamic>(StoredProcedureNames.LoadRawMaterialByPartyPurchaseDateTime, new { PartyId, PurchaseDateTime, OnlyActive });

    public static List<PurchaseDetailModel> ConvertCartToDetails(List<PurchaseItemCartModel> cart, int purchaseId) =>
        [.. cart.Select(item => new PurchaseDetailModel
        {
            Id = 0,
            MasterId = purchaseId,
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
        })];

    public static async Task DeleteTransaction(PurchaseModel purchase)
    {
        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            await FinancialYearData.ValidateFinancialYear(purchase.TransactionDateTime, sqlDataAccessTransaction);

            purchase.Status = false;
            await InsertPurchase(purchase, sqlDataAccessTransaction);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Purchase), purchase.Id, sqlDataAccessTransaction);

            var purchaseVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(purchaseVoucher.Value), purchase.Id, purchase.TransactionNo, sqlDataAccessTransaction);

            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = purchase.LastModifiedBy;
                existingAccounting.LastModifiedAt = purchase.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = purchase.LastModifiedFromPlatform;

                await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }

            sqlDataAccessTransaction.CommitTransaction();

            await PurchaseNotify.Notify(purchase.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }

    }

    public static async Task RecoverTransaction(PurchaseModel purchase)
    {
        purchase.Status = true;
        var purchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(TableNames.PurchaseDetail, purchase.Id);

        await SaveTransaction(purchase, null, purchaseDetails, false);

        await PurchaseNotify.Notify(purchase.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(PurchaseModel purchase, List<PurchaseItemCartModel> cart, List<PurchaseDetailModel> purchaseDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = purchase.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await PurchaseInvoiceExport.ExportInvoice(purchase.Id, InvoiceExportType.PDF);

            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                purchase.Id = await SaveTransaction(purchase, cart, purchaseDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await PurchaseNotify.Notify(purchase.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return purchase.Id;
        }

        if (update)
        {
            var existingPurchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, purchase.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingPurchase.TransactionDateTime, sqlDataAccessTransaction);
        }

        await FinancialYearData.ValidateFinancialYear(purchase.TransactionDateTime, sqlDataAccessTransaction);

        purchase.Id = await InsertPurchase(purchase, sqlDataAccessTransaction);
        purchaseDetails ??= ConvertCartToDetails(cart, purchase.Id);
        await SaveTransactionDetail(purchase, purchaseDetails, update, sqlDataAccessTransaction);
        await SaveRawMaterialStock(purchase, purchaseDetails, update, sqlDataAccessTransaction);
        await SaveAccounting(purchase, update, sqlDataAccessTransaction);
        await UpdateRawMaterialRateAndUOMOnPurchase(purchaseDetails, sqlDataAccessTransaction);

        return purchase.Id;
    }

    private static async Task SaveTransactionDetail(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (purchaseDetails is null || purchaseDetails.Count != purchase.TotalItems || purchaseDetails.Sum(d => d.Quantity) != purchase.TotalQuantity)
            throw new InvalidOperationException("Purchase details do not match the transaction summary.");

        if (purchaseDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Purchase detail items must be active.");

        if (update)
        {
            var existingPurchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(TableNames.PurchaseDetail, purchase.Id, sqlDataAccessTransaction);
            foreach (var item in existingPurchaseDetails)
            {
                item.Status = false;
                await InsertPurchaseDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in purchaseDetails)
        {
            item.MasterId = purchase.Id;
            var id = await InsertPurchaseDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save purchase detail item.");
        }
    }

    private static async Task SaveRawMaterialStock(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Purchase), purchase.Id, sqlDataAccessTransaction);

        foreach (var item in purchaseDetails)
        {
            var id = await RawMaterialStockData.InsertRawMaterialStock(new()
            {
                Id = 0,
                RawMaterialId = item.RawMaterialId,
                Quantity = item.Quantity,
                NetRate = item.NetRate,
                Type = nameof(StockType.Purchase),
                TransactionId = purchase.Id,
                TransactionNo = purchase.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(purchase.TransactionDateTime)
            }, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save raw material stock entry.");
        }
    }

    private static async Task SaveAccounting(PurchaseModel purchase, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            var purchaseVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(purchaseVoucher.Value), purchase.Id, purchase.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = purchase.LastModifiedBy;
                existingAccounting.LastModifiedAt = purchase.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = purchase.LastModifiedFromPlatform;

                await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }
        }

        var purchaseOverview = await CommonData.LoadTableDataById<PurchaseOverviewModel>(ViewNames.PurchaseOverview, purchase.Id, sqlDataAccessTransaction);
        if (purchaseOverview is null)
            return;

        if (purchaseOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<FinancialAccountingItemCartModel>();

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
            var purchaseLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId, sqlDataAccessTransaction);
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
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
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

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId, sqlDataAccessTransaction);
        var accounting = new FinancialAccountingModel
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

        await FinancialAccountingData.SaveTransaction(accounting, accountingCart, null, false, sqlDataAccessTransaction);
    }

    private static async Task UpdateRawMaterialRateAndUOMOnPurchase(List<PurchaseDetailModel> purchaseDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        var isUpdateItemRateOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterRateOnPurchase, sqlDataAccessTransaction)).Value);
        var isUpdateItemUOMOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterUOMOnPurchase, sqlDataAccessTransaction)).Value);

        if (!isUpdateItemRateOnPurchaseEnabled && !isUpdateItemUOMOnPurchaseEnabled)
            return;

        var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        foreach (var purchaseItem in purchaseDetails)
        {
            var rawMaterial = rawMaterials.FirstOrDefault(i => i.Id == purchaseItem.RawMaterialId);
            if (rawMaterial is not null)
            {
                if (isUpdateItemRateOnPurchaseEnabled)
                    rawMaterial.Rate = purchaseItem.Rate;
                if (isUpdateItemUOMOnPurchaseEnabled)
                    rawMaterial.UnitOfMeasurement = purchaseItem.UnitOfMeasurement;

                await RawMaterialData.InsertRawMaterial(rawMaterial, sqlDataAccessTransaction);
            }
        }
    }
}