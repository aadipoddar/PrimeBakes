using System.Drawing.Printing;

namespace PrimeBakes.Printing;

public static class PrintOrderThermal
{
	private static readonly Font headerFont = new("Arial", 25, FontStyle.Bold);
	private static readonly Font regularFont = new("Courier New", 12, FontStyle.Bold);
	private static readonly Font footerFont = new("Courier New", 8, FontStyle.Bold);
	private static Font font = regularFont;

	private static readonly StringFormat center = new(StringFormatFlags.FitBlackBox) { Alignment = StringAlignment.Center };
	private static readonly StringFormat tabbedFormat = new();
	private static int startPosition = 10;
	private static int lowerSpacing = 0;
	private static int maxWidth;

	static PrintOrderThermal() => tabbedFormat.SetTabStops(0, [100]);

	private static void DrawString(Graphics g, string content, bool isCenter = false, bool useTabs = false)
	{
		StringFormat format = isCenter ? center : (useTabs ? tabbedFormat : new StringFormat());
		SizeF size = g.MeasureString(content, font, maxWidth, format);
		g.DrawString(content, font, Brushes.Black, new RectangleF(startPosition, lowerSpacing, maxWidth, size.Height), format);
		lowerSpacing += (int)size.Height;
	}

	private static void DrawHeader(Graphics g, string customerName)
	{
		font = headerFont;
		DrawString(g, $"** {customerName} **", true);
	}

	private static void DrawReceiptDetails(Graphics g, ViewOrderModel viewOrderModel)
	{
		font = regularFont;
		DrawString(g, $"Order Id: {viewOrderModel.OrderId}");
		DrawString(g, $"Order DT: {viewOrderModel.OrderDateTime:dd/MM/yy HH:mm}");
		DrawString(g, $"Customer Name: {viewOrderModel.CustomerName}");
		DrawString(g, $"Order Taken By: {viewOrderModel.UserName}");
		DrawString(g, "------------------------", true);
	}

	private static void DrawPaymentDetails(Graphics g, List<PrintOrderDetailModel> detailedPrintModel)
	{
		font = footerFont;
		DrawString(g, "Name\tCode\tQuantity", false, true);

		foreach (var items in detailedPrintModel)
			DrawString(g, $"{items.ItemName}\t{items.ItemCode}\t{items.Quantity}", false, true);

		DrawString(g, "------------------------", true);
	}

	private static void DrawFooter(Graphics g)
	{
		font = footerFont;
		DrawString(g, "Thank You", true);
	}

	public static void DrawGraphics(PrintPageEventArgs e, int orderId)
	{
		OrderModel orderModel = Task.Run(async () => await CommonData.LoadTableDataById<OrderModel>("OrderTable", orderId)).Result.FirstOrDefault();

		ViewOrderModel order = new()
		{
			OrderId = orderModel.Id,
			UserName = Task.Run(async () => await CommonData.LoadTableDataById<UserModel>("UserTable", orderModel.UserId)).Result.FirstOrDefault().Name,
			CustomerName = Task.Run(async () => await CommonData.LoadTableDataById<UserModel>("CustomerTable", orderModel.CustomerId)).Result.FirstOrDefault().Name,
			OrderDateTime = orderModel.DateTime
		};

		var detailedPrintModel = Task.Run(async () => await OrderData.LoadPrintOrderDetailsByOrderId(order.OrderId)).Result.ToList();

		Graphics g = e.Graphics;
		maxWidth = e.PageBounds.Width - 20;
		startPosition = 10;
		lowerSpacing = 0;

		DrawHeader(g, order.CustomerName);
		DrawReceiptDetails(g, order);
		DrawPaymentDetails(g, detailedPrintModel);
		DrawFooter(g);

		PaperSize ps58 = new PaperSize("58mm Thermal", 220, lowerSpacing + 20);
		e.PageSettings.PaperSize = ps58;

		e.HasMorePages = false;
	}
}