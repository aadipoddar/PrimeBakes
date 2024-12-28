namespace PrimeBakes.Forms;

public partial class OrderForm : Form
{
	private readonly int _userId;

	public OrderForm(int userId)
	{
		InitializeComponent();

		_userId = userId;
	}

	private void OrderForm_Load(object sender, EventArgs e) => LoadComboBox();

	private async void LoadComboBox()
	{
		customerComboBox.DataSource = (await CommonData.LoadTableData<CustomerModel>("CustomerTable")).ToList();
		customerComboBox.DisplayMember = nameof(CustomerModel.DisplayName);
		customerComboBox.ValueMember = nameof(CustomerModel.Id);

		itemComboBox.DataSource = (await CommonData.LoadTableData<ItemModel>("ItemTable")).ToList();
		itemComboBox.DisplayMember = nameof(ItemModel.DisplayName);
		itemComboBox.ValueMember = nameof(ItemModel.Id);
	}

	private void quantityTextBox_KeyPress(object sender, KeyPressEventArgs e)
	{
		if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
			e.Handled = true;
	}

	private void addButton_Click(object sender, EventArgs e) => AddItemToDataGridView();

	private void AddItemToDataGridView()
	{
		if (int.Parse(quantityTextBox.Text) == 0)
			quantityTextBox.Text = "1";

		if (customerComboBox.SelectedItem is CustomerModel selectedCustomer && itemComboBox.SelectedItem is ItemModel selectedItem)
		{
			int quantity = int.TryParse(quantityTextBox.Text, out int result) ? result : 1;

			itemsDataGridView.Rows.Add(selectedCustomer.Name, selectedItem.Code, quantity);
		}
		else
			MessageBox.Show("Please select a customer and an item.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	private void ClearForm()
	{
		customerComboBox.SelectedIndex = -1;
		itemComboBox.SelectedIndex = -1;
		quantityTextBox.Text = "1";
		itemsDataGridView.Rows.Clear();
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (itemsDataGridView.Rows.Count == 0)
		{
			MessageBox.Show("Please add at least one item to the order.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		await InsertIntoOrderDetailTable(await InsertIntoOrderTable());
	}

	private async Task<int> InsertIntoOrderTable()
	{
		OrderModel orderModel = new()
		{
			Id = 0,
			UserId = _userId,
			CustomerId = (customerComboBox.SelectedItem as CustomerModel).Id,
			DateTime = DateTime.Now
		};

		return await OrderData.OrderInsert(orderModel);
	}

	private async Task InsertIntoOrderDetailTable(int orderId)
	{
		foreach (DataGridViewRow row in itemsDataGridView.Rows)
		{
			if (row.IsNewRow) continue;

			var itemCode = row.Cells[1].Value.ToString();
			var item = (await CommonData.LoadTableData<ItemModel>("ItemTable")).FirstOrDefault(i => i.Code == itemCode);

			if (item != null)
			{
				OrderDetailModel detailModel = new()
				{
					Id = 0,
					OrderId = orderId,
					ItemId = item.Id,
					Quantity = int.Parse(row.Cells[2].Value.ToString())
				};

				await OrderData.OrderDetailInsert(detailModel);
			}
		}

		ClearForm();
	}
}
