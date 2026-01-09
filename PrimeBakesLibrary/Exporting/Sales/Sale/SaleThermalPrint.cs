using System.Text;

using NumericWordsConversion;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Sales.Masters;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public class SaleThermalPrint
{
    public static async Task<StringBuilder> GenerateThermalBill(int saleId)
    {
        var sale = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, saleId);
        StringBuilder content = new();

        await AddHeader(content);

        await AddBillDetails(sale, content);

        await AddItemDetails(sale, content);

        await AddTotalDetails(sale, content);

        await AddPaymentModes(sale, content);

        await AddFooter(content);

        return content;
    }

    private static async Task AddHeader(StringBuilder content)
    {
        var primaryCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, int.Parse(primaryCompanyId.Value));

        content.AppendLine("<div class='header'>");
        content.AppendLine($"<div class='company-name'>PRIME BAKES</div>");

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

    private static async Task AddBillDetails(SaleOverviewModel sale, StringBuilder content)
    {
        content.AppendLine("<div class='bill-details'>");
        content.AppendLine($"<div class='detail-row'><span class='detail-label'>Outlet:</span> <span class='detail-value'>{sale.LocationName}</span></div>");
        content.AppendLine($"<div class='detail-row'><span class='detail-label'>Bill No:</span> <span class='detail-value'>{sale.TransactionNo}</span></div>");
        content.AppendLine($"<div class='detail-row'><span class='detail-label'>Date:</span> <span class='detail-value'>{sale.TransactionDateTime:dd/MM/yy hh:mm tt}</span></div>");
        content.AppendLine($"<div class='detail-row'><span class='detail-label'>User:</span> <span class='detail-value'>{sale.CreatedByName}</span></div>");

        if (sale.OrderId.HasValue && sale.OrderId.Value > 0)
        {
            content.AppendLine($"<div class='detail-row'><span class='detail-label'>Order:</span>");
            content.AppendLine($"<span class='detail-value'>{sale.OrderTransactionNo}</span></div>");
            content.AppendLine($"<span class='detail-value'>{sale.OrderDateTime:dd/MM/yy hh:mm tt}</span></div>");
        }

        if (sale.PartyId.HasValue && sale.PartyId.Value > 0)
        {
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
            if (party is not null)
                content.AppendLine($"<div class='detail-row'><span class='detail-label'>Party:</span> <span class='detail-value'>{party.Name}</span></div>");
        }

        if (sale.CustomerId.HasValue && sale.CustomerId.Value > 0)
        {
            var customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, sale.CustomerId.Value);
            content.AppendLine($"<div class='detail-row'><span class='detail-label'>Cust. Name:</span> <span class='detail-value'>{customer.Name}</span></div>");
            content.AppendLine($"<div class='detail-row'><span class='detail-label'>Cust. No.:</span> <span class='detail-value'>{customer.Number}</span></div>");
        }

        content.AppendLine("</div>");
        content.AppendLine("<div class='bold-separator'></div>");
    }

    private static async Task AddItemDetails(SaleOverviewModel sale, StringBuilder content)
    {
        var saleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);

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

        foreach (var item in saleDetails)
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

    private static async Task AddTotalDetails(SaleOverviewModel sale, StringBuilder content)
    {
        content.AppendLine("<table class='summary-table'>");
        content.AppendLine($"<tr><td class='summary-label'>Sub Total:</td><td align='right' class='summary-value'>{sale.TotalAfterTax.FormatIndianCurrency()}</td></tr>");

        if (sale.DiscountPercent > 0)
            content.AppendLine($"<tr><td class='summary-label'>Discount ({sale.DiscountPercent}%):</td><td align='right' class='summary-value'>{sale.DiscountAmount.FormatIndianCurrency()}</td></tr>");

        if (sale.RoundOffAmount != 0)
            content.AppendLine($"<tr><td class='summary-label'>Round Off:</td><td align='right' class='summary-value'>{sale.RoundOffAmount.FormatIndianCurrency()}</td></tr>");

        content.AppendLine("</table>");
        content.AppendLine("<div class='bold-separator'></div>");

        content.AppendLine("<table class='grand-total'>");
        content.AppendLine($"<tr><td class='grand-total-label'>Grand Total:</td><td align='right' class='grand-total-value'>{sale.TotalAmount.FormatIndianCurrency()}</td></tr>");
        content.AppendLine("</table>");

        CurrencyWordsConverter numericWords = new(new()
        {
            Culture = Culture.Hindi,
            OutputFormat = OutputFormat.English
        });

        string amountInWords = numericWords.ToWords(Math.Round(sale.TotalAmount));
        if (string.IsNullOrEmpty(amountInWords))
            amountInWords = "Zero";

        amountInWords += " Rupees Only";

        content.AppendLine("<div class='amount-words'>" + amountInWords + "</div>");
    }

    private static async Task AddPaymentModes(SaleOverviewModel sale, StringBuilder content)
    {
        // Check if any payment is made
        bool hasPayments = sale.Cash > 0 || sale.Card > 0 || sale.UPI > 0 || sale.Credit > 0;

        if (!hasPayments)
            return;

        content.AppendLine("<table class='summary-table'>");

        if (sale.Cash > 0)
            content.AppendLine($"<tr><td class='summary-label'>Cash:</td><td align='right' class='summary-value'>{sale.Cash.FormatIndianCurrency()}</td></tr>");

        if (sale.Card > 0)
            content.AppendLine($"<tr><td class='summary-label'>Card:</td><td align='right' class='summary-value'>{sale.Card.FormatIndianCurrency()}</td></tr>");

        if (sale.UPI > 0)
            content.AppendLine($"<tr><td class='summary-label'>UPI:</td><td align='right' class='summary-value'>{sale.UPI.FormatIndianCurrency()}</td></tr>");

        if (sale.Credit > 0)
            content.AppendLine($"<tr><td class='summary-label'>Credit:</td><td align='right' class='summary-value'>{sale.Credit.FormatIndianCurrency()}</td></tr>");

        content.AppendLine("</table>");
    }

    private static async Task AddFooter(StringBuilder content)
    {
        content.AppendLine("<div class='bold-separator'></div>");

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        content.AppendLine($"<div class='footer-timestamp'>Printed: {currentDateTime:dd/MM/yy hh:mm tt}</div>");

        string footerLine = "Thanks. Visit Again";
        content.AppendLine($"<div class='footer-text'>{footerLine}</div>");

        footerLine = "A Product of aadisoft.vercel.app";
        content.AppendLine($"<div class='footer-text'>{footerLine}</div>");
    }
}
