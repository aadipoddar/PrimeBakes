using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;
using PrimeBakesLibrary.Models.Store.Sale;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Sale.Mobile;

public partial class SaleMobileCartPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private readonly SaleModel _sale = new();

	private List<SaleItemCartModel> _cart = [];

	private SfGrid<SaleItemCartModel> _sfCartGrid;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);
		await LoadData();
		_isLoading = false;
		await SaveTransactionFile();
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_cart.Clear();

		if (await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();

		StateHasChanged();
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

	private async Task UpdateRate(SaleItemCartModel item, decimal newRate)
	{
		if (item is null || _isProcessing)
			return;
		item.Rate = Math.Max(0, newRate);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);
		var items = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		foreach (var item in _cart.Where(_ => _.Quantity > 0))
		{
			item.DiscountPercent = 0;
			item.DiscountAmount = 0;

			item.BaseTotal = item.Rate * item.Quantity;
			item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

			var selectedItem = items.FirstOrDefault(s => s.Id == item.ItemId);
			var tax = taxes.FirstOrDefault(s => s.Id == selectedItem.TaxId);

			item.CGSTPercent = tax?.CGST ?? 0;
			item.SGSTPercent = tax?.SGST ?? 0;
			item.IGSTPercent = 0;
			item.InclusiveTax = tax?.Inclusive ?? false;

			if (item.InclusiveTax)
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / (100 + item.CGSTPercent));
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / (100 + item.SGSTPercent));
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / (100 + item.IGSTPercent));
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount;
			}
			else
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount + item.TotalTaxAmount;
			}

			item.NetRate = item.Total / item.Quantity;
			item.Remarks = null;
		}
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.SaleMobile, true);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion
}