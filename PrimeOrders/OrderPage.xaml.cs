using System.Collections.ObjectModel;

using Microsoft.IdentityModel.Tokens;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models;

namespace PrimeOrders;

public partial class OrderPage : ContentPage
{
	public readonly int _userId;
	public int customerId = 0;
	private ObservableCollection<ViewOrderDetailModel> _items = [];
	public ObservableCollection<ViewOrderDetailModel> cart = [];

	public OrderPage(int userId)
	{
		InitializeComponent();

		_userId = userId;
		itemsCollectionView.ItemsSource = _items;
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

		if (customerId is not 0)
			customerComboBox.SelectedValue = customerId;

		categoryComboBox.ItemsSource = await CommonData.LoadTableData<CategoryModel>("Category");
		categoryComboBox.DisplayMemberPath = nameof(CategoryModel.Name);
		categoryComboBox.SelectedValuePath = nameof(CategoryModel.Id);

		_items.Clear();
	}

	private async Task LoadItemsData()
	{
		if (categoryComboBox.SelectedItem is CategoryModel selectedCategory)
		{
			_items.Clear();

			var items = (await ItemData.LoadItemByCategory(selectedCategory.Id)).ToList();

			foreach (var item in items)
			{
				var existingCart = cart.FirstOrDefault(x => x.ItemId == item.Id);
				if (existingCart is not null) _items.Add(existingCart);

				else _items.Add(new ViewOrderDetailModel
				{
					ItemId = item.Id,
					ItemCode = item.Code,
					ItemName = item.Name,
					CategoryId = selectedCategory.Id,
					CategoryCode = selectedCategory.Code,
					CategoryName = selectedCategory.Name,
					Quantity = 0
				});
			}
		}
	}

	private async void categoryComboBox_SelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e) => await LoadItemsData();

	#endregion

	private void quantityNumericEntry_ValueChanged(object sender, Syncfusion.Maui.Inputs.NumericEntryValueChangedEventArgs e)
	{
		var item = (sender as Syncfusion.Maui.Inputs.SfNumericEntry).Parent.BindingContext as ViewOrderDetailModel;
		if (item is null) return;

		var quantity = Convert.ToInt32(e.NewValue);

		var existingCart = cart.FirstOrDefault(x => x.ItemId == item.ItemId);
		if (existingCart is not null) cart.Remove(existingCart);

		if (quantity is not 0)
			cart.Add(new ViewOrderDetailModel
			{
				ItemId = item.ItemId,
				ItemCode = item.ItemCode,
				ItemName = item.ItemName,
				CategoryId = item.CategoryId,
				CategoryCode = item.CategoryCode,
				CategoryName = item.CategoryName,
				Quantity = quantity
			});
	}

	private void OnCartButtonClicked(object sender, EventArgs e)
	{
		if (cart.IsNullOrEmpty())
			DisplayAlert("Error", "Please Add Items to Cart", "OK");

		else if (customerComboBox.SelectedItem is CustomerModel selectedCustomer)
		{
			customerId = selectedCustomer.Id;
			Navigation.PushAsync(new CartPage(this));
		}

		else DisplayAlert("Error", "Please Select a Customer", "OK");
	}

	private void OnLogoutButtonClicked(object sender, EventArgs e)
	{
	}
}
