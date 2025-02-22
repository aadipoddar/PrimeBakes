using System.Collections.ObjectModel;

using Microsoft.IdentityModel.Tokens;

namespace PrimeOrders;

public partial class OrderPage : ContentPage
{
	public readonly int _userId;
	public int _customerId;
	private readonly ObservableCollection<ViewOrderDetailModel> _items = [];
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
		var user = await CommonData.LoadTableDataById<UserModel>(Table.User, _userId);
		var customer = await CommonData.LoadTableDataById<CustomerModel>(Table.Customer, user.CustomerId);
		customerNameLabel.Text = customer.Name;
		_customerId = customer.Id;

		categoryComboBox.ItemsSource = await CommonData.LoadTableData<ItemCategoryModel>(Table.ItemCategory);
		categoryComboBox.DisplayMemberPath = nameof(ItemCategoryModel.DisplayName);
		categoryComboBox.SelectedValuePath = nameof(ItemCategoryModel.Id);

		_items.Clear();
	}

	private async Task LoadItemsData()
	{
		if (categoryComboBox.SelectedItem is ItemCategoryModel selectedCategory)
		{
			_items.Clear();

			var user = await CommonData.LoadTableDataById<UserModel>(Table.User, _userId);
			var items = await ItemData.LoadItemByCategory(selectedCategory.Id, user.UserCategoryId);

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
		if ((sender as Syncfusion.Maui.Inputs.SfNumericEntry).Parent.BindingContext is not ViewOrderDetailModel item) return;

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

		Navigation.PushAsync(new CartPage(this));
	}
}
