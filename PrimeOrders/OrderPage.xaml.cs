using System.Collections.ObjectModel;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models;
using PrimeBakesLibrary.Printing;

using PrimeOrders.Services;

namespace PrimeOrders;

public partial class OrderPage : ContentPage
{
	private readonly int _userId;
	private ObservableCollection<ViewOrderDetailModel> _orderDetails = [];

	public OrderPage(int userId)
	{
		InitializeComponent();

		_userId = userId;
		itemsDataGridView.ItemsSource = _orderDetails;
	}

	#region LoadData

	protected override void OnAppearing()
	{
		base.OnAppearing();
		LoadData();
	}

	private async void LoadData()
	{
		customerComboBox.ItemsSource = await CommonData.LoadTableData<CustomerModel>("Customer");
		customerComboBox.DisplayMemberPath = nameof(CustomerModel.Name);
		customerComboBox.SelectedValuePath = nameof(CustomerModel.Id);

		categoryComboBox.ItemsSource = await CommonData.LoadTableData<CategoryModel>("Category");
		categoryComboBox.DisplayMemberPath = nameof(CategoryModel.Name);
		categoryComboBox.SelectedValuePath = nameof(CategoryModel.Id);

		await LoadItemsData();
	}

	private async Task LoadItemsData()
	{
		if (categoryComboBox.SelectedItem is CategoryModel selectedCategory)
		{
			itemComboBox.ItemsSource = (await ItemData.LoadItemByCategory(selectedCategory.Id)).ToList();
			itemComboBox.DisplayMemberPath = nameof(ItemModel.Name);
			itemComboBox.SelectedValuePath = nameof(ItemModel.Id);
		}
	}

	private async void categoryComboBox_SelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e) => await LoadItemsData();

	#endregion

	#region DataGrid

	private void OnAddButtonClicked(object sender, EventArgs e)
	{
		if (itemComboBox.SelectedItem is ItemModel selectedItem && categoryComboBox.SelectedItem is CategoryModel selectedCategory)
		{
			var existingItem = _orderDetails.FirstOrDefault(item => item.ItemId == selectedItem.Id);
			if (existingItem is not null)
			{
				_orderDetails.Remove(existingItem);
				_orderDetails.Add(new ViewOrderDetailModel
				{
					ItemId = selectedItem.Id,
					ItemName = selectedItem.Name,
					ItemCode = selectedItem.Code,
					CategoryId = selectedCategory.Id,
					CategoryName = selectedCategory.Name,
					CategoryCode = selectedCategory.Code,
					Quantity = existingItem.Quantity + (int)quantityNumericEntry.Value
				});
			}

			else _orderDetails.Add(new ViewOrderDetailModel
			{
				Id = 0,
				ItemId = selectedItem.Id,
				ItemName = selectedItem.Name,
				ItemCode = selectedItem.Code,
				CategoryId = selectedCategory.Id,
				CategoryName = selectedCategory.Name,
				CategoryCode = selectedCategory.Code,
				Quantity = (int)quantityNumericEntry.Value
			});
		}

		else DisplayAlert("Error", "Please select an item", "OK");

		quantityNumericEntry.Value = 1;
	}

	private void itemsDataGridView_CellDoubleTapped(object sender, Syncfusion.Maui.DataGrid.DataGridCellDoubleTappedEventArgs e) => _orderDetails.RemoveAt(e.RowColumnIndex.RowIndex - 1);

	#endregion

	#region Saving

	private async void OnSaveButtonClicked(object sender, EventArgs e)
	{
		if (customerComboBox.SelectedItem is not CustomerModel customer)
		{
			await DisplayAlert("Error", "Please select a customer", "OK");
			return;
		}

		if (_orderDetails.Count == 0)
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

		var orderDetails = _orderDetails.Select(o => new OrderDetailModel
		{
			OrderId = order.Id,
			ItemId = o.ItemId,
			Quantity = o.Quantity
		});

		foreach (var orderDetail in orderDetails)
			await OrderData.InsertOrderDetail(orderDetail);

		await DisplayAlert("Success", "Order saved successfully", "OK");
		_orderDetails.Clear();

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

	#endregion
}
