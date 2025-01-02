using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;

using PrimeBakes.Printing;

using PrimeBakesLibrary.Printing;

namespace PrimeBakes.Forms;

public partial class OrderForm : Form
{
	private int _orderId;
	private readonly int _userId;
	private readonly OrderModel _orderModel;
	private BindingList<ViewOrderDetailModel> _orderDetails;

	public OrderForm(int userId)
	{
		InitializeComponent();

		_userId = userId;
		_orderDetails = new();
		itemsDataGridView.DataSource = _orderDetails;
		printPDFButton.Visible = false;
		printThermalButton.Visible = false;
		statusCheckBox.Visible = false;
		HideFirstColumn();
	}

	public OrderForm(OrderModel orderModel)
	{
		InitializeComponent();

		_orderModel = orderModel;
		_orderId = _orderModel.Id;
		_userId = orderModel.UserId;
		_orderDetails = new();
		itemsDataGridView.DataSource = _orderDetails;
		printPDFButton.Visible = true;
		printThermalButton.Visible = true;
		statusCheckBox.Visible = true;
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

		if (_orderModel is not null)
		{
			customerComboBox.SelectedValue = _orderModel.CustomerId;
			_orderDetails = new BindingList<ViewOrderDetailModel>((await OrderData.LoadViewOrderDetailsByOrderId(_orderModel.Id)).ToList());
			itemsDataGridView.DataSource = _orderDetails;
			statusCheckBox.Checked = _orderModel.Status;
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

			var existingItem = _orderDetails.FirstOrDefault(item => item.ItemId == selectedItem.Id);
			if (existingItem != null)
			{
				existingItem.Quantity += quantity;
				itemsDataGridView.Refresh();
			}
			else
			{
				_orderDetails.Add(new ViewOrderDetailModel
				{
					ItemId = selectedItem.Id,
					ItemName = selectedItem.Name,
					ItemCode = selectedItem.Code,
					Quantity = quantity
				});
			}
		}

		else MessageBox.Show("Please select a customer and an item", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	private void itemsDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex >= 0)
		{
			itemComboBox.SelectedValue = _orderDetails[e.RowIndex].ItemId;
			quantityTextBox.Text = _orderDetails[e.RowIndex].Quantity.ToString();
			_orderDetails.RemoveAt(e.RowIndex);
		}
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
			_orderId = await InsertIntoOrderTable();
			await InsertIntoOrderDetailTable(_orderId);
		}

		else
		{
			await UpdateOrderTable();
			await DeleteOrderDetailTable();
			await InsertIntoOrderDetailTable(_orderId);
			DialogResult = DialogResult.OK;
		}

		if (MessageBox.Show("Do you want to print the order?", "Print Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
		{
			var printMethod = MessageBox.Show("Choose print method: Yes for PDF, No for Thermal", "Print Method", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (printMethod == DialogResult.Yes) printPDFButton_Click(sender, e);
			else if (printMethod == DialogResult.No) printThermalButton_Click(sender, e);
		}
	}

	private async Task DeleteOrderDetailTable() =>
		await OrderData.OrderDetailsDelete(_orderId);

	private async Task UpdateOrderTable() =>
		await OrderData.OrderUpdate(new OrderModel
		{
			Id = _orderId,
			UserId = _userId,
			CustomerId = (customerComboBox.SelectedItem as CustomerModel).Id,
			DateTime = DateTime.Now,
			Status = statusCheckBox.Checked
		});

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

	private async void printPDFButton_Click(object sender, EventArgs e)
	{
		MemoryStream ms = await PrintSingleOrderPDF.PrintOrder(_orderModel.Id);
		using (FileStream stream = new(Path.Combine(Path.GetTempPath(), "OrderReport.pdf"), FileMode.Create, FileAccess.Write))
			ms.WriteTo(stream);
		Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\OrderReport.pdf") { UseShellExecute = true });
	}

	private void printThermalButton_Click(object sender, EventArgs e)
	{
		PrintDialog printDialog = new();
		printDialog.Document = printDocument;
		printDocument.Print();
	}

	private void printDocument_PrintPage(object sender, PrintPageEventArgs e) => PrintOrderThermal.DrawGraphics(e, _orderId);
}
