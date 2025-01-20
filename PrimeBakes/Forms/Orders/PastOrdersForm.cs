using System.Diagnostics;
using System.Reflection;

using PrimeBakesLibrary.Printing;

namespace PrimeBakes.Forms.Orders;

public partial class PastOrdersForm : Form
{
	public PastOrdersForm() => InitializeComponent();

	private void PastOrdersForm_Load(object sender, EventArgs e)
	{
		toDateTimePicker.Value = DateTime.Now.AddDays(1);
		LoadOrders();
		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private void dateTimePicker_ValueChanged(object sender, EventArgs e) => LoadOrders();

	private void showClearedCheckBox_CheckedChanged(object sender, EventArgs e) => LoadOrders();

	private void refreshButton_Click(object sender, EventArgs e) => LoadOrders();

	private async void LoadOrders() => orderDataGridView.DataSource = await OrderData.LoadOrdersByDateStatus(fromDateTimePicker.Value.Date, toDateTimePicker.Value.Date, !showClearedCheckBox.Checked);

	private async void orderDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		ViewOrderModel detailedOrderModel = (ViewOrderModel)orderDataGridView.Rows[e.RowIndex].DataBoundItem;

		var orderModel = await CommonData.LoadTableDataById<OrderModel>(Table.Order, detailedOrderModel.OrderId);

		UpdateOrderForm updateOrderForm = new(orderModel);
		if (updateOrderForm.ShowDialog() == DialogResult.OK) LoadOrders();
	}

	private async void printButton_Click(object sender, EventArgs e)
	{
		MemoryStream ms = await PrintOrdersPDF.PrintOrders((List<ViewOrderModel>)orderDataGridView.DataSource, GetDateString());
		using FileStream stream = new(Path.Combine(Path.GetTempPath(), "OrderReport.pdf"), FileMode.Create, FileAccess.Write);
		await ms.CopyToAsync(stream);
		ms.Close();
		Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\OrderReport.pdf") { UseShellExecute = true });
	}

	private string GetDateString()
	{
		string fromDate = fromDateTimePicker.Value.Date.ToString("dd-MM-yyyy");
		string toDate = toDateTimePicker.Value.Date.ToString("dd-MM-yyyy");
		return $"{fromDate} to {toDate}";
	}
}
