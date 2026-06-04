using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Order;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakes.Shared.Pages.Store.Order;

public partial class OrderMobilePage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private ProductCategoryModel _selectedCategory;
	private LocationModel _location;
	private string _query = string.Empty;
	private int _page = 0;

	private List<ProductCategoryModel> _productCategories = [];
	private List<OrderItemCartModel> _cart = [];

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
		await SaveOrderFile();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		try
		{
			_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
			_productCategories.Add(new()
			{
				Id = 0,
				Name = "All"
			});
			_productCategories = [.. _productCategories.OrderBy(s => s.Id == 0 ? 0 : 1).ThenBy(s => s.Name)];
			_selectedCategory = _productCategories.FirstOrDefault(s => s.Id == 0);

			_location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);

			var mainLocationProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: 1);
			var orderLocationProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: _user.LocationId);
			var allProducts = mainLocationProducts.Where(x => orderLocationProducts.Any(y => y.ProductId == x.ProductId)).DistinctBy(x => x.ProductId).ToList();
			foreach (var product in allProducts)
				_cart.Add(new()
				{
					ItemCategoryId = product.ProductCategoryId,
					ItemId = product.ProductId,
					ItemName = product.Name,
					Remarks = null,
					Quantity = 0
				});
			_cart = [.. _cart.OrderBy(s => s.ItemName)];
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.Dashboard);
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			if (await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
			{
				var existingCart = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileCartDataFileName));
				foreach (var item in existingCart)
					_cart.FirstOrDefault(p => p.ItemId == item.ItemId)?.Quantity = item.Quantity;
			}
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.OrderMobile, true);
			await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
		}
		finally
		{
			await SaveOrderFile();
		}
	}
	#endregion

	#region Filter / Page
	private List<OrderItemCartModel> GetFilteredProducts()
	{
		IEnumerable<OrderItemCartModel> query = _cart;

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
	private async Task AddToCart(OrderItemCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveOrderFile();
	}

	private async Task UpdateQuantity(OrderItemCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveOrderFile();
	}

	private async Task OnQuantityInput(OrderItemCartModel item, string raw)
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
		await SaveOrderFile();
	}

	private async Task SaveOrderFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.OrderMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.OrderMobile, true);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task GoToCart()
	{
		await SaveOrderFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || !await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo(PageRouteNames.OrderMobileCart);
	}
	#endregion
}
