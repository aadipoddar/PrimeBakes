namespace PrimeBakes.Forms;
public partial class ItemForm : Form
{
	public ItemForm() => InitializeComponent();

	private void ItemForm_Load(object sender, EventArgs e) => LoadComboBox();

	private async void LoadComboBox()
	{
		itemComboBox.DataSource = (await CommonData.LoadTableData<ItemModel>("ItemTable")).ToList();
		itemComboBox.DisplayMember = nameof(ItemModel.DisplayName);
		itemComboBox.ValueMember = nameof(ItemModel.Id);

		itemComboBox.SelectedIndex = -1;
	}

	private void itemComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (itemComboBox?.SelectedItem is ItemModel selectedItem)
		{
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
			Code = codeTextBox.Text,
			Name = nameTextBox.Text,
			Status = statusCheckBox.Checked
		};

		if (itemComboBox.SelectedIndex == -1) await ItemData.ItemInsert(item);
		else
		{
			item.Id = (itemComboBox.SelectedItem as ItemModel).Id;
			await ItemData.ItemUpdate(item);
		}

		LoadComboBox();
	}
}
