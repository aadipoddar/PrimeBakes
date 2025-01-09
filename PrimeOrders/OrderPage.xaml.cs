using System.Collections.ObjectModel;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models;
using PrimeBakesLibrary.Printing;

using PrimeOrders.Services;

namespace PrimeOrders;

public partial class OrderPage : ContentPage
{
	private readonly int _userId;
	private ObservableCollection<ViewOrderDetailModel> _orders;

	public OrderPage(int userId)
	{
		InitializeComponent();

		_userId = userId;
		_orders = [];
		ordersDataGridView.ItemsSource = _orders;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		LoadComboBox();
	}

	private async void LoadComboBox()
	{
		customerComboBox.ItemsSource = await CommonData.LoadTableData<CustomerModel>("CustomerTable");
		customerComboBox.DisplayMemberPath = nameof(CustomerModel.Name);
		customerComboBox.SelectedValuePath = nameof(CustomerModel.Id);

		itemComboBox.ItemsSource = await CommonData.LoadTableData<ItemModel>("ItemTable");
		itemComboBox.DisplayMemberPath = nameof(ItemModel.Name);
		itemComboBox.SelectedValuePath = nameof(ItemModel.Id);
	}

	private void OnAddButtonClicked(object sender, EventArgs e)
	{
		if (itemComboBox.SelectedItem is not ItemModel item)
		{
			DisplayAlert("Error", "Please select an item", "OK");
			return;
		}

		var existingOrder = _orders.FirstOrDefault(o => o.ItemId == item.Id);

		if (existingOrder != null)
		{
			_orders.Remove(existingOrder);
			_orders.Add(new ViewOrderDetailModel
			{
				ItemId = item.Id,
				ItemName = item.Name,
				ItemCode = item.Code,
				Quantity = existingOrder.Quantity + (int)quantityNumericEntry.Value
			});
		}

		else _orders.Add(new ViewOrderDetailModel
		{
			ItemId = item.Id,
			ItemName = item.Name,
			ItemCode = item.Code,
			Quantity = (int)quantityNumericEntry.Value
		});

		quantityNumericEntry.Value = 1;
		itemComboBox.SelectedIndex = -1;
	}

	private void ordersDataGridView_CellDoubleTapped(object sender, Syncfusion.Maui.DataGrid.DataGridCellDoubleTappedEventArgs e) => _orders.RemoveAt(e.RowColumnIndex.RowIndex - 1);

	private async void OnSaveButtonClicked(object sender, EventArgs e)
	{
		if (customerComboBox.SelectedItem is not CustomerModel customer)
		{
			await DisplayAlert("Error", "Please select a customer", "OK");
			return;
		}

		if (_orders.Count == 0)
		{
			await DisplayAlert("Error", "Please add items to the order", "OK");
			return;
		}

		var order = new OrderModel
		{
			UserId = _userId,
			CustomerId = customer.Id,
			DateTime = DateTime.Now,
			Status = true
		};

		order.Id = await OrderData.InsertOrder(order);

		var orderDetails = _orders.Select(o => new OrderDetailModel
		{
			OrderId = order.Id,
			ItemId = o.ItemId,
			Quantity = o.Quantity
		});

		foreach (var orderDetail in orderDetails)
			await OrderData.InsertOrderDetail(orderDetail);

		await DisplayAlert("Success", "Order saved successfully", "OK");
		_orders.Clear();

		if (await DisplayAlert("Print Order", "Do you want to print the order?", "Yes", "No"))
			await PrintPDF(order.Id);

		quantityNumericEntry.Value = 1;
		customerComboBox.SelectedIndex = -1;
		itemComboBox.SelectedIndex = -1;
	}

	private async Task PrintPDF(int orderId)
	{
		MemoryStream ms = await PrintSingleOrderPDF.PrintOrder(orderId);
		SaveService saveService = new();
		saveService.SaveAndView("OrderReport.pdf", "application/pdf", ms);
	}
}
