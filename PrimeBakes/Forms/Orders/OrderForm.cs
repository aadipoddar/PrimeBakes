using System.ComponentModel;
using System.Diagnostics;

using PrimeBakesLibrary.Printing;

namespace PrimeBakes.Forms.Orders;

public partial class OrderForm : Form
{
	private int _orderId;
	private readonly int _userId;
	private BindingList<ViewOrderDetailModel> _orderDetails = [];

	public OrderForm(int userId)
	{
		InitializeComponent();

		_userId = userId;
		itemsDataGridView.DataSource = _orderDetails;
	}

	#region LoadData

	private void OrderForm_Load(object sender, EventArgs e) => LoadData();

	private async void LoadData()
	{
		customerComboBox.DataSource = (await CommonData.LoadTableData<CustomerModel>("Customer")).ToList();
		customerComboBox.DisplayMember = nameof(CustomerModel.DisplayName);
		customerComboBox.ValueMember = nameof(CustomerModel.Id);

		categoryComboBox.DataSource = (await CommonData.LoadTableData<CategoryModel>("Category")).ToList();
		categoryComboBox.DisplayMember = nameof(CategoryModel.DisplayName);
		categoryComboBox.ValueMember = nameof(CategoryModel.Id);

		await LoadItemsData();
		HideFirstColumn();
	}

	private async Task LoadItemsData()
	{
		itemComboBox.DataSource = (await ItemData.LoadItemByCategory((categoryComboBox.SelectedItem as CategoryModel).Id)).ToList();
		itemComboBox.DisplayMember = nameof(ItemModel.DisplayName);
		itemComboBox.ValueMember = nameof(ItemModel.Id);
	}

	private async void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e) => await LoadItemsData();

	#endregion

	#region DataGrid

	private void HideFirstColumn() => itemsDataGridView.Columns[0].Visible = false;

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

		_orderId = await InsertIntoOrderTable();
		await InsertIntoOrderDetailTable(_orderId);

		if (MessageBox.Show("Do you want to print the order?", "Print Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			printPDFButton_Click(sender, e);
	}

	private async Task<int> InsertIntoOrderTable() =>
		await OrderData.InsertOrder(new OrderModel
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
		customerComboBox.Focus();
	}

	private async void printPDFButton_Click(object sender, EventArgs e)
	{
		MemoryStream ms = await PrintSingleOrderPDF.PrintOrder(_orderId);
		using (FileStream stream = new(Path.Combine(Path.GetTempPath(), "OrderReport.pdf"), FileMode.Create, FileAccess.Write))
			ms.WriteTo(stream);
		Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\OrderReport.pdf") { UseShellExecute = true });
	}

	#endregion
}
