using System.Reflection;

namespace PrimeBakes.Forms.Orders;

public partial class OrderIdForm : Form
{
	public OrderIdForm() => InitializeComponent();

	private void OrderIdForm_Load(object sender, EventArgs e) =>
		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";

	private async void goButton_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(orderIdTextBox.Text))
		{
			MessageBox.Show("Please enter an order ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		var order = await CommonData.LoadTableDataById<OrderModel>(Table.Order, int.Parse(orderIdTextBox.Text));
		if (order is null)
		{
			MessageBox.Show("Order not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		UpdateOrderForm updateOrderForm = new(order);
		updateOrderForm.ShowDialog();
	}

	private void orderIdTextBox_KeyPress(object sender, KeyPressEventArgs e)
	{
		if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
			e.Handled = true;
	}
}
