using System.Reflection;

namespace PrimeBakes.Forms;

public partial class CustomerForm : Form
{
	public CustomerForm() => InitializeComponent();

	private void CustomerForm_Load(object sender, EventArgs e) => LoadData();

	private async void LoadData()
	{
		customerComboBox.DataSource = await CommonData.LoadTableData<CustomerModel>(Table.Customer);
		customerComboBox.DisplayMember = nameof(CustomerModel.DisplayName);
		customerComboBox.ValueMember = nameof(CustomerModel.Id);

		customerComboBox.SelectedIndex = -1;

		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private void customerComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (customerComboBox?.SelectedItem is CustomerModel selectedCustomer)
		{
			codeTextBox.Text = selectedCustomer.Code;
			nameTextBox.Text = selectedCustomer.Name;
			emailTextBox.Text = selectedCustomer.Email;
			statusCheckBox.Checked = selectedCustomer.Status;
		}
		else
		{
			codeTextBox.Clear();
			nameTextBox.Clear();
			emailTextBox.Clear();
			statusCheckBox.Checked = true;
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrEmpty(codeTextBox.Text) ||
			string.IsNullOrEmpty(nameTextBox.Text) ||
			string.IsNullOrEmpty(emailTextBox.Text))
		{
			MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		if (customerComboBox.SelectedIndex == -1 && (await CommonData.LoadTableDataByCode<CustomerModel>(Table.Customer, codeTextBox.Text)) is not null)
		{
			MessageBox.Show("Code already Present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!await ValidateForm()) return;

		CustomerModel customer = new()
		{
			Code = codeTextBox.Text,
			Name = nameTextBox.Text,
			Email = emailTextBox.Text,
			Status = statusCheckBox.Checked
		};

		if (customerComboBox.SelectedIndex == -1) await CustomerData.InsertCustomer(customer);
		else
		{
			customer.Id = (customerComboBox.SelectedItem as CustomerModel).Id;
			await CustomerData.UpdateCustomer(customer);
		}

		LoadData();
	}
}
