using PrimeBakes.Forms.Orders;

namespace PrimeBakes.Forms;

public partial class ValidateUserForm : Form
{
	private int _userId;

	public ValidateUserForm() => InitializeComponent();

	private void userIdTextBox_KeyPress(object sender, KeyPressEventArgs e)
	{
		if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
			e.Handled = true;
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrEmpty(userIdTextBox.Text)) return false;
		if (string.IsNullOrEmpty(passwordTextBox.Text)) return false;

		var user = await CommonData.LoadTableDataById<UserModel>(Table.User, int.Parse(userIdTextBox.Text));
		if (user is null) return false;
		if (passwordTextBox.Text != user.Password) return false;

		_userId = user.Id;
		return true;
	}

	private async void goButton_Click(object sender, EventArgs e)
	{
		if (!await ValidateForm())
		{
			MessageBox.Show("Enter Correct User Id and Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

			userIdTextBox.Text = string.Empty;
			passwordTextBox.Text = string.Empty;
			return;
		}

		userIdTextBox.Text = string.Empty;
		passwordTextBox.Text = string.Empty;

		OrderForm orderForm = new(_userId);
		orderForm.ShowDialog();
	}
}
