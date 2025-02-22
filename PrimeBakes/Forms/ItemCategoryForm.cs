using System.Reflection;

namespace PrimeBakes.Forms;

public partial class ItemCategoryForm : Form
{
	public ItemCategoryForm() => InitializeComponent();

	private async void ItemCatgeoryForm_Load(object sender, EventArgs e) => await LoadData();

	private async Task LoadData()
	{
		categoryComboBox.DataSource = await CommonData.LoadTableData<ItemCategoryModel>(Table.ItemCategory);
		categoryComboBox.DisplayMember = nameof(ItemCategoryModel.DisplayName);
		categoryComboBox.ValueMember = nameof(ItemCategoryModel.Id);

		categoryComboBox.SelectedIndex = -1;

		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (categoryComboBox?.SelectedItem is ItemCategoryModel selectedCategory)
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

		if (categoryComboBox.SelectedIndex == -1 && (await CommonData.LoadTableDataByCode<ItemCategoryModel>(Table.ItemCategory, codeTextBox.Text)) is not null)
		{
			MessageBox.Show("Code already Present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!await ValidateForm()) return;

		ItemCategoryModel category = new()
		{
			Code = codeTextBox.Text,
			Name = nameTextBox.Text,
			Status = statusCheckBox.Checked
		};

		if (categoryComboBox.SelectedIndex == -1) await ItemCategoryData.InsertItemCategory(category);
		else
		{
			category.Id = (categoryComboBox.SelectedItem as ItemCategoryModel).Id;
			await ItemCategoryData.UpdateItemCategory(category);
		}

		await LoadData();
	}
}
