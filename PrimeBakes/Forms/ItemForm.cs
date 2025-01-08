namespace PrimeBakes.Forms;
public partial class ItemForm : Form
{
	public ItemForm() => InitializeComponent();

	private async void ItemForm_Load(object sender, EventArgs e) => await LoadData();

	private async Task LoadData()
	{
		itemComboBox.DataSource = (await CommonData.LoadTableData<ItemModel>("Item")).ToList();
		itemComboBox.DisplayMember = nameof(ItemModel.DisplayName);
		itemComboBox.ValueMember = nameof(ItemModel.Id);

		categoryComboBox.DataSource = (await CommonData.LoadTableData<CategoryModel>("Category")).ToList();
		categoryComboBox.DisplayMember = nameof(CategoryModel.DisplayName);
		categoryComboBox.ValueMember = nameof(CategoryModel.Id);

		itemComboBox.SelectedIndex = -1;
	}

	private void itemComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (itemComboBox?.SelectedItem is ItemModel selectedItem)
		{
			categoryComboBox.SelectedValue = selectedItem.CategoryId;
			codeTextBox.Text = selectedItem.Code;
			nameTextBox.Text = selectedItem.Name;
			statusCheckBox.Checked = selectedItem.Status;
		}
		else
		{
			codeTextBox.Clear();
			nameTextBox.Clear();
			statusCheckBox.Checked = true;
		}
	}

	private bool ValidateForm()
	{
		if (codeTextBox.Text == string.Empty) return false;
		if (nameTextBox.Text == string.Empty) return false;
		return true;
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (!ValidateForm())
		{
			MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		ItemModel item = new()
		{
			CategoryId = (categoryComboBox.SelectedItem as CategoryModel).Id,
			Code = codeTextBox.Text,
			Name = nameTextBox.Text,
			Status = statusCheckBox.Checked
		};

		if (itemComboBox.SelectedIndex == -1) await ItemData.InsertItem(item);
		else
		{
			item.Id = (itemComboBox.SelectedItem as ItemModel).Id;
			await ItemData.UpdateItem(item);
		}

		await LoadData();
	}
}
