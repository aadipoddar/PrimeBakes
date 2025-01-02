namespace PrimeBakes.Forms;

public partial class PastOrdersForm : Form
{
	public PastOrdersForm() => InitializeComponent();

	private void PastOrdersForm_Load(object sender, EventArgs e) => toDateTimePicker.Value = DateTime.Now.AddDays(1);

	private async void LoadOrders() => orderDataGridView.DataSource = await OrderData.LoadOrdersByDate(fromDateTimePicker.Value.Date, toDateTimePicker.Value.Date);

	private void dateTimePicker_ValueChanged(object sender, EventArgs e) => LoadOrders();

	private async void orderDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		ViewOrderModel detailedOrderModel = (ViewOrderModel)orderDataGridView.Rows[e.RowIndex].DataBoundItem;

		var orderModel = (await CommonData.LoadTableDataById<OrderModel>("OrderTable", detailedOrderModel.OrderId)).FirstOrDefault();

		OrderForm orderForm = new(orderModel);
		if (orderForm.ShowDialog() == DialogResult.OK)
			LoadOrders();
	}
}
