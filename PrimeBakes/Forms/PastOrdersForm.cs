using System.Diagnostics;

using PrimeBakesLibrary.Printing;

namespace PrimeBakes.Forms;

public partial class PastOrdersForm : Form
{
	public PastOrdersForm() => InitializeComponent();

	private void PastOrdersForm_Load(object sender, EventArgs e)
	{
		toDateTimePicker.Value = DateTime.Now.AddDays(1);
		LoadOrders();
	}

	private void dateTimePicker_ValueChanged(object sender, EventArgs e) => LoadOrders();

	private void refreshButton_Click(object sender, EventArgs e) => LoadOrders();

	private async void LoadOrders() => orderDataGridView.DataSource = await OrderData.LoadOrdersByDate(fromDateTimePicker.Value.Date, toDateTimePicker.Value.Date);

	private async void orderDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		ViewOrderModel detailedOrderModel = (ViewOrderModel)orderDataGridView.Rows[e.RowIndex].DataBoundItem;

		var orderModel = (await CommonData.LoadTableDataById<OrderModel>("OrderTable", detailedOrderModel.OrderId)).FirstOrDefault();

		OrderForm orderForm = new(orderModel);
		if (orderForm.ShowDialog() == DialogResult.OK) LoadOrders();
	}

	private async void printButton_Click(object sender, EventArgs e)
	{
		MemoryStream ms = await PrintOrdersPDF.PrintOrders((List<ViewOrderModel>)orderDataGridView.DataSource, GetDateString());

		using (FileStream stream = new FileStream(Path.Combine(Path.GetTempPath(), "OrderReport.pdf"), FileMode.Create, FileAccess.Write)) ms.WriteTo(stream);

		Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\OrderReport.pdf") { UseShellExecute = true });
	}

	private string GetDateString()
	{
		string fromDate = fromDateTimePicker.Value.Date.ToString("dd-MM-yyyy");
		string toDate = toDateTimePicker.Value.Date.ToString("dd-MM-yyyy");
		return $"{fromDate} to {toDate}";
	}
}
