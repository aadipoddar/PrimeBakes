using System.Reflection;

namespace PrimeBakes.Forms;

public partial class UserForm : Form
{
	public UserForm() => InitializeComponent();

	private void UserForm_Load(object sender, EventArgs e) => LoadData();

	private async void LoadData()
	{
		customerComboBox.DataSource = await CommonData.LoadTableData<CustomerModel>(Table.Customer);
		customerComboBox.DisplayMember = nameof(CustomerModel.DisplayName);
		customerComboBox.ValueMember = nameof(CustomerModel.Id);

		userComboBox.DataSource = await CommonData.LoadTableData<UserModel>(Table.User);
		userComboBox.DisplayMember = nameof(UserModel.DisplayName);
		userComboBox.ValueMember = nameof(UserModel.Id);

		userComboBox.SelectedIndex = -1;

		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private void userComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (userComboBox?.SelectedItem is UserModel selectedUser)
		{
			nameTextBox.Text = selectedUser.Name;
			codeTextBox.Text = selectedUser.Code;
			passwordTextBox.Text = selectedUser.Password;
			customerComboBox.SelectedValue = selectedUser.CustomerId;
			statusCheckBox.Checked = selectedUser.Status;
		}
		else
		{
			nameTextBox.Clear();
			codeTextBox.Clear();
			passwordTextBox.Clear();
			customerComboBox.SelectedIndex = 0;
			statusCheckBox.Checked = true;
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrEmpty(nameTextBox.Text) ||
			string.IsNullOrEmpty(codeTextBox.Text) ||
			string.IsNullOrEmpty(passwordTextBox.Text))
		{
			MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		if ((await CommonData.LoadTableDataByCode<UserModel>(Table.User, codeTextBox.Text)) is not null)
		{
			MessageBox.Show("Code already Present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!await ValidateForm()) return;

		UserModel user = new()
		{
			Name = nameTextBox.Text,
			Code = codeTextBox.Text,
			Password = passwordTextBox.Text,
			CustomerId = (customerComboBox.SelectedItem as CustomerModel).Id,
			Status = statusCheckBox.Checked
		};

		if (userComboBox.SelectedIndex == -1) await UserData.InsertUser(user);
		else
		{
			user.Id = (userComboBox.SelectedItem as UserModel).Id;
			await UserData.UpdateUser(user);
		}

		LoadData();
	}
}
