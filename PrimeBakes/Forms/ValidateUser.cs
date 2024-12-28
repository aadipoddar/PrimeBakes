namespace PrimeBakes.Forms;

public partial class ValidateUser : Form
{
	public ValidateUser()
	{
		InitializeComponent();
	}

	private void ValidateUser_Load(object sender, EventArgs e)
	{
		LoadComboBox();
	}

	private async void LoadComboBox()
	{
		userComboBox.DataSource = (await CommonData.LoadTableData<UserModel>("UserTable")).ToList();
		userComboBox.DisplayMember = "Name";
		userComboBox.ValueMember = "Id";
	}

	private void goButton_Click(object sender, EventArgs e)
	{
		if (ValidateUserPassword())
		{
			OrderForm orderForm = new((userComboBox.SelectedItem as UserModel).Id);
			orderForm.ShowDialog();
		}

		else MessageBox.Show("Invalid Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	private bool ValidateUserPassword()
	{
		if (passwordTextBox.Text == (userComboBox.SelectedItem as UserModel).Password) return true;

		return false;
	}
}
