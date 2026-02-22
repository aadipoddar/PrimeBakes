using System.Text;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

public static class KOTThermalPrint
{
	public static async Task<StringBuilder> GenerateThermalBill(int billId)
	{
		var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);
		var kotItems = billDetails.Where(d => d.KOTPrint).ToList();

		if (kotItems.Count == 0)
			return new StringBuilder();

		StringBuilder content = new();

		await AddHeader(content);
		await AddBillDetails(bill, content);
		await AddItems(kotItems, content);
		await AddFooter(bill, content);

		await BillData.MarkKOTAsPrinted(bill.Id);

		return content;
	}

	private static Task AddHeader(StringBuilder content)
	{
		content.AppendLine("<div class='header'>");
		content.AppendLine("<div class='company-name'>KOT</div>");
		content.AppendLine("</div>");
		content.AppendLine("<div class='bold-separator'></div>");

		return Task.CompletedTask;
	}

	private static async Task AddBillDetails(BillModel bill, StringBuilder content)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, bill.LocationId);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, bill.DiningTableId);

		content.AppendLine("<div class='bill-details'>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Outlet:</span> <span class='detail-value'>{location?.Name}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Bill No:</span> <span class='detail-value'>{bill.TransactionNo}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Date:</span> <span class='detail-value'>{bill.TransactionDateTime:dd/MM/yy hh:mm tt}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Table No:</span> <span class='detail-value'>{table?.Name}</span></div>");
		content.AppendLine("</div>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddItems(List<BillDetailModel> kotItems, StringBuilder content)
	{
		var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		content.AppendLine("<table class='items-table'>");
		content.AppendLine("<thead>");
		content.AppendLine("<tr class='table-header'>");
		content.AppendLine("<th align='left'>Item</th>");
		content.AppendLine("<th align='right'>Qty.</th>");
		content.AppendLine("</tr>");
		content.AppendLine("</thead>");
		content.AppendLine("<tbody>");

		foreach (var item in kotItems)
		{
			var productName = products.FirstOrDefault(p => p.Id == item.ProductId)?.Name;

			content.AppendLine("<tr class='table-row'>");
			content.AppendLine($"<td align='left'><strong>{productName}</strong></td>");
			content.AppendLine($"<td align='right'>{item.Quantity.FormatSmartDecimal()}</td>");
			content.AppendLine("</tr>");

			if (!string.IsNullOrWhiteSpace(item.Remarks))
			{
				content.AppendLine("<tr class='table-row'>");
				content.AppendLine($"<td colspan='2' class='remarks'>&nbsp;&nbsp;[Note] {item.Remarks}</td>");
				content.AppendLine("</tr>");
			}
		}

		content.AppendLine("</tbody>");
		content.AppendLine("</table>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddFooter(BillModel bill, StringBuilder content)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		content.AppendLine($"<div class='footer-text'>Printed By: {users.FirstOrDefault(u => u.Id == bill.CreatedBy)?.Name}</div>");
		content.AppendLine($"<div class='footer-timestamp'>Printed On: {currentDateTime:dd/MM/yy hh:mm tt}</div>");

		const string footerLineTwo = "A Product of aadisoft.vercel.app";
		content.AppendLine($"<div class='footer-text'>{footerLineTwo}</div>");
	}
}
