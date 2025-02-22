using System.Reflection;

namespace PrimeBakes.Forms;
public partial class ItemForm : Form
{
	private List<ItemModel> allItems;

	public ItemForm() => InitializeComponent();

	private async void ItemForm_Load(object sender, EventArgs e) => await LoadData();

	private async Task LoadData()
	{
		nameSearchTextBox.Clear();
		codeSearchTextBox.Clear();

		allItems = await CommonData.LoadTableData<ItemModel>(Table.Item);
		allItems = [.. allItems.OrderBy(item => item.DisplayName)];
		itemListBox.DataSource = allItems;
		itemListBox.DisplayMember = nameof(ItemModel.DisplayName);
		itemListBox.ValueMember = nameof(ItemModel.Id);

		userCategoryComboBox.DataSource = await CommonData.LoadTableData<UserCategoryModel>(Table.UserCategory);
		userCategoryComboBox.DisplayMember = nameof(UserCategoryModel.Name);
		userCategoryComboBox.ValueMember = nameof(UserCategoryModel.Id);

		categoryComboBox.DataSource = await CommonData.LoadTableData<ItemCategoryModel>(Table.ItemCategory);
		categoryComboBox.DisplayMember = nameof(ItemCategoryModel.DisplayName);
		categoryComboBox.ValueMember = nameof(ItemCategoryModel.Id);

		itemListBox.SelectedIndex = -1;

		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private void ApplySearchFilter()
	{
		var nameSearch = nameSearchTextBox.Text.Trim();
		var codeSearch = codeSearchTextBox.Text.Trim();

		var filteredItems = allItems
			.Where(item =>
				(string.IsNullOrEmpty(nameSearch) || item.DisplayName.Contains(nameSearch, StringComparison.CurrentCultureIgnoreCase)) &&
				(string.IsNullOrEmpty(codeSearch) || item.Code.Contains(codeSearch, StringComparison.CurrentCultureIgnoreCase)))
			.ToList();

		itemListBox.DataSource = filteredItems;
	}

	private void nameSearchTextBox_TextChanged(object sender, EventArgs e) => ApplySearchFilter();

	private void codeSearchTextBox_TextChanged(object sender, EventArgs e) => ApplySearchFilter();

	private void itemListBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (itemListBox?.SelectedItem is ItemModel selectedItem)
		{
			categoryComboBox.SelectedValue = selectedItem.ItemCategoryId;
			codeTextBox.Text = selectedItem.Code;
			nameTextBox.Text = selectedItem.Name;
			userCategoryComboBox.SelectedValue = selectedItem.UserCategoryId;
			statusCheckBox.Checked = selectedItem.Status;
		}
		else
		{
			codeTextBox.Clear();
			nameTextBox.Clear();
			categoryComboBox.SelectedIndex = 0;
			statusCheckBox.Checked = true;
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrEmpty(codeTextBox.Text) ||
			string.IsNullOrEmpty(nameTextBox.Text))
		{
			MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		if (itemListBox.SelectedIndex == -1 && (await CommonData.LoadTableDataByCode<ItemModel>(Table.Item, codeTextBox.Text)) is not null)
		{
			MessageBox.Show("Code already Present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!await ValidateForm()) return;

		ItemModel item = new()
		{
			ItemCategoryId = (categoryComboBox.SelectedItem as ItemCategoryModel).Id,
			Code = codeTextBox.Text,
			Name = nameTextBox.Text,
			UserCategoryId = (userCategoryComboBox.SelectedItem as UserCategoryModel).Id,
			Status = statusCheckBox.Checked
		};

		if (itemListBox.SelectedIndex == -1) await ItemData.InsertItem(item);
		else
		{
			item.Id = (itemListBox.SelectedItem as ItemModel).Id;
			await ItemData.UpdateItem(item);
		}

		await LoadData();
	}
}
