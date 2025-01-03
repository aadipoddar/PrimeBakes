using System.Reflection;

using PrimeBakes.Forms;
using PrimeBakes.Forms.Orders;

using PrimeBakesLibrary.DataAccess;

namespace PrimeBakes;

public partial class Dashboard : Form
{
	public Dashboard() => InitializeComponent();

	private async void Dashboard_Load(object sender, EventArgs e) => await UpdateCheck();

	private async Task UpdateCheck()
	{
		bool isUpdateAvailable = await AadiSoftUpdater.AadiSoftUpdater.CheckForUpdates("aadipoddar", $"{Secrets.DatabaseName}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

		if (isUpdateAvailable)
			if (MessageBox.Show("New Version Available. Do you want to update?", "Update Available", MessageBoxButtons.YesNo) == DialogResult.Yes)
				await AadiSoftUpdater.AadiSoftUpdater.UpdateApp("aadipoddar", $"{Secrets.DatabaseName}", $"{Secrets.DatabaseName}", "0E465F58-E338-47FD-98D5-85DC8F4BECD1");
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
		ValidateUserForm validateUser = new();
		validateUser.ShowDialog();
	}

	private void pastOrdersButton_Click(object sender, EventArgs e)
	{
		PastOrdersForm pastOrdersForm = new();
		pastOrdersForm.ShowDialog();
	}

	private void viewUpdateOrderButton_Click(object sender, EventArgs e)
	{
		OrderIdForm orderIdForm = new();
		orderIdForm.ShowDialog();
	}
}
