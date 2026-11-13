using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Data.Store.Product;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;

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
	private const int _favouritesCategoryId = -1;

	private List<ProductModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<SaleItemCartModel> _cart = [];

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthService.ValidateUser([UserRoles.Store], pageRoute: StoreRouteNames.SaleMobile);
		await InitializePage();
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadExistingCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();
		_ = LoadRecommendations();
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

			var allProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, _user.LocationId, DateOnly.FromDateTime(await CommonData.LoadCurrentDateTime()));
			foreach (var product in allProducts)
				_cart.Add(new()
				{
					ItemCategoryId = product.ProductCategoryId,
					ItemId = product.ProductId,
					ItemName = product.Name,
					Quantity = 0,
					Rate = product.Rate,
					Remarks = null,
					Favourite = false,
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

	#region Recommendations
	private async Task LoadRecommendations()
	{
		var recommendedIds = await GetRecommendedIds();

		foreach (var item in _cart)
			item.Favourite = recommendedIds.Contains(item.ItemId);

		if (_cart.Any(x => x.Favourite) && !_productCategories.Any(c => c.Id == _favouritesCategoryId))
			_productCategories.Insert(1, new() { Id = _favouritesCategoryId, Name = "Favourites" });

		await InvokeAsync(StateHasChanged);
	}

	private async Task<HashSet<int>> GetRecommendedIds()
	{
		var fileName = StorageFileNames.SaleMobileRecommendationDataFileName;

		// 1. Use today's cached list if there is one
		try
		{
			if (await DataStorageService.LocalExists(fileName))
			{
				var cached = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemRecommendationModel>>(await DataStorageService.LocalGetAsync(fileName));
				if (cached is { Count: > 0 } && cached[0].FetchedDateTime.Date == DateTime.Today)
					return [.. cached.Select(item => item.ItemId)];
			}
		}
		catch { }

		// 2. Otherwise fetch fresh and cache it
		try
		{
			await DataStorageService.LocalRemove(fileName);

			var recommendations = (await AnalysisData.LoadProductRecommendations(_user.LocationId, isSale: true))
				.Select(product => new SaleItemRecommendationModel { ItemId = product.Id, FetchedDateTime = DateTime.Now })
				.ToList();

			await DataStorageService.LocalSaveAsync(fileName, System.Text.Json.JsonSerializer.Serialize(recommendations));

			return [.. recommendations.Select(item => item.ItemId)];
		}
		catch { }

		return [];
	}
	#endregion

	#region Filter / Page
	private List<SaleItemCartModel> GetFilteredProducts()
	{
		IEnumerable<SaleItemCartModel> query = _cart;

		if (_selectedCategory?.Id == _favouritesCategoryId)
			query = query.Where(x => x.Favourite);
		else if (_selectedCategory is not null && _selectedCategory.Id != 0)
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

			_cart.ApplyItemFinancialDetails(_products, _taxes);

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
