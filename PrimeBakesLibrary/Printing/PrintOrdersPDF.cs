using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

using Color = Syncfusion.Drawing.Color;
using PointF = Syncfusion.Drawing.PointF;
using RectangleF = Syncfusion.Drawing.RectangleF;
using SizeF = Syncfusion.Drawing.SizeF;

namespace PrimeBakesLibrary.Printing;

public static class PrintOrdersPDF
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

	public static async Task<MemoryStream> PrintOrders(List<ViewOrderModel> orders, string dateHeader)
	{
		PdfDocument pdfDocument = new();
		PdfPage pdfPage = pdfDocument.Pages.Add();

		pdfDocument.Template.Top = AddHeader(pdfDocument, dateHeader, "Order Report");
		pdfDocument.Template.Bottom = AddFooter(pdfDocument);

		PdfLayoutFormat layoutFormat = new();
		layoutFormat.Layout = PdfLayoutType.Paginate;
		layoutFormat.Break = PdfLayoutBreakType.FitPage;

		PdfLayoutResult result = null;

		foreach (var order in orders) result = await PrintSingleOrderPDF.PDFBody(pdfPage, layoutFormat, order, result);

		MemoryStream ms = new();
		pdfDocument.Save(ms);
		pdfDocument.Close(true);
		ms.Position = 0;

		return ms;
	}
}