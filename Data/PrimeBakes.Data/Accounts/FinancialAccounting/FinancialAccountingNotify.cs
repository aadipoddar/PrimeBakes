using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Exports.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Accounts.FinancialAccounting;

internal static class FinancialAccountingNotify
{
	internal static async Task Notify(int transactionId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		if (type != NotifyType.Created)
			await NotifyByMail(transactionId, type, previousInvoice);
	}

	private static async Task NotifyByMail(int transactionId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var transaction = await CommonData.LoadTableDataById<FinancialAccountingOverviewModel>(AccountNames.FinancialAccountingOverview, transactionId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Accounting",
			TransactionNo = transaction.TransactionNo,
			Action = type,
			LocationName = transaction.VoucherName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = transaction.TransactionNo,
				["Voucher"] = transaction.VoucherName,
				["Transaction Date"] = transaction.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Ledgers"] = transaction.TotalLedgers.ToString(),
				["Debit Ledgers"] = transaction.TotalDebitLedgers.ToString(),
				["Credit Ledgers"] = transaction.TotalCreditLedgers.ToString(),
				["Total Debit"] = transaction.TotalDebitAmount.FormatIndianCurrency(),
				["Total Credit"] = transaction.TotalCreditAmount.FormatIndianCurrency(),
				["Total Amount"] = transaction.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = transaction.LastModifiedByUserName ?? transaction.CreatedByName
			},
			Remarks = transaction.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(AccountNames.FinancialAccounting, transaction.TransactionNo)).RecordValue : null
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = FinancialAccountingInvoiceExport.ExportInvoice(await FinancialAccountingData.LoadInvoiceBundle(transactionId), InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = FinancialAccountingInvoiceExport.ExportInvoice(await FinancialAccountingData.LoadInvoiceBundle(transactionId), InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
