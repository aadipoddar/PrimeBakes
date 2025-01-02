using System.ComponentModel;

namespace PrimeBakes.Forms;

public partial class OrderForm : Form
{
	private readonly int _userId;
	private readonly OrderModel _orderModel;
	private BindingList<ViewOrderDetailModel> _orderDetails;

	public OrderForm(int userId)
	{
		InitializeComponent();

		_userId = userId;
		_orderDetails = new();
		itemsDataGridView.DataSource = _orderDetails;
		HideFirstColumn();
	}

	public OrderForm(OrderModel orderModel)
	{
		InitializeComponent();

		_orderModel = orderModel;
		_userId = orderModel.UserId;
		_orderDetails = new();
		itemsDataGridView.DataSource = _orderDetails;
		HideFirstColumn();
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

		if (_orderModel != null)
		{
			customerComboBox.SelectedValue = _orderModel.CustomerId;
			_orderDetails = new BindingList<ViewOrderDetailModel>((await OrderData.LoadOrderDetailsByOrderId(_orderModel.Id)).ToList());
			itemsDataGridView.DataSource = _orderDetails;
			HideFirstColumn();
		}
	}

	private void HideFirstColumn() => itemsDataGridView.Columns[0].Visible = false;

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

		if (itemComboBox.SelectedItem is ItemModel selectedItem)
		{
			int quantity = int.TryParse(quantityTextBox.Text, out int result) ? result : 1;

			_orderDetails.Add(new ViewOrderDetailModel
			{
				ItemId = selectedItem.Id,
				ItemName = selectedItem.Name,
				ItemCode = selectedItem.Code,
				Quantity = quantity
			});
		}
		else
			MessageBox.Show("Please select a customer and an item", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (_orderDetails.Count == 0)
		{
			MessageBox.Show("Please add at least one item to the order", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		if (_orderModel is null)
		{
			await InsertIntoOrderDetailTable(await InsertIntoOrderTable());
			return;
		}

		else
		{
			int orderId = await InsertIntoOrderTable();
			await OrderData.OrderUpdate(_orderModel.Id, orderId);
			await InsertIntoOrderDetailTable(orderId);
			DialogResult = DialogResult.OK;
		}
	}

	private async Task<int> InsertIntoOrderTable() =>
		await OrderData.OrderInsert(new OrderModel
		{
			Id = 0,
			UserId = _userId,
			CustomerId = (customerComboBox.SelectedItem as CustomerModel).Id,
			DateTime = DateTime.Now
		});

	private async Task InsertIntoOrderDetailTable(int orderId)
	{
		foreach (var detail in _orderDetails)
		{
			await OrderData.OrderDetailInsert(new OrderDetailModel
			{
				Id = 0,
				OrderId = orderId,
				ItemId = detail.ItemId,
				Quantity = detail.Quantity
			});
		}

		ClearForm();
	}

	private void ClearForm()
	{
		quantityTextBox.Text = "1";
		_orderDetails.Clear();
	}
}
