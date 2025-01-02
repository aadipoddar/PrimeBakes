namespace PrimeBakes.Forms;

public partial class OrderIdForm : Form
{
	public OrderIdForm()
	{
		InitializeComponent();
	}

	private async void goButton_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(orderIdTextBox.Text))
		{
			MessageBox.Show("Please enter an order ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		var order = (await CommonData.LoadTableDataById<OrderModel>("OrderTable", int.Parse(orderIdTextBox.Text))).FirstOrDefault();
		if (order is null)
		{
			MessageBox.Show("Order not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		OrderForm orderForm = new(order);
		orderForm.ShowDialog();
	}

	private void orderIdTextBox_KeyPress(object sender, KeyPressEventArgs e)
	{
		if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
			e.Handled = true;
	}
}
