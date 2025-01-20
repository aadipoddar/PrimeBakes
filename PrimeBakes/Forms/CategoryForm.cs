using System.Reflection;

namespace PrimeBakes.Forms;

public partial class CategoryForm : Form
{
	public CategoryForm() => InitializeComponent();

	private async void CatgeoryForm_Load(object sender, EventArgs e) => await LoadData();

	private async Task LoadData()
	{
		categoryComboBox.DataSource = await CommonData.LoadTableData<CategoryModel>(Table.Category);
		categoryComboBox.DisplayMember = nameof(CategoryModel.DisplayName);
		categoryComboBox.ValueMember = nameof(CategoryModel.Id);

		categoryComboBox.SelectedIndex = -1;

		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (categoryComboBox?.SelectedItem is CategoryModel selectedCategory)
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

		if ((await CommonData.LoadTableDataByCode<CategoryModel>(Table.Category, codeTextBox.Text)) is not null)
		{
			MessageBox.Show("Code already Present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!await ValidateForm()) return;

		CategoryModel category = new()
		{
			Code = codeTextBox.Text,
			Name = nameTextBox.Text,
			Status = statusCheckBox.Checked
		};

		if (categoryComboBox.SelectedIndex == -1) await CategoryData.InsertCategory(category);
		else
		{
			category.Id = (categoryComboBox.SelectedItem as CategoryModel).Id;
			await CategoryData.UpdateCategory(category);
		}

		await LoadData();
	}
}
