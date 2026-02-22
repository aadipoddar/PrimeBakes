using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Product;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class BillMobileCartPage
{
	[Parameter] public int? DiningTableId { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DiningTableModel _diningTable;

	private List<BillItemCartModel> _cart = [];
	private List<BillItemCartModel> _previousCart = [];
	private SfGrid<BillItemCartModel> _sfCartGrid;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);

		await LoadData();
		_isLoading = false;
		await SaveTransactionFile();
		StateHasChanged();
	}

	private async Task LoadData()
	{
		if (!await ResolveBillContext())
			return;

		await LoadDiningTable();
		await LoadCart();

		StateHasChanged();
	}

	private async Task<bool> ResolveBillContext()
	{
		if (!DiningTableId.HasValue)
		{
			NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
			return false;
		}

		try
		{
			var diningTable = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, DiningTableId.Value);
			if (diningTable is null)
			{
				NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
				return false;
			}

			var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(TableNames.DiningArea, diningTable.DiningAreaId);
			if (diningArea is null)
			{
				NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
				return false;
			}

			if (_user.LocationId != 1 && diningArea.LocationId != _user.LocationId)
			{
				NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
				return false;
			}

			return true;
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
			return false;
		}
	}

	private async Task LoadDiningTable()
	{
		try
		{
			if (DiningTableId.HasValue)
				_diningTable = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, DiningTableId.Value);
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
			return;
		}
	}

	private async Task LoadCart()
	{
		_cart.Clear();
		_previousCart.Clear();

		if (await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<BillItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.BillMobileCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		var products = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: _user.LocationId);
		var runningBills = await BillData.LoadRunningBillByLocationId(_user.LocationId);
		var runningBill = runningBills.FirstOrDefault(b => b.DiningTableId == DiningTableId);

		if (runningBill is not null)
		{
			var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, runningBill.Id);
			foreach (var detail in billDetails)
			{
				var cartItem = products.FirstOrDefault(p => p.ProductId == detail.ProductId);

				if (cartItem is null)
					continue;

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

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();
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

	private async Task UpdateRate(BillItemCartModel item, decimal newRate)
	{
		if (item is null || _isProcessing)
			return;

		item.Rate = Math.Max(0, newRate);
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
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
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

			if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.BillMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.DiningDashboard, true);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

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

		NavigationManager.NavigateTo($"{PageRouteNames.BillMobilePayment}/table/{_diningTable.Id}");
	}
	#endregion
}