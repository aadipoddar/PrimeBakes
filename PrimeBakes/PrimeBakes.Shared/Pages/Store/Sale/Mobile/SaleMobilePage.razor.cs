using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Store.Sale.Data;
using PrimeBakes.Library.Store.Sale.Models;

namespace PrimeBakes.Shared.Pages.Store.Sale.Mobile;

public partial class SaleMobilePage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private ProductCategoryModel _selectedCategory;
	private LocationModel _location;
	private string _query = string.Empty;
	private int _page = 0;

	private List<ProductModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<SaleItemCartModel> _cart = [];

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);
		await InitializePage();
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadExistingCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();
	}

	private async Task LoadData()
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
			_productCategories = [.. _productCategories.OrderBy(s => s.Id == 0 ? 0 : 1).ThenBy(s => s.Name)];
			_selectedCategory = _productCategories.FirstOrDefault(s => s.Id == 0);

			_location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, _user.LocationId);

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
			NavigationManager.NavigateTo(OperationRouteNames.Dashboard, true);
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			if (await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
			{
				var existingCart = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileCartDataFileName));
				foreach (var item in existingCart)
					_cart.FirstOrDefault(p => p.ItemId == item.ItemId)?.Quantity = item.Quantity;
			}
		}
		catch
		{
			await DataStorageService.LocalRemove(StorageFileNames.SaleMobileDataFileName);
			await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
			NavigationManager.NavigateTo(OperationRouteNames.Dashboard, true);
		}
		finally
		{
			await SaveTransactionFile();
		}
	}
	#endregion

	#region Filter / Page
	private List<SaleItemCartModel> GetFilteredProducts()
	{
		IEnumerable<SaleItemCartModel> query = _cart;

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
	private async Task AddToCart(SaleItemCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveTransactionFile();
	}

	private async Task UpdateQuantity(SaleItemCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveTransactionFile();
	}

	private async Task OnQuantityInput(SaleItemCartModel item, string raw)
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

			SaleData.ApplyItemFinancialDetails(_cart, _products, _taxes);

			if (!_cart.Any(x => x.Quantity > 0))
				await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch
		{
			await DataStorageService.LocalRemove(StorageFileNames.SaleMobileDataFileName);
			await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
			NavigationManager.NavigateTo(OperationRouteNames.Dashboard, true);
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

		if (_cart.Sum(x => x.Quantity) <= 0 || !await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo(StoreRouteNames.SaleMobileCart);
	}
	#endregion
}
