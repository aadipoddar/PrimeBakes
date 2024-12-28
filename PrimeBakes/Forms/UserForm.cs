namespace PrimeBakes.Forms;

public partial class UserForm : Form
{
	public UserForm()
	{
		InitializeComponent();
	}

	private void UserForm_Load(object sender, EventArgs e)
	{
		LoadComboBox();
	}

	private async void LoadComboBox()
	{
		userComboBox.DataSource = (await CommonData.LoadTableData<UserModel>("UserTable")).ToList();
		userComboBox.DisplayMember = "Name";
		userComboBox.ValueMember = "Id";

		userComboBox.SelectedIndex = -1;
	}

	private void userComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (userComboBox?.SelectedItem is UserModel selectedUser)
		{
			nameTextBox.Text = selectedUser.Name;
			passwordTextBox.Text = selectedUser.Password;
			statusCheckBox.Checked = selectedUser.Status;
		}
		else
		{
			nameTextBox.Clear();
			passwordTextBox.Clear();
			statusCheckBox.Checked = true;
		}
	}

	private bool ValidateForm()
	{
		if (nameTextBox.Text == string.Empty) return false;
		if (passwordTextBox.Text == string.Empty) return false;
		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!ValidateForm())
		{
			MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		UserModel user = new()
		{
			Name = nameTextBox.Text,
			Password = passwordTextBox.Text,
			Status = statusCheckBox.Checked
		};

		if (userComboBox.SelectedIndex == -1) await UserData.UserInsert(user);
		else
		{
			user.Id = (userComboBox.SelectedItem as UserModel).Id;
			await UserData.UserUpdate(user);
		}

		LoadComboBox();
	}
}
