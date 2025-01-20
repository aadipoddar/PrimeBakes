using System.Reflection;

using PrimeBakes.Forms.Orders;

namespace PrimeBakes.Forms;

public partial class ValidateUserForm : Form
{
	private int _userId;

	public ValidateUserForm() => InitializeComponent();

	private void ValidateUserForm_Load(object sender, EventArgs e) =>
		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrEmpty(userCodeTextBox.Text)) return false;
		if (string.IsNullOrEmpty(passwordTextBox.Text)) return false;

		var user = await CommonData.LoadTableDataByCodeActive<UserModel>(Table.User, userCodeTextBox.Text);
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

			userCodeTextBox.Text = string.Empty;
			passwordTextBox.Text = string.Empty;
			return;
		}

		userCodeTextBox.Text = string.Empty;
		passwordTextBox.Text = string.Empty;

		OrderForm orderForm = new(_userId);
		orderForm.ShowDialog();
	}
}
