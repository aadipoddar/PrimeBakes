using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Order.Data;
using PrimeBakesLibrary.Store.Order.Models;

namespace PrimeBakes.Shared.Pages.Store.Order.Mobile;

public partial class OrderMobileCartPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showValidationDialog = false;

	private string _orderRemarks = string.Empty;
	private string _query = string.Empty;
	private int _page = 0;

	private List<OrderItemCartModel> _cart = [];
	private readonly List<(string Field, string Message)> _validationErrors = [];

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
		_cart.Clear();

		if (await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		_isLoading = false;
		StateHasChanged();
	}
	#endregion

	#region Filter / Page
	private List<OrderItemCartModel> GetFilteredCart()
	{
		IEnumerable<OrderItemCartModel> query = _cart;

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

	#region Products
	private async Task UpdateQuantity(OrderItemCartModel item, decimal newQuantity)
	{
		if (item is null || _isProcessing)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveOrderFile();
	}

	private async Task OnQuantityInput(OrderItemCartModel item, string raw)
	{
		if (item is null || _isProcessing)
			return;

		var digits = string.IsNullOrWhiteSpace(raw)
			? string.Empty
			: new string(raw.Where(char.IsDigit).ToArray());

		decimal value = 0;
		if (!string.IsNullOrWhiteSpace(digits) && decimal.TryParse(digits, out var parsed))
			value = Math.Min(9999, parsed);

		item.Quantity = value;

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveOrderFile();
	}
	#endregion

	#region Saving
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
		catch (Exception ex)
		{
			ShowError("An error occurred while saving cart data", ex.Message);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task OnPlaceOrderClick()
	{
		if (_isProcessing || _isLoading)
			return;

		await SaveOrderFile();

		try
		{
			_isProcessing = true;
			StateHasChanged();

			var order = new OrderModel
			{
				Id = 0,
				LocationId = _user.LocationId,
				CompanyId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId)).Value),
				TransactionDateTime = await CommonData.LoadCurrentDateTime(),
				FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
				CreatedAt = await CommonData.LoadCurrentDateTime(),
				CreatedBy = _user.Id,
				TotalItems = _cart.Count,
				TotalQuantity = _cart.Sum(x => x.Quantity),
				CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
				Remarks = string.IsNullOrWhiteSpace(_orderRemarks?.Trim()) ? null : _orderRemarks.Trim(),
				SaleId = null,
				Status = true,
			};

			order.Id = await OrderData.SaveTransaction(order, OrderData.ConvertCartToDetails(_cart));
			await NotificationNavigate(order.Id);
		}
		catch (Exception ex)
		{
			ShowError("An error occurred while placing the order", ex.Message);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task NotificationNavigate(int orderId)
	{
		var overview = await CommonData.LoadTableDataById<OrderOverviewModel>(StoreNames.OrderOverview, orderId);

		await DataStorageService.LocalSaveAsync(StorageFileNames.OrderMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		await DataStorageService.LocalSaveAsync(StorageFileNames.OrderMobileDataFileName, System.Text.Json.JsonSerializer.Serialize(overview));

		await SendLocalNotification(overview);
		VibrationService.VibrateWithTime(500);
		NavigationManager.NavigateTo(StoreRouteNames.OrderMobileConfirmation, true);
	}
	#endregion

	#region Utilities
	private async Task SendLocalNotification(OrderOverviewModel order) =>
		await NotificationService.ShowLocalNotification(
			order.Id,
			"Order Placed",
			$"{order.TransactionNo}",
			$"Your order #{order.TransactionNo} has been successfully placed | Total Items: {order.TotalItems} | Total Qty: {order.TotalQuantity} | Date: {order.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {order.Remarks}");

	private void ShowError(string title, string message)
	{
		_validationErrors.Clear();
		_validationErrors.Add((title, message));
		_showValidationDialog = true;
		StateHasChanged();
	}
	#endregion
}
