using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Restaurant.Bill.Data;
using PrimeBakes.Library.Restaurant.Bill.Models;
using PrimeBakes.Library.Restaurant.Dining.Models;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class BillMobileCartPage
{
	[Parameter] public int? DiningTableId { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private string _query = string.Empty;
	private int _page = 0;

	private DiningTableModel _diningTable;
	private DiningAreaModel _diningArea;
	private BillModel _runningBill;

	private List<ProductModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<BillItemCartModel> _cart = [];
	private List<BillItemCartModel> _previousCart = [];

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);

		await LoadData();
	}

	private async Task LoadData()
	{
		await ResolveBillContext();
		await LoadDiningTable();
		await LoadCart();

		_isLoading = false;
		await SaveTransactionFile();
		StateHasChanged();
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

	private async Task LoadCart()
	{
		_products = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
		_taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);

		_previousCart.Clear();

		if (await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName))
			_cart = JsonSerializer.Deserialize<List<BillItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.BillMobileCartDataFileName)) ?? [];

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		var products = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, _user.LocationId, DateOnly.FromDateTime(await CommonData.LoadCurrentDateTime()));
		var runningBills = await BillData.LoadRunningBillByLocationId(_user.LocationId);
		_runningBill = runningBills.FirstOrDefault(b => b.DiningTableId == DiningTableId);

		if (_runningBill is not null)
		{
			var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, _runningBill.Id);
			foreach (var detail in billDetails)
			{
				var cartItem = products.FirstOrDefault(p => p.ProductId == detail.ProductId);
				_previousCart.Add(new()
				{
					ItemId = detail.ProductId,
					ItemName = cartItem.Name,
					Quantity = detail.Quantity,
					Rate = cartItem.Rate,
					Total = detail.Total,
					Remarks = detail.Remarks
				});
			}

			_previousCart = [.. _previousCart.OrderBy(x => x.ItemName)];
		}
	}
	#endregion

	#region Filter / Page
	private List<BillItemCartModel> GetFilteredCart()
	{
		IEnumerable<BillItemCartModel> query = _cart;

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
	private async Task UpdateQuantity(BillItemCartModel item, decimal newQuantity)
	{
		if (item is null || _isProcessing)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveTransactionFile();
	}

	private async Task OnQuantityInput(BillItemCartModel item, string raw)
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

	private async Task UpdateRemarks(BillItemCartModel item, string newRemarks)
	{
		if (item is null || _isProcessing)
			return;

		item.Remarks = string.IsNullOrWhiteSpace(newRemarks) ? null : newRemarks.Trim();
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
				await DataStorageService.LocalSaveAsync(StorageFileNames.BillMobileCartDataFileName, JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch
		{
			await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);
			await DataStorageService.LocalRemove(StorageFileNames.BillMobileDataFileName);
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningDashboard, true);
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

		if (_cart.Count == 0 && _previousCart.Count == 0)
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo($"{RestaurantRouteNames.BillMobilePayment}/table/{_diningTable.Id}");
	}
	#endregion
}
