using System.Reflection;

namespace PrimeBakes;

public partial class UserCategoryForm : Form
{
	public UserCategoryForm() => InitializeComponent();

	private async void UserCatgeoryForm_Load(object sender, EventArgs e) => await LoadData();

	private async Task LoadData()
	{
		categoryComboBox.DataSource = await CommonData.LoadTableData<UserCategoryModel>(Table.UserCategory);
		categoryComboBox.DisplayMember = nameof(UserCategoryModel.DisplayName);
		categoryComboBox.ValueMember = nameof(UserCategoryModel.Id);

		categoryComboBox.SelectedIndex = -1;

		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (categoryComboBox?.SelectedItem is UserCategoryModel selectedCategory)
		{
			codeTextBox.Text = selectedCategory.Code;
			nameTextBox.Text = selectedCategory.Name;
			statusCheckBox.Checked = selectedCategory.Status;
		}
		else
		{
			codeTextBox.Clear();
			nameTextBox.Clear();
			statusCheckBox.Checked = true;
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrEmpty(codeTextBox.Text) || string.IsNullOrEmpty(nameTextBox.Text))
		{
			MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		if (categoryComboBox.SelectedIndex == -1 && (await CommonData.LoadTableDataByCode<UserCategoryModel>(Table.ItemCategory, codeTextBox.Text)) is not null)
		{
			MessageBox.Show("Code already Present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!await ValidateForm()) return;

		UserCategoryModel category = new()
		{
			Code = codeTextBox.Text,
			Name = nameTextBox.Text,
			Status = statusCheckBox.Checked
		};

		if (categoryComboBox.SelectedIndex == -1) await UserCategoryData.InsertUserCategory(category);
		else
		{
			category.Id = (categoryComboBox.SelectedItem as UserCategoryModel).Id;
			await UserCategoryData.UpdateUserCategory(category);
		}

		await LoadData();
	}
}
