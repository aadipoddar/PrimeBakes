using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Restaurant.Bill.Data;
using PrimeBakesLibrary.Restaurant.Bill.Exports;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Restaurant.Dining.Models;
using PrimeBakesLibrary.Store.Customer.Data;
using PrimeBakesLibrary.Store.Customer.Models;
using PrimeBakesLibrary.Store.PaymentMode;
using PrimeBakesLibrary.Store.Product.Models;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class BillMobilePaymentPage
{
	[Parameter] public int? DiningTableId { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showValidationDialog = false;

	private DiningTableModel _diningTable;
	private CustomerModel _selectedCustomer = new();
	private BillModel _bill = new();

	private List<ProductModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<BillItemCartModel> _cart = [];
	private List<BillDetailModel> _finalCart = [];
	private List<BillDetailModel> _previousCart = [];
	private readonly List<(string Field, string Message)> _validationErrors = [];
	private readonly List<PaymentItem> _payments = [];
	private readonly List<PaymentModeModel> _paymentMethods = [.. PaymentModeData.GetPaymentModes().Where(m => m.Name != "Credit")];

	private PaymentModeModel _selectedPaymentMethod = new();
	private decimal _paymentAmount = 0;
	private decimal _remainingAmount => _bill.TotalAmount - _payments.Sum(p => p.Amount);

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
		await LoadRunningBill();
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

	private async Task LoadRunningBill()
	{
		_previousCart.Clear();

		var runningBills = await BillData.LoadRunningBillByLocationId(_user.LocationId);
		var runningBill = runningBills.FirstOrDefault(b => b.DiningTableId == DiningTableId && b.Running);
		if (runningBill is null)
			return;

		_bill = runningBill;

		_previousCart = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, runningBill.Id);

		if (_bill.CustomerId.HasValue)
			_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(StoreNames.Customer, _bill.CustomerId.Value);
	}

	private async Task LoadCart()
	{
		_products = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
		_taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);

		if (await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName))
			_cart = JsonSerializer.Deserialize<List<BillItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.BillMobileCartDataFileName)) ?? [];

		if (_cart.Count == 0)
			NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		_selectedPaymentMethod = _paymentMethods.FirstOrDefault();
	}
	#endregion

	#region Changed Events
	private async Task OnCustomerNumberChanged(string args)
	{
		if (string.IsNullOrWhiteSpace(args))
		{
			_selectedCustomer = new();
			_bill.CustomerId = null;
			await SaveTransactionFile();
			return;
		}

		args = args.Trim();
		if (args.Any(c => !char.IsDigit(c)))
			args = new string([.. args.Where(char.IsDigit)]);

		_selectedCustomer = await CustomerData.LoadCustomerByNumber(args);
		_selectedCustomer ??= new()
		{
			Id = 0,
			Name = "",
			Number = args
		};

		_bill.CustomerId = _selectedCustomer.Id;
		await SaveTransactionFile();
	}

	private async Task OnDiscountPercentChanged(string raw)
	{
		_bill.DiscountPercent = Math.Clamp(ParseDecimal(raw, allowNegative: false), 0, 100);
		await SaveTransactionFile();
	}

	private async Task AdjustDiscountPercent(decimal delta)
	{
		_bill.DiscountPercent = Math.Clamp(_bill.DiscountPercent + delta, 0, 100);
		await SaveTransactionFile();
	}

	private async Task OnServiceChargePercentChanged(string raw)
	{
		_bill.ServiceChargePercent = Math.Clamp(ParseDecimal(raw, allowNegative: false), 0, 100);
		await SaveTransactionFile();
	}

	private async Task AdjustServiceChargePercent(decimal delta)
	{
		_bill.ServiceChargePercent = Math.Clamp(_bill.ServiceChargePercent + delta, 0, 100);
		await SaveTransactionFile();
	}

	private async Task OnRoundOffAmountChanged(string raw)
	{
		_bill.RoundOffAmount = ParseDecimal(raw, allowNegative: true);
		await SaveTransactionFile(true);
	}

	private async Task AdjustRoundOff(decimal delta)
	{
		_bill.RoundOffAmount += delta;
		await SaveTransactionFile(true);
	}

	private static decimal ParseDecimal(string raw, bool allowNegative)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return 0;

		var allowed = allowNegative ? "0123456789.-" : "0123456789.";
		var cleaned = new string([.. raw.Where(c => allowed.Contains(c))]);
		var negative = allowNegative && cleaned.StartsWith('-');
		cleaned = cleaned.Replace("-", "");

		if (!decimal.TryParse(cleaned, out var value))
			return 0;

		return negative ? -value : value;
	}
	#endregion

	#region Payment
	private void SelectPaymentMethod(PaymentModeModel method)
	{
		if (method is null || _isProcessing)
			return;

		_selectedPaymentMethod = method;
	}

	private void FillRemaining()
	{
		if (_remainingAmount <= 0)
			return;

		_paymentAmount = _remainingAmount;
	}

	private void OnPaymentAmountChanged(string raw) => _paymentAmount = ParseDecimal(raw, allowNegative: false);

	private void AddPayment()
	{
		if (_paymentAmount <= 0 || _selectedPaymentMethod == null || _selectedPaymentMethod.Id <= 0)
			return;

		if (_paymentAmount > _remainingAmount)
		{
			ShowError("Invalid Payment Amount", $"Payment amount cannot exceed remaining amount of ₹{_remainingAmount:N2}");
			return;
		}

		if (_payments.Any(p => p.Method == _selectedPaymentMethod.Name))
			_payments.First(p => p.Method == _selectedPaymentMethod.Name).Amount += _paymentAmount;

		else
			_payments.Add(new()
			{
				Id = _selectedPaymentMethod.Id,
				Method = _selectedPaymentMethod.Name,
				Amount = _paymentAmount
			});

		// Reset for next payment
		_paymentAmount = _remainingAmount;
		_selectedPaymentMethod = _paymentMethods.FirstOrDefault(_ => _.Id != _selectedPaymentMethod.Id);
		StateHasChanged();
	}

	private void RemovePayment(PaymentItem payment)
	{
		_payments.Remove(payment);
		_paymentAmount = _remainingAmount;
		StateHasChanged();
	}

	private void ConfirmPayment()
	{
		_bill.Cash = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Cash")?.Id)?.Amount ?? 0;
		_bill.Card = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Card")?.Id)?.Amount ?? 0;
		_bill.UPI = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "UPI")?.Id)?.Amount ?? 0;
		_bill.Credit = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Credit")?.Id)?.Amount ?? 0;
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails(bool customRoundOff = false)
	{
		if (!_user.ChangeProductFinancial)
			_bill.DiscountPercent = 0;

		await BillData.ApplyItemFinancialDetails(_cart, _products, _taxes);

		_finalCart.Clear();
		_finalCart = BillData.ConvertCartToDetails(_cart, _bill.Id);
		_finalCart.AddRange(_previousCart);

		foreach (var item in _finalCart.Where(_ => _.Quantity > 0))
		{
			item.Id = 0;
			item.NetRate = item.Total / item.Quantity * (1 - _bill.DiscountPercent / 100);
		}

		_bill.TotalItems = _finalCart.Count(x => x.Quantity > 0);
		_bill.TotalQuantity = _finalCart.Sum(x => x.Quantity);
		_bill.BaseTotal = _finalCart.Sum(x => x.BaseTotal);
		_bill.ItemDiscountAmount = _finalCart.Sum(x => x.DiscountAmount);
		_bill.TotalAfterItemDiscount = _finalCart.Sum(x => x.AfterDiscount);
		_bill.TotalInclusiveTaxAmount = _finalCart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_bill.TotalExtraTaxAmount = _finalCart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_bill.TotalAfterTax = _finalCart.Sum(x => x.Total);

		_bill.DiscountAmount = _bill.TotalAfterTax * _bill.DiscountPercent / 100;
		var totalAfterDiscount = _bill.TotalAfterTax - _bill.DiscountAmount;

		_bill.ServiceChargeAmount = totalAfterDiscount * _bill.ServiceChargePercent / 100;
		var totalAfterServiceCharge = totalAfterDiscount + _bill.ServiceChargeAmount;

		if (!customRoundOff)
			_bill.RoundOffAmount = Math.Round(totalAfterServiceCharge) - totalAfterServiceCharge;

		_bill.TotalAmount = totalAfterServiceCharge + _bill.RoundOffAmount;
	}

	private async Task SaveTransactionFile(bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails(customRoundOff);

			if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.BillMobileCartDataFileName, JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch (Exception ex)
		{
			ShowError("An Error Occurred While Saving Cart Data", ex.Message);
		}
		finally
		{
			if (_paymentAmount <= 0)
				_paymentAmount = Math.Max(0, _remainingAmount);

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task PrepareBillForSave()
	{
		_bill.Status = true;
		_bill.LocationId = _user.LocationId;
		_bill.CreatedBy = _user.Id;
		_bill.LastModifiedBy = _user.Id;
		_bill.CompanyId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId)).Value);
		_bill.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		_bill.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(_bill.TransactionDateTime)).Id;
		_bill.CreatedAt = await CommonData.LoadCurrentDateTime();
		_bill.LastModifiedAt = _bill.CreatedAt;
		_bill.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_bill.LastModifiedFromPlatform = _bill.CreatedFromPlatform;
		if (_bill.Id == 0)
			_bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(_bill);
	}

	private void HandleBillSettlement()
	{
		_bill.Running = _bill.Cash + _bill.Card + _bill.UPI + _bill.Credit != _bill.TotalAmount;

		if (!_bill.Running)
			foreach (var item in _finalCart)
				item.KOTPrint = false;

		else
		{
			_bill.Cash = 0;
			_bill.Card = 0;
			_bill.UPI = 0;
			_bill.Credit = 0;
		}
	}

	private async Task HandleKOTPrint()
	{
		var kotCategoryItems = await BillData.KOTCategoryItemsFromBill(_bill.Id);
		if (kotCategoryItems.Count == 0)
		{
			ShowError("KOT Print", "No items found for KOT printing.");
			return;
		}

		foreach (var kotCategoryId in kotCategoryItems.Keys)
			await ThermalPrintDispatcher.PrintAsync(
				async () => await KOTThermalPrint.GenerateThermalBill(_bill.Id, kotCategoryId, kotCategoryItems[kotCategoryId]),
				async () => await KOTThermalPrint.GenerateThermalBillPng(_bill.Id, kotCategoryId, kotCategoryItems[kotCategoryId]));

		await BillData.MarkKOTAsPrinted(_bill.Id);
	}

	private async Task SaveTransaction(bool kotOnly = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await SaveTransactionFile(true);
			ConfirmPayment();

			await PrepareBillForSave();

			HandleBillSettlement();

			_bill.Id = await BillData.SaveTransaction(_bill, _finalCart, _selectedCustomer);
			if (_bill.Id <= 0)
				throw new Exception("Failed to save the bill. Please try again.");

			if (kotOnly)
			{
				await HandleKOTPrint();
				NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);
				return;
			}

			if (_bill.Running)
			{
				NavigationManager.NavigateTo(RestaurantRouteNames.DiningMobileDashboard, true);
				return;
			}

			await NotificationNavigate(_bill.Id);
		}
		catch (Exception ex)
		{
			ShowError("An Error Occurred While Saving Bill", ex.Message);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Utilities
	private async Task NotificationNavigate(int billId)
	{
		var overview = await CommonData.LoadTableDataById<BillOverviewModel>(RestaurantNames.BillOverview, billId);
		await DataStorageService.LocalSaveAsync(StorageFileNames.BillMobileDataFileName, JsonSerializer.Serialize(overview));
		await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);

		await SendLocalNotification(overview);
		VibrationService.VibrateWithTime(500);
		NavigationManager.NavigateTo(RestaurantRouteNames.BillMobileConfirmation, true);
	}

	private async Task SendLocalNotification(BillOverviewModel bill) =>
		await NotificationService.ShowLocalNotification(
			bill.Id,
			"Bill Placed",
			$"{bill.TransactionNo}",
			$"Your bill #{bill.TransactionNo} has been successfully placed | Total Items: {bill.TotalItems} | Total Qty: {bill.TotalQuantity} | Date: {bill.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {bill.Remarks}");

	private void ShowError(string title, string message)
	{
		_validationErrors.Clear();
		_validationErrors.Add((title, message));
		_showValidationDialog = true;
		StateHasChanged();
	}
	#endregion
}
