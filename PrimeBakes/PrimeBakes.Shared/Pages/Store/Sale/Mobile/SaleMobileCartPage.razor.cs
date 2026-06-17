using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Data;
using PrimeBakesLibrary.Store.Sale.Models;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Store.Sale.Mobile;

public partial class SaleMobileCartPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private string _query = string.Empty;
	private int _page = 0;

	private List<ProductModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<SaleItemCartModel> _cart = [];

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);
		await LoadData();
	}

	private async Task LoadData()
	{
		_products = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
		_taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);

		if (await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
			_cart = JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileCartDataFileName)) ?? [];

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		_isLoading = false;
		await SaveTransactionFile();
		StateHasChanged();
	}
	#endregion

	#region Filter / Page
	private List<SaleItemCartModel> GetFilteredCart()
	{
		IEnumerable<SaleItemCartModel> query = _cart;

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
	#endregion

	#region Cart
	private async Task UpdateQuantity(SaleItemCartModel item, decimal newQuantity)
	{
		if (item is null || _isProcessing)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveTransactionFile();
	}

	private async Task OnQuantityInput(SaleItemCartModel item, string raw)
	{
		if (item is null || _isProcessing)
			return;

		var digits = string.IsNullOrWhiteSpace(raw)
			? string.Empty
			: new string([.. raw.Where(char.IsDigit)]);

		decimal value = 0;
		if (!string.IsNullOrWhiteSpace(digits) && decimal.TryParse(digits, out var parsed))
			value = Math.Min(9999, parsed);

		item.Quantity = value;

		if (item.Quantity == 0)
			_cart.Remove(item);

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

			await SaleData.ApplyItemFinancialDetails(_cart, _products, _taxes);

			if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileCartDataFileName, JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(StoreRouteNames.SaleMobile, true);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task GoToPayment()
	{
		await SaveTransactionFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || !await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo(StoreRouteNames.SaleMobilePayment);
	}
	#endregion
}
