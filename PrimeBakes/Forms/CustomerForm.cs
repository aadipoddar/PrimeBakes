﻿namespace PrimeBakes.Forms;

public partial class CustomerForm : Form
{
	public CustomerForm() => InitializeComponent();

	private void CustomerForm_Load(object sender, EventArgs e) => LoadComboBox();

	private async void LoadComboBox()
	{
		customerComboBox.DataSource = (await CommonData.LoadTableData<CustomerModel>("Customer")).ToList();
		customerComboBox.DisplayMember = nameof(CustomerModel.DisplayName);
		customerComboBox.ValueMember = nameof(CustomerModel.Id);

		customerComboBox.SelectedIndex = -1;
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

	private bool ValidateForm()
	{
		if (codeTextBox.Text == string.Empty) return false;
		if (nameTextBox.Text == string.Empty) return false;
		if (emailTextBox.Text == string.Empty) return false;
		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!ValidateForm())
		{
			MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

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

		LoadComboBox();
	}
}
