using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Restaurant.Bill.Data;
using PrimeBakes.Library.Restaurant.Bill.Models;
using PrimeBakes.Library.Restaurant.Dining.Models;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class BillMobilePage
{
	[Parameter] public int? DiningTableId { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DiningTableModel _diningTable;
	private ProductCategoryModel _selectedCategory;
	private DiningAreaModel _diningArea;
	private BillModel _runningBill;
	private DateTime _now = DateTime.Now;

	private string _query = string.Empty;
	private int _page = 0;

	private List<ProductModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<BillDetailModel> _previousCart = [];
	private List<BillItemCartModel> _cart = [];

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);
		await InitializePage();
	}

	private async Task InitializePage()
	{
		_now = await CommonData.LoadCurrentDateTime();

		await ResolveBillContext();
		await LoadDiningTable();
		await LoadItems();
		await LoadCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();
	}

	private async Task ResolveBillContext()
	{
		if (!DiningTableId.HasValue)
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);

		try
		{
			var diningTable = await CommonData.LoadTableDataById<DiningTableModel>(RestaurantNames.DiningTable, DiningTableId.Value);
			if (diningTable is null)
				NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);

			var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(RestaurantNames.DiningArea, diningTable.DiningAreaId);
			if (diningArea is null)
				NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);

			if (_user.LocationId != 1 && diningArea.LocationId != _user.LocationId)
				NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);

			_diningArea = diningArea;
		}
		catch
		{
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);
		}
	}

	private async Task LoadDiningTable()
	{
		try
		{
			if (DiningTableId.HasValue)
				_diningTable = await CommonData.LoadTableDataById<DiningTableModel>(RestaurantNames.DiningTable, DiningTableId.Value);
		}
		catch
		{
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);
		}
	}

	private async Task LoadItems()
	{
		try
		{
			_products = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
			_taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);

			_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
			_productCategories.Add(new()
			{
				Id = 0,
				Name = "All"
			});
			_productCategories = [.. _productCategories.OrderBy(s => s.Name)];
			_selectedCategory = _productCategories.FirstOrDefault(s => s.Id == 0);

			var allProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: _user.LocationId);
			foreach (var product in allProducts)
				_cart.Add(new()
				{
					ItemCategoryId = product.ProductCategoryId,
					ItemId = product.ProductId,
					ItemName = product.Name,
					Quantity = 0,
					Rate = product.Rate,
					Remarks = null,
				});
			_cart = [.. _cart.OrderBy(s => s.ItemName)];
		}
		catch
		{
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);
		}
	}

	private async Task LoadCart()
	{
		try
		{
			if (await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName))
			{
				var existingCart = System.Text.Json.JsonSerializer.Deserialize<List<BillItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.BillMobileCartDataFileName));
				foreach (var item in existingCart)
				{
					_cart.FirstOrDefault(p => p.ItemId == item.ItemId)?.Quantity = item.Quantity;
					_cart.FirstOrDefault(p => p.ItemId == item.ItemId)?.Remarks = item.Remarks;
				}
			}

			var runningBills = await BillData.LoadRunningBillByLocationId(_user.LocationId);
			_runningBill = runningBills.FirstOrDefault(b => b.DiningTableId == DiningTableId);

			if (_runningBill is not null)
				_previousCart = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, _runningBill.Id);
		}
		catch
		{
			await DataStorageService.LocalRemove(StorageFileNames.BillMobileDataFileName);
			await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);
		}
		finally
		{
			await SaveTransactionFile();
		}
	}

	private string Elapsed(DateTime since)
	{
		var span = _now - since;
		if (span < TimeSpan.Zero)
			span = TimeSpan.Zero;

		return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes}m" : $"{(int)span.TotalMinutes}m";
	}
	#endregion

	#region Filter / Page
	private List<BillItemCartModel> GetFilteredProducts()
	{
		IEnumerable<BillItemCartModel> query = _cart;

		if (_selectedCategory is not null && _selectedCategory.Id != 0)
			query = query.Where(x => x.ItemCategoryId == _selectedCategory.Id);

		if (!string.IsNullOrWhiteSpace(_query))
			query = query.Where(x =>
			x.ItemName is not null &&
			x.ItemName.Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase));

		return [.. query];
	}

	private void OnQueryChange(string value)
	{
		_query = value ?? string.Empty;
		_page = 0;
	}

	private void SelectCategory(ProductCategoryModel category)
	{
		_selectedCategory = category;
		_page = 0;
	}
	#endregion

	#region Cart
	private async Task AddToCart(BillItemCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveTransactionFile();
	}

	private async Task UpdateQuantity(BillItemCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveTransactionFile();
	}

	private async Task OnQuantityInput(BillItemCartModel item, string raw)
	{
		if (item is null)
			return;

		var digits = string.IsNullOrWhiteSpace(raw)
			? string.Empty
			: new string([.. raw.Where(char.IsDigit)]);

		decimal value = 0;
		if (!string.IsNullOrWhiteSpace(digits) && decimal.TryParse(digits, out var parsed))
			value = Math.Min(9999, parsed);

		item.Quantity = value;
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			BillData.ApplyItemFinancialDetails(_cart, _products, _taxes);

			if (!_cart.Any(x => x.Quantity > 0))
				await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.BillMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch
		{
			await DataStorageService.LocalRemove(StorageFileNames.BillMobileDataFileName);
			await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task GoToCart()
	{
		await SaveTransactionFile();

		var hasCurrentCart = _cart.Sum(x => x.Quantity) > 0 && await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName);
		if (!hasCurrentCart && _previousCart.Count == 0)
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo($"{RestaurantRouteNames.BillMobileCart}/table/{_diningTable.Id}");
	}
	#endregion
}
