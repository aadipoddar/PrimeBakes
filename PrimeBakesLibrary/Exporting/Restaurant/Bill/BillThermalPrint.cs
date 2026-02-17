using System.Text;

using NumericWordsConversion;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Masters;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

public class BillThermalPrint
{
	public static async Task<StringBuilder> GenerateThermalBill(int billId)
	{
		var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);
		StringBuilder content = new();

		await AddHeader(content);
		await AddBillDetails(bill, content);
		await AddItemDetails(bill, content);
		await AddTotalDetails(bill, content);
		await AddPaymentModes(bill, content);
		await AddFooter(bill, content);

		return content;
	}

	private static async Task AddHeader(StringBuilder content)
	{
		var primaryCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, int.Parse(primaryCompanyId.Value));

		content.AppendLine("<div class='header'>");
		content.AppendLine("<div class='company-name'>PRIME BAKES</div>");

		if (!string.IsNullOrEmpty(company.Alias))
			content.AppendLine($"<div class='header-line'>{company.Alias}</div>");

		if (!string.IsNullOrEmpty(company.GSTNo))
			content.AppendLine($"<div class='header-line'>GSTNO: {company.GSTNo}</div>");

		if (!string.IsNullOrEmpty(company.Address))
			content.AppendLine($"<div class='header-line'>{company.Address}</div>");

		if (!string.IsNullOrEmpty(company.Email))
			content.AppendLine($"<div class='header-line'>Email: {company.Email}</div>");

		if (!string.IsNullOrEmpty(company.Phone))
			content.AppendLine($"<div class='header-line'>Phone: {company.Phone}</div>");

		content.AppendLine("</div>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddBillDetails(BillModel bill, StringBuilder content)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, bill.LocationId);
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, bill.DiningTableId);

		content.AppendLine("<div class='bill-details'>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Outlet:</span> <span class='detail-value'>{location?.Name}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Bill No:</span> <span class='detail-value'>{bill.TransactionNo}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Date:</span> <span class='detail-value'>{bill.TransactionDateTime:dd/MM/yy hh:mm tt}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Table :</span> <span class='detail-value'>{table?.Name} | Pax: {bill.TotalPeople}</span></div>");

		content.AppendLine("</div>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddItemDetails(BillModel bill, StringBuilder content)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);

		content.AppendLine("<table class='items-table'>");
		content.AppendLine("<thead>");
		content.AppendLine("<tr class='table-header'>");
		content.AppendLine("<th align='left'>Item</th>");
		content.AppendLine("<th align='center'>Qty</th>");
		content.AppendLine("<th align='right'>Rate</th>");
		content.AppendLine("<th align='right'>Amt</th>");
		content.AppendLine("</tr>");
		content.AppendLine("</thead>");
		content.AppendLine("<tbody>");

		var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		foreach (var item in billDetails)
		{
			content.AppendLine("<tr class='table-row'>");
			content.AppendLine($"<td align='left'>{products.FirstOrDefault(p => p.Id == item.ProductId)?.Name}</td>");
			content.AppendLine($"<td align='center'>{item.Quantity.FormatSmartDecimal()}</td>");
			content.AppendLine($"<td align='right'>{item.Rate.FormatSmartDecimal()}</td>");
			content.AppendLine($"<td align='right'>{item.BaseTotal.FormatSmartDecimal()}</td>");
			content.AppendLine("</tr>");
		}

		content.AppendLine("</tbody>");
		content.AppendLine("</table>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddTotalDetails(BillModel bill, StringBuilder content)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);

		content.AppendLine("<table class='summary-table'>");

		if (billDetails.Where(s => !s.InclusiveTax).Sum(s => s.CGSTAmount) > 0)
			content.AppendLine($"<tr><td class='summary-label'>CGST:</td><td align='right' class='summary-value'>{billDetails.Sum(s => s.CGSTAmount).FormatIndianCurrency()}</td></tr>");

		if (billDetails.Where(s => !s.InclusiveTax).Sum(s => s.SGSTAmount) > 0)
			content.AppendLine($"<tr><td class='summary-label'>SGST:</td><td align='right' class='summary-value'>{billDetails.Sum(s => s.SGSTAmount).FormatIndianCurrency()}</td></tr>");

		if (billDetails.Where(s => !s.InclusiveTax).Sum(s => s.IGSTAmount) > 0)
			content.AppendLine($"<tr><td class='summary-label'>IGST:</td><td align='right' class='summary-value'>{billDetails.Sum(s => s.IGSTAmount).FormatIndianCurrency()}</td></tr>");

		content.AppendLine($"<tr><td class='summary-label'>Sub Total:</td><td align='right' class='summary-value'>{bill.TotalAfterTax.FormatIndianCurrency()}</td></tr>");

		if (bill.DiscountPercent > 0)
			content.AppendLine($"<tr><td class='summary-label'>Discount ({bill.DiscountPercent}%):</td><td align='right' class='summary-value'>{bill.DiscountAmount.FormatIndianCurrency()}</td></tr>");

		if (bill.ServiceChargePercent > 0)
			content.AppendLine($"<tr><td class='summary-label'>Service ({bill.ServiceChargePercent}%):</td><td align='right' class='summary-value'>{bill.ServiceChargeAmount.FormatIndianCurrency()}</td></tr>");

		if (bill.RoundOffAmount != 0)
			content.AppendLine($"<tr><td class='summary-label'>Round Off:</td><td align='right' class='summary-value'>{bill.RoundOffAmount.FormatIndianCurrency()}</td></tr>");

		content.AppendLine("</table>");
		content.AppendLine("<div class='bold-separator'></div>");

		content.AppendLine("<table class='grand-total'>");
		content.AppendLine($"<tr><td class='grand-total-label'>Grand Total:</td><td align='right' class='grand-total-value'>{bill.TotalAmount.FormatIndianCurrency()}</td></tr>");
		content.AppendLine("</table>");

		CurrencyWordsConverter numericWords = new(new()
		{
			Culture = Culture.Hindi,
			OutputFormat = OutputFormat.English
		});

		string amountInWords = numericWords.ToWords(Math.Round(bill.TotalAmount));
		if (string.IsNullOrEmpty(amountInWords))
			amountInWords = "Zero";

		amountInWords += " Rupees Only";

		content.AppendLine("<div class='amount-words'>" + amountInWords + "</div>");
	}

	private static Task AddPaymentModes(BillModel bill, StringBuilder content)
	{
		bool hasPayments = bill.Cash > 0 || bill.Card > 0 || bill.UPI > 0 || bill.Credit > 0;

		if (!hasPayments)
			return Task.CompletedTask;

		content.AppendLine("<table class='summary-table'>");

		if (bill.Cash > 0)
			content.AppendLine($"<tr><td class='summary-label'>Cash:</td><td align='right' class='summary-value'>{bill.Cash.FormatIndianCurrency()}</td></tr>");

		if (bill.Card > 0)
			content.AppendLine($"<tr><td class='summary-label'>Card:</td><td align='right' class='summary-value'>{bill.Card.FormatIndianCurrency()}</td></tr>");

		if (bill.UPI > 0)
			content.AppendLine($"<tr><td class='summary-label'>UPI:</td><td align='right' class='summary-value'>{bill.UPI.FormatIndianCurrency()}</td></tr>");

		if (bill.Credit > 0)
			content.AppendLine($"<tr><td class='summary-label'>Credit:</td><td align='right' class='summary-value'>{bill.Credit.FormatIndianCurrency()}</td></tr>");

		content.AppendLine("</table>");

		return Task.CompletedTask;
	}

	private static async Task AddFooter(BillModel bill, StringBuilder content)
	{
		content.AppendLine("<div class='bold-separator'></div>");

		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		content.AppendLine($"<div class='footer-text'>Printed By: {users.FirstOrDefault(u => u.Id == bill.CreatedBy)?.Name}</div>");

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		content.AppendLine($"<div class='footer-timestamp'>Printed On: {currentDateTime:dd/MM/yy hh:mm tt}</div>");

		const string footerLineOne = "Thanks. Visit Again";
		content.AppendLine($"<div class='footer-text'>{footerLineOne}</div>");

		const string footerLineTwo = "A Product of aadisoft.tech";
		content.AppendLine($"<div class='footer-text'>{footerLineTwo}</div>");
	}
}
