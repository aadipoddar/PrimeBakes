using PrimeBakes.Forms;

namespace PrimeBakes;

public partial class Dashboard : Form
{
	public Dashboard()
	{
		InitializeComponent();
	}

	private void userButton_Click(object sender, EventArgs e)
	{
		UserForm userForm = new();
		userForm.ShowDialog();
	}

	private void customerButton_Click(object sender, EventArgs e)
	{
		CustomerForm customerForm = new();
		customerForm.ShowDialog();
	}

	private void itemButton_Click(object sender, EventArgs e)
	{
		ItemForm itemForm = new();
		itemForm.ShowDialog();
	}

	private void orderButton_Click(object sender, EventArgs e)
	{
		OrderForm orderForm = new();
		orderForm.ShowDialog();
	}
}
