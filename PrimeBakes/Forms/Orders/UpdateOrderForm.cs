using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

using PrimeBakesLibrary.Printing;

namespace PrimeBakes.Forms.Orders;

public partial class UpdateOrderForm : Form
{
	private int _customerId;
	private readonly int _userId;
	private readonly OrderModel _orderModel;
	private BindingList<ViewOrderDetailModel> _orderDetails = [];

	public UpdateOrderForm(OrderModel orderModel)
	{
		InitializeComponent();

		_orderModel = orderModel;
		_userId = orderModel.UserId;
		itemsDataGridView.DataSource = _orderDetails;
	}

	#region LoadData

	private void OrderForm_Load(object sender, EventArgs e) => LoadData();

	private async void LoadData()
	{
		var user = await CommonData.LoadTableDataById<UserModel>(Table.User, _userId);
		var customer = await CommonData.LoadTableDataById<CustomerModel>(Table.Customer, user.CustomerId);
		Text = $"Update Order - {customer.DisplayName}";
		_customerId = customer.Id;

		categoryComboBox.DataSource = await CommonData.LoadTableData<CategoryModel>(Table.Category);
		categoryComboBox.DisplayMember = nameof(CategoryModel.DisplayName);
		categoryComboBox.ValueMember = nameof(CategoryModel.Id);

		await LoadItemsData();

		_orderDetails = new BindingList<ViewOrderDetailModel>(await OrderData.LoadOrderDetailsByOrderId(_orderModel.Id));
		itemsDataGridView.DataSource = _orderDetails;
		statusCheckBox.Checked = _orderModel.Status;
		HideColumns();

		richTextBoxFooter.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
	}

	private async Task LoadItemsData()
	{
		itemComboBox.DataSource = await ItemData.LoadItemByCategory((categoryComboBox.SelectedItem as CategoryModel).Id);
		itemComboBox.DisplayMember = nameof(ItemModel.DisplayName);
		itemComboBox.ValueMember = nameof(ItemModel.Id);
	}

	private async void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e) => await LoadItemsData();

	#endregion

	#region DataGrid

	private void HideColumns()
	{
		itemsDataGridView.Columns[0].Visible = false;
		itemsDataGridView.Columns[8].Visible = false;
	}

	private void quantityTextBox_KeyPress(object sender, KeyPressEventArgs e)
	{
		if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
			e.Handled = true;
	}

	private void addButton_Click(object sender, EventArgs e) => AddItemToDataGridView();

	private void AddItemToDataGridView()
	{
		if (int.Parse(quantityTextBox.Text) == 0) quantityTextBox.Text = "1";

		if (itemComboBox.SelectedItem is ItemModel selectedItem && categoryComboBox.SelectedItem is CategoryModel selectedCategory)
		{
			int quantity = int.TryParse(quantityTextBox.Text, out int result) ? result : 1;

			var existingItem = _orderDetails.FirstOrDefault(item => item.ItemId == selectedItem.Id);
			if (existingItem != null)
			{
				existingItem.Quantity += quantity;
				itemsDataGridView.Refresh();
			}
			else _orderDetails.Add(new ViewOrderDetailModel
			{
				ItemId = selectedItem.Id,
				ItemName = selectedItem.Name,
				ItemCode = selectedItem.Code,
				CategoryId = selectedCategory.Id,
				CategoryName = selectedCategory.Name,
				CategoryCode = selectedCategory.Code,
				Quantity = quantity
			});
		}

		else MessageBox.Show("Please select a customer and an item", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

		quantityTextBox.Text = "1";
		categoryComboBox.Focus();
	}

	private async void itemsDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex >= 0)
		{
			categoryComboBox.SelectedValue = _orderDetails[e.RowIndex].CategoryId;
			await LoadItemsData();
			itemComboBox.SelectedValue = _orderDetails[e.RowIndex].ItemId;
			quantityTextBox.Text = _orderDetails[e.RowIndex].Quantity.ToString();
			_orderDetails.RemoveAt(e.RowIndex);
			quantityTextBox.Focus();
		}
	}

	#endregion

	#region Saving

	private async void saveButton_Click(object sender, EventArgs e)
	{
		if (_orderDetails.Count == 0)
		{
			MessageBox.Show("Please add at least one item to the order", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		await UpdateOrderTable();
		await DeleteOrderDetailTable();
		await InsertIntoOrderDetailTable(_orderModel.Id);
		DialogResult = DialogResult.OK;

		if (MessageBox.Show("Do you want to print the order?", "Print Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			printPDFButton_Click(sender, e);
	}

	private async Task DeleteOrderDetailTable() =>
		await OrderData.DeleteOrderDetails(_orderModel.Id);

	private async Task UpdateOrderTable() =>
		await OrderData.UpdateOrder(new OrderModel
		{
			Id = _orderModel.Id,
			UserId = _userId,
			CustomerId = _customerId,
			DateTime = DateTime.Now,
			Status = statusCheckBox.Checked
		});

	private async Task InsertIntoOrderDetailTable(int orderId)
	{
		foreach (var detail in _orderDetails)
		{
			await OrderData.InsertOrderDetail(new OrderDetailModel
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

	private async void printPDFButton_Click(object sender, EventArgs e)
	{
		MemoryStream ms = await PrintSingleOrderPDF.PrintOrder(_orderModel.Id);
		using FileStream stream = new(Path.Combine(Path.GetTempPath(), "OrderReport.pdf"), FileMode.Create, FileAccess.Write);
		await ms.CopyToAsync(stream);
		ms.Close();
		Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\OrderReport.pdf") { UseShellExecute = true });
	}

	#endregion
}
