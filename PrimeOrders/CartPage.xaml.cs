using System.Collections.ObjectModel;

using Microsoft.IdentityModel.Tokens;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models;
using PrimeBakesLibrary.Printing;

using PrimeOrders.Services;

namespace PrimeOrders;

public partial class CartPage : ContentPage
{
	private readonly OrderPage _orderPage;
	private readonly ObservableCollection<ViewOrderDetailModel> _cart = [];

	public CartPage(OrderPage orderPage)
	{
		InitializeComponent();

		_orderPage = orderPage;
		_cart = orderPage.cart;

		CreateCategoryCollectionViews();
	}

	private void CreateCategoryCollectionViews()
	{
		var groupedItems = _cart.GroupBy(item => item.CategoryId);

		foreach (var group in groupedItems)
		{
			var categoryLabel = new Label
			{
				Text = $"Category: {group.FirstOrDefault().CategoryName}",
				FontAttributes = FontAttributes.Bold,
				FontSize = 16
			};

			var collectionView = new CollectionView
			{
				ItemsSource = group.ToList(),
				Margin = new Thickness(0, 0, 0, 5),
				ItemTemplate = new DataTemplate(() =>
				{
					var grid = new Grid
					{
						Padding = new Thickness(10),
						ColumnSpacing = 10,
						ColumnDefinitions = {
							new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
							new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
							new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }
						}
					};

					var itemNameLabel = new Label();
					itemNameLabel.SetBinding(Label.TextProperty, nameof(ViewOrderDetailModel.ItemName));

					var itemCodeLabel = new Label();
					itemCodeLabel.SetBinding(Label.TextProperty, nameof(ViewOrderDetailModel.ItemCode));

					var quantityLabel = new Label();
					quantityLabel.SetBinding(Label.TextProperty, nameof(ViewOrderDetailModel.Quantity));

					grid.Add(itemNameLabel, 0, 0);
					grid.Add(itemCodeLabel, 1, 0);
					grid.Add(quantityLabel, 2, 0);

					return grid;
				})
			};

			cartStackLayout.Children.Add(categoryLabel);
			cartStackLayout.Children.Add(collectionView);
		}
	}

	private async Task<int> InsertIntoOrderTable() =>
		await OrderData.InsertOrder(new OrderModel
		{
			UserId = _orderPage._userId,
			CustomerId = _orderPage._customerId,
			DateTime = DateTime.Now,
			Status = true
		});

	private async Task InsertIntoOrderDetailTable(int orderId)
	{
		var orderDetails = _cart.Select(o => new OrderDetailModel
		{
			OrderId = orderId,
			ItemId = o.ItemId,
			Quantity = o.Quantity
		});

		foreach (var orderDetail in orderDetails)
			await OrderData.InsertOrderDetail(orderDetail);
	}

	private async void OnSaveButtonClicked(object sender, EventArgs e)
	{
		if (_cart.IsNullOrEmpty())
		{
			await DisplayAlert("Error", "Please add items to the order", "OK");
			return;
		}

		int orderId = await InsertIntoOrderTable();
		await InsertIntoOrderDetailTable(orderId);

		string filePath = await PrintPDF(orderId);
		string customerEmail = (await CommonData.LoadTableDataById<CustomerModel>("Customer", _orderPage._customerId)).FirstOrDefault().Email;
		Mailing.MailPDF(customerEmail, filePath);

		_cart.Clear();
		_orderPage.cart.Clear();
		await Navigation.PopAsync();
	}

	private async Task<string> PrintPDF(int orderId)
	{
		MemoryStream ms = await PrintSingleOrderPDF.PrintOrder(orderId);
		SaveService saveService = new();
		return saveService.SaveAndView("OrderReport.pdf", "application/pdf", ms);
	}
}