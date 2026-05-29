using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;

using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Utils.ExportUtils;
using PrimeBakesLibrary.Utils.MailUtils;
using PrimeBakesLibrary.Utils.NotificationUtils;

namespace PrimeBakesLibrary.Accounts.FinancialAccounting.Exports;

internal static class FinancialAccountingNotify
{
	internal static async Task Notify(int accountingId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		if (type == NotifyType.Created)
			return;

		await NotifyByMail(accountingId, type, previousInvoice);
	}

	private static async Task NotifyByMail(int accountingId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var accounting = await CommonData.LoadTableDataById<FinancialAccountingOverviewModel>(AccountNames.FinancialAccountingOverview, accountingId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Accounting",
			TransactionNo = accounting.TransactionNo,
			Action = type,
			LocationName = accounting.VoucherName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = accounting.TransactionNo,
				["Voucher"] = accounting.VoucherName,
				["Transaction Date"] = accounting.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Ledgers"] = accounting.TotalLedgers.ToString(),
				["Debit Ledgers"] = accounting.TotalDebitLedgers.ToString(),
				["Credit Ledgers"] = accounting.TotalCreditLedgers.ToString(),
				["Total Debit"] = accounting.TotalDebitAmount.FormatIndianCurrency(),
				["Total Credit"] = accounting.TotalCreditAmount.FormatIndianCurrency(),
				["Total Amount"] = accounting.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = accounting.LastModifiedByUserName ?? accounting.CreatedByName
			},
			Remarks = accounting.Remarks
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = await FinancialAccountingInvoiceExport.ExportInvoice(accountingId, InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = await FinancialAccountingInvoiceExport.ExportInvoice(accountingId, InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
