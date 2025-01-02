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

		PdfStringFormat format = new();
		format.Alignment = PdfTextAlignment.Center;
		format.LineAlignment = PdfVerticalAlignment.Middle;

		header.Graphics.DrawString(title, font, brush, new RectangleF(0, 0, header.Width, header.Height), format);
		brush = new PdfSolidBrush(Color.Gray);
		font = new PdfStandardFont(PdfFontFamily.Helvetica, 6, PdfFontStyle.Bold);

		format = new PdfStringFormat();
		format.Alignment = PdfTextAlignment.Left;
		format.LineAlignment = PdfVerticalAlignment.Bottom;

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

		PdfCompositeField compositeField = new(font, brush, "Page {0} of {1}", pageNumber, count);
		compositeField.Bounds = footer.Bounds;

		compositeField.Draw(footer.Graphics, new PointF(470, 40));

		return footer;
	}

	public static async Task<MemoryStream> PrintOrder(int orderId)
	{
		OrderModel orderModel = (await CommonData.LoadTableDataById<OrderModel>("OrderTable", orderId)).FirstOrDefault();

		ViewOrderModel order = new()
		{
			OrderId = orderModel.Id,
			UserName = (await CommonData.LoadTableDataById<UserModel>("UserTable", orderModel.UserId)).FirstOrDefault().Name,
			CustomerName = (await CommonData.LoadTableDataById<UserModel>("CustomerTable", orderModel.CustomerId)).FirstOrDefault().Name,
			OrderDateTime = orderModel.DateTime
		};

		PdfDocument pdfDocument = new();
		PdfPage pdfPage = pdfDocument.Pages.Add();

		pdfDocument.Template.Top = AddHeader(pdfDocument, $"{order.CustomerName}", "Order Report");
		pdfDocument.Template.Bottom = AddFooter(pdfDocument);

		PdfLayoutFormat layoutFormat = new();
		layoutFormat.Layout = PdfLayoutType.Paginate;
		layoutFormat.Break = PdfLayoutBreakType.FitPage;

		PdfStandardFont font = new(PdfFontFamily.Helvetica, 25, PdfFontStyle.Bold);

		PdfLayoutResult result = null;
		PdfTextElement textElement;

		font = new PdfStandardFont(PdfFontFamily.Helvetica, 15, PdfFontStyle.Bold);

		textElement = new PdfTextElement($"Order Id: {order.OrderId}", font);
		if (result == null) result = textElement.Draw(pdfPage, new PointF(0, 20), layoutFormat);
		else result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 20), layoutFormat);

		textElement = new PdfTextElement($"Order Date: {order.OrderDateTime}", font);
		result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 10), layoutFormat);

		textElement = new PdfTextElement($"Customer Name: {order.CustomerName}", font);
		result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 10), layoutFormat);

		textElement = new PdfTextElement($"Order Taken By: {order.UserName}", font);
		result = textElement.Draw(result.Page, new PointF(0, result.Bounds.Bottom + 10), layoutFormat);

		var detailedPrintModel = (await OrderData.LoadPrintOrderDetailsByOrderId(order.OrderId)).ToList();
		PdfGrid pdfGrid = new() { DataSource = detailedPrintModel };

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
		result = pdfGrid.Draw(result.Page, new PointF(10, result.Bounds.Bottom + 20), layoutFormat);

		MemoryStream ms = new();
		pdfDocument.Save(ms);
		pdfDocument.Close(true);
		ms.Position = 0;

		return ms;
	}
}