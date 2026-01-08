using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Data.Inventory.Purchase;

public static class PurchaseReturnData
{
    private static async Task<int> InsertPurchaseReturn(PurchaseReturnModel purchaseReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseReturn, purchaseReturn, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertPurchaseReturnDetail(PurchaseReturnDetailModel purchaseReturnDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseReturnDetail, purchaseReturnDetail, sqlDataAccessTransaction)).FirstOrDefault();

    private static List<PurchaseReturnDetailModel> ConvertCartToDetails(List<PurchaseReturnItemCartModel> cart, int masterId) =>
        [.. cart.Select(item => new PurchaseReturnDetailModel
        {
            Id = 0,
            MasterId = masterId,
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

    public static async Task DeleteTransaction(PurchaseReturnModel purchaseReturn)
    {

        using SqlDataAccessTransaction sqlDataAccessTransaction = new();
        try
        {
            sqlDataAccessTransaction.StartTransaction();

            await FinancialYearData.ValidateFinancialYear(purchaseReturn.TransactionDateTime, sqlDataAccessTransaction);

            purchaseReturn.Status = false;
            await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.PurchaseReturn), purchaseReturn.Id, sqlDataAccessTransaction);

            var purchaseReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(purchaseReturnVoucher.Value), purchaseReturn.Id, purchaseReturn.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = purchaseReturn.LastModifiedBy;
                existingAccounting.LastModifiedAt = purchaseReturn.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = purchaseReturn.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }

            sqlDataAccessTransaction.CommitTransaction();

            await PurchaseReturnNotify.Notify(purchaseReturn.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }

    }

    public static async Task RecoverTransaction(PurchaseReturnModel purchaseReturn)
    {
        purchaseReturn.Status = true;
        var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(TableNames.PurchaseReturnDetail, purchaseReturn.Id);

        await SaveTransaction(purchaseReturn, null, transactionDetails, false);

        await PurchaseReturnNotify.Notify(purchaseReturn.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(PurchaseReturnModel purchaseReturn, List<PurchaseReturnItemCartModel> cart, List<PurchaseReturnDetailModel> purchaseReturnDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = purchaseReturn.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await PurchaseReturnInvoiceExport.ExportInvoice(purchaseReturn.Id, InvoiceExportType.PDF);

            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                purchaseReturn.Id = await SaveTransaction(purchaseReturn, cart, purchaseReturnDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await PurchaseReturnNotify.Notify(purchaseReturn.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return purchaseReturn.Id;
        }

        if (update)
        {
            var existingPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturn.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingPurchaseReturn.TransactionDateTime, sqlDataAccessTransaction);
        }

        await FinancialYearData.ValidateFinancialYear(purchaseReturn.TransactionDateTime, sqlDataAccessTransaction);

        purchaseReturn.Id = await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);
        purchaseReturnDetails ??= ConvertCartToDetails(cart, purchaseReturn.Id);
        await SaveTransactionDetail(purchaseReturn, purchaseReturnDetails, update, sqlDataAccessTransaction);
        await SaveRawMaterialStock(purchaseReturn, purchaseReturnDetails, update, sqlDataAccessTransaction);
        await SaveAccounting(purchaseReturn, update, sqlDataAccessTransaction);

        return purchaseReturn.Id;
    }

    private static async Task SaveTransactionDetail(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (purchaseReturnDetails is null || purchaseReturnDetails.Count != purchaseReturn.TotalItems || purchaseReturnDetails.Sum(d => d.Quantity) != purchaseReturn.TotalQuantity)
            throw new InvalidOperationException("Purchase return details do not match the transaction summary.");

        if (purchaseReturnDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Purchase return detail items must be active.");

        if (update)
        {
            var existingPurchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(TableNames.PurchaseReturnDetail, purchaseReturn.Id, sqlDataAccessTransaction);
            foreach (var item in existingPurchaseDetails)
            {
                item.Status = false;
                await InsertPurchaseReturnDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in purchaseReturnDetails)
        {
            item.MasterId = purchaseReturn.Id;
            var id = await InsertPurchaseReturnDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save purchase return detail item.");
        }
    }

    private static async Task SaveRawMaterialStock(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.PurchaseReturn), purchaseReturn.Id, sqlDataAccessTransaction);

        foreach (var item in purchaseReturnDetails)
        {
            var insertedId = await RawMaterialStockData.InsertRawMaterialStock(new()
            {
                Id = 0,
                RawMaterialId = item.RawMaterialId,
                Quantity = -item.Quantity,
                NetRate = item.NetRate,
                TransactionId = purchaseReturn.Id,
                Type = nameof(StockType.PurchaseReturn),
                TransactionNo = purchaseReturn.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(purchaseReturn.TransactionDateTime)
            }, sqlDataAccessTransaction);

            if (insertedId <= 0)
                throw new InvalidOperationException("Failed to save raw material stock for purchase return.");
        }
    }

    private static async Task SaveAccounting(PurchaseReturnModel purchaseReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
        {
            var purchaseReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId, sqlDataAccessTransaction);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(purchaseReturnVoucher.Value), purchaseReturn.Id, purchaseReturn.TransactionNo, sqlDataAccessTransaction);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                existingAccounting.LastModifiedBy = purchaseReturn.LastModifiedBy;
                existingAccounting.LastModifiedAt = purchaseReturn.LastModifiedAt;
                existingAccounting.LastModifiedFromPlatform = purchaseReturn.LastModifiedFromPlatform;

                await AccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
            }
        }

