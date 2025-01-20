using PrimeBakesLibrary.Data;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

using Color = Syncfusion.Drawing.Color;
using PointF = Syncfusion.Drawing.PointF;
using RectangleF = Syncfusion.Drawing.RectangleF;
using SizeF = Syncfusion.Drawing.SizeF;

namespace PrimeBakesLibrary.Printing;

public static class PrintSingleOrderPDF
{
	public static PdfPageTemplateElement AddHeader(PdfDocument doc, string title, string description)
	{
		RectangleF rect = new(0, 0, doc.Pages[0].GetClientSize().Width, 50);

		PdfPageTemplateElement header = new(rect);
		PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 24);
		float doubleHeight = font.Height * 2;
		Color activeColor = Color.FromArgb(44, 71, 120);
		SizeF imageSize = new(110f, 35f);

		PdfSolidBrush brush = new(activeColor);

		PdfPen pen = new(Color.DarkBlue, 3f);
		font = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);

		PdfStringFormat format = new()
		{
			Alignment = PdfTextAlignment.Center,
			LineAlignment = PdfVerticalAlignment.Middle
		};

		header.Graphics.DrawString(title, font, brush, new RectangleF(0, 0, header.Width, header.Height), format);
		brush = new PdfSolidBrush(Color.Gray);
		font = new PdfStandardFont(PdfFontFamily.Helvetica, 6, PdfFontStyle.Bold);

		format = new PdfStringFormat
		{
			Alignment = PdfTextAlignment.Left,
			LineAlignment = PdfVerticalAlignment.Bottom
		};

		header.Graphics.DrawString(description, font, brush, new RectangleF(0, 0, header.Width, header.Height - 8), format);

		pen = new PdfPen(Color.DarkBlue, 0.7f);
		header.Graphics.DrawLine(pen, 0, 0, header.Width, 0);
		pen = new PdfPen(Color.DarkBlue, 2f);
		header.Graphics.DrawLine(pen, 0, 03, header.Width + 3, 03);
		pen = new PdfPen(Color.DarkBlue, 2f);
		header.Graphics.DrawLine(pen, 0, header.Height - 3, header.Width, header.Height - 3);
		header.Graphics.DrawLine(pen, 0, header.Height, header.Width, header.Height);

		return header;
	}

	public static PdfPageTemplateElement AddFooter(PdfDocument doc)
	{
		RectangleF rect = new(0, 0, doc.Pages[0].GetClientSize().Width, 50);

		PdfPageTemplateElement footer = new(rect);
		PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);

		PdfSolidBrush brush = new(Color.Black);

		PdfPageNumberField pageNumber = new(font, brush);

		PdfPageCountField count = new(font, brush);

		PdfCompositeField compositeField = new(font, brush, "Page {0} of {1}", pageNumber, count)
		{
			Bounds = footer.Bounds
		};

		compositeField.Draw(footer.Graphics, new PointF(470, 40));

		return footer;
	}

	public static async Task<MemoryStream> PrintOrder(int orderId)
	{
		OrderModel orderModel = await CommonData.LoadTableDataById<OrderModel>("Order", orderId);

		ViewOrderModel order = new()
		{
			OrderId = orderModel.Id,
			UserName = (await CommonData.LoadTableDataById<UserModel>("User", orderModel.UserId)).Name,
			CustomerName = (await CommonData.LoadTableDataById<UserModel>("Customer", orderModel.CustomerId)).Name,
			OrderDateTime = orderModel.DateTime
		};

		PdfDocument pdfDocument = new();
		PdfPage pdfPage = pdfDocument.Pages.Add();

		pdfDocument.Template.Top = AddHeader(pdfDocument, $"{order.CustomerName}", "Order Report");
		pdfDocument.Template.Bottom = AddFooter(pdfDocument);

		PdfLayoutFormat layoutFormat = new()
		{
			Layout = PdfLayoutType.Paginate,
			Break = PdfLayoutBreakType.FitPage
		};

		PdfLayoutResult result = null;
		result = await PDFBody(pdfPage, layoutFormat, order, result);

		MemoryStream ms = new();
		pdfDocument.Save(ms);
		pdfDocument.Close(true);
		ms.Position = 0;

		return ms;
	}

	public static async Task<PdfLayoutResult> PDFBody(PdfPage pdfPage, PdfLayoutFormat layoutFormat, ViewOrderModel order, PdfLayoutResult result)
	{
		PdfTextElement textElement;

		PdfStandardFont font = new(PdfFontFamily.Helvetica, 15, PdfFontStyle.Bold);

		textElement = new PdfTextElement($"Order Id: {order.OrderId}", font);
		if (result is null) result = textElement.Draw(pdfPage, new PointF(0, 20), layoutFormat);
		else result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 20), layoutFormat);

		textElement = new PdfTextElement($"Order Date: {order.OrderDateTime}", font);
		result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 10), layoutFormat);

		textElement = new PdfTextElement($"Customer Name: {order.CustomerName}", font);
		result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 10), layoutFormat);

		textElement = new PdfTextElement($"Order Taken By: {order.UserName}", font);
		result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 10), layoutFormat);

		var groupedItems = (await OrderData.LoadOrderDetailsByOrderId(order.OrderId)).GroupBy(item => item.CategoryId);

		foreach (var item in groupedItems)
		{
			font = new PdfStandardFont(PdfFontFamily.Helvetica, 13, PdfFontStyle.Regular);

			textElement = new PdfTextElement($"Category: {item.FirstOrDefault().CategoryName}", font);
			result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 20), layoutFormat);

			var detailedPrintModels = item
				.Select(v => new
				{
					v.ItemName,
					v.ItemCode,
					v.Quantity
				}).ToList();

			PdfGrid pdfGrid = new() { DataSource = detailedPrintModels };

			foreach (PdfGridRow row in pdfGrid.Rows)
			{
				foreach (PdfGridCell cell in row.Cells)
				{
					PdfGridCellStyle cellStyle = new()
					{
						CellPadding = new PdfPaddings(5, 5, 5, 5),
						Font = new PdfStandardFont(PdfFontFamily.Helvetica, 12)
					};
					cell.Style = cellStyle;
				}
			}

			pdfGrid.ApplyBuiltinStyle(PdfGridBuiltinStyle.GridTable4Accent1);

			foreach (PdfGridColumn column in pdfGrid.Columns)
				column.Format = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
			result = pdfGrid.Draw(result.Page, new PointF(10, result.Bounds.Bottom + 10), layoutFormat);
		}

		return result;
	}
}