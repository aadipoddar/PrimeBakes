using System.Collections.ObjectModel;

using Microsoft.IdentityModel.Tokens;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models;
using PrimeBakesLibrary.Printing;

using PrimeOrders.Services;

namespace PrimeOrders;

public partial class CartPage : ContentPage
{
	private readonly int _userId, _customerId;
	private readonly OrderPage _orderPage;
	private ObservableCollection<ViewOrderDetailModel> _cart = [];

	public CartPage(int userId, int customerId, ObservableCollection<ViewOrderDetailModel> cart, OrderPage orderPage)
	{
		InitializeComponent();

		_userId = userId;
		_customerId = customerId;
		_cart = cart;
		_orderPage = orderPage;

		cartDataGridView.ItemsSource = _cart;
	}

	protected override void OnAppearing()
	{
		_cart.OrderBy(x => x.CategoryId).ToList();
	}

	private async void OnSaveButtonClicked(object sender, EventArgs e)
	{
		if (_cart.IsNullOrEmpty())
		{
			await DisplayAlert("Error", "Please add items to the order", "OK");
			return;
		}

		var order = new OrderModel
		{
			UserId = _userId,
			CustomerId = _customerId,
			DateTime = DateTime.Now,
			Status = true
		};

		order.Id = await OrderData.InsertOrder(order);

		var orderDetails = _cart.Select(o => new OrderDetailModel
		{
			OrderId = order.Id,
			ItemId = o.ItemId,
			Quantity = o.Quantity
		});

		foreach (var orderDetail in orderDetails)
			await OrderData.InsertOrderDetail(orderDetail);

		await DisplayAlert("Success", "Order saved successfully", "OK");
		_cart.Clear();

		if (await DisplayAlert("Print Order", "Do you want to print the order?", "Yes", "No"))
			await PrintPDF(order.Id);

		_orderPage.cart.Clear();
		await Navigation.PopAsync();
	}

	private async Task PrintPDF(int orderId)
	{
		MemoryStream ms = await PrintSingleOrderPDF.PrintOrder(orderId);
		SaveService saveService = new();
		saveService.SaveAndView("OrderReport.pdf", "application/pdf", ms);
	}
}