        var purchaseReturnOverview = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(ViewNames.PurchaseReturnOverview, purchaseReturn.Id, sqlDataAccessTransaction);
        if (purchaseReturnOverview is null)
            return;

        if (purchaseReturnOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<AccountingItemCartModel>();

        if (purchaseReturnOverview.TotalAmount > 0)
            accountingCart.Add(new()
            {
                ReferenceId = purchaseReturnOverview.Id,
                ReferenceType = nameof(ReferenceTypes.PurchaseReturn),
                ReferenceNo = purchaseReturnOverview.TransactionNo,
                LedgerId = purchaseReturnOverview.PartyId,
                Debit = purchaseReturnOverview.TotalAmount,
                Credit = null,
                Remarks = $"Party Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
            });

        if (purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalExtraTaxAmount > 0)
        {
            var purchaseLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = purchaseReturnOverview.Id,
                ReferenceType = nameof(ReferenceTypes.PurchaseReturn),
                ReferenceNo = purchaseReturnOverview.TransactionNo,
                LedgerId = int.Parse(purchaseLedger.Value),
                Debit = null,
                Credit = purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalExtraTaxAmount,
                Remarks = $"Purchase Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
            });
        }

        if (purchaseReturnOverview.TotalExtraTaxAmount > 0)
        {
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
            accountingCart.Add(new()
            {
                ReferenceId = purchaseReturnOverview.Id,
                ReferenceType = nameof(ReferenceTypes.PurchaseReturn),
                ReferenceNo = purchaseReturnOverview.TransactionNo,
                LedgerId = int.Parse(gstLedger.Value),
                Debit = null,
                Credit = purchaseReturnOverview.TotalExtraTaxAmount,
                Remarks = $"GST Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
            });
        }

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId, sqlDataAccessTransaction);
        var accounting = new AccountingModel
        {
            Id = 0,
            TransactionNo = "",
            CompanyId = purchaseReturnOverview.CompanyId,
            VoucherId = int.Parse(voucher.Value),
            ReferenceId = purchaseReturnOverview.Id,
            ReferenceNo = purchaseReturnOverview.TransactionNo,
            TransactionDateTime = purchaseReturnOverview.TransactionDateTime,
            FinancialYearId = purchaseReturnOverview.FinancialYearId,
            TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
            TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
            TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
            TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
            Remarks = purchaseReturnOverview.Remarks,
            CreatedBy = purchaseReturnOverview.CreatedBy,
            CreatedAt = purchaseReturnOverview.CreatedAt,
            CreatedFromPlatform = purchaseReturnOverview.CreatedFromPlatform,
            Status = true
        };

        await AccountingData.SaveTransaction(accounting, accountingCart, null, false, sqlDataAccessTransaction);
    }
